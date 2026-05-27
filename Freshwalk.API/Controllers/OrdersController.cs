using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Freshwalk.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Agent,Admin")]
public class OrdersController(AppDbContext db, IReferenceCodeIssuer referenceIssuer, OrderOtpService otpService, IEmailService emailService, ILogger<OrdersController> log) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await db.Orders
            .Include(x => x.Booking)
            .ThenInclude(b => b!.Customer)
            .Include(x => x.RiderAssignments)
            .ThenInclude(a => a.Rider)
            .OrderByDescending(x => x.Booking!.CreatedAt)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var order = await db.Orders
            .Include(x => x.Booking)
            .ThenInclude(b => b!.Customer)
            .Include(x => x.Booking!)
            .ThenInclude(b => b.Payment)
            .Include(x => x.RiderAssignments)
            .ThenInclude(a => a.Rider)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpPost("{id}/assign-pickup-rider")]
    public async Task<IActionResult> AssignPickupRider([FromRoute] Guid id, [FromBody] AssignRiderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();

        if (order.Status is not (OrderStatus.AwaitingPickupRider or OrderStatus.PaymentConfirmed))
            return BadRequest(new { message = "Order is not waiting for a pickup rider assignment." });

        order.Status = OrderStatus.PickupRiderAssigned;
        order.PickupOtp = Random.Shared.Next(100000, 999999).ToString();
        order.PickupOtpExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

        var raRef = await referenceIssuer.IssueAsync(ReferencePrefixes.RiderAssignment, cancellationToken);
        db.OrderRiderAssignments.Add(new OrderRiderAssignment
        {
            Id = Guid.NewGuid(),
            OrderId = id,
            RiderId = request.RiderId,
            AssignmentReference = raRef,
            AssignmentType = AssignmentType.Pickup,
            Status = AssignmentStatus.Assigned
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { order.Id, order.PickupOtp, order.Status });
    }

    [HttpPost("{id}/mark-at-shop")]
    public async Task<IActionResult> MarkAtShop([FromRoute] Guid id)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.InTransitToShop)
            return BadRequest(new { message = "Order must be in transit to the shop before marking as received." });
        order.Status = OrderStatus.AtShop;
        await db.SaveChangesAsync();
        return Ok(new { order.Id, order.Status });
    }

    [HttpPost("{id}/start-cleaning")]
    public async Task<IActionResult> StartCleaning([FromRoute] Guid id)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.AtShop)
            return BadRequest(new { message = "Shoes must be at the shop before cleaning starts." });
        order.Status = OrderStatus.CleaningInProgress;
        order.CleaningStartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { order.Id, order.Status });
    }

    [HttpPost("{id}/complete-cleaning")]
    public async Task<IActionResult> CompleteCleaning([FromRoute] Guid id)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == id);
        if (order is null) return NotFound();
        if (order.Status != OrderStatus.CleaningInProgress)
            return BadRequest(new { message = "Cleaning must be in progress to complete." });
        order.Status = OrderStatus.AwaitingDeliveryRider;
        order.CleaningCompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { order.Id, order.Status });
    }

    [HttpPost("{id}/assign-delivery-rider")]
    public async Task<IActionResult> AssignDeliveryRider([FromRoute] Guid id, [FromBody] AssignRiderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(o => o.Booking).ThenInclude(b => b.Customer)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (order is null) return NotFound();

        if (order.Status != OrderStatus.AwaitingDeliveryRider)
            return BadRequest(new { message = "Order must await a delivery rider (mark ready for delivery after cleaning)." });

        order.Status = OrderStatus.DeliveryRiderAssigned;
        var delCode = Random.Shared.Next(100000, 999999).ToString();
        var delExp = OrderOtpService.DefaultExpiryUtc();
        order.DeliveryOtp = delCode;
        order.DeliveryOtpExpiresAt = delExp;
        await otpService.UpsertDeliveryAsync(order.Id, delCode, delExp, cancellationToken);

        var raRef = await referenceIssuer.IssueAsync(ReferencePrefixes.RiderAssignment, cancellationToken);
        db.OrderRiderAssignments.Add(new OrderRiderAssignment
        {
            Id = Guid.NewGuid(),
            OrderId = id,
            RiderId = request.RiderId,
            AssignmentReference = raRef,
            AssignmentType = AssignmentType.Delivery,
            Status = AssignmentStatus.Assigned
        });
        await db.SaveChangesAsync(cancellationToken);

        // Email 2 — delivery ready + OTP (non-fatal)
        _ = TrySendDeliveryReadyEmailAsync(order, delCode, cancellationToken);

        return Ok(new { order.Id, order.DeliveryOtp, order.Status });
    }

    private async Task TrySendDeliveryReadyEmailAsync(Order order, string deliveryOtp, CancellationToken ct)
    {
        var customer = order.Booking?.Customer;
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email)) return;
        var booking = order.Booking!;
        try
        {
            var (subject, html) = EmailTemplates.DeliveryReady(
                customer.FullName,
                booking.BookingReference,
                order.OrderReference ?? booking.BookingReference,
                deliveryOtp,
                booking.PickupAddress,
                booking.SneakersLines,
                booking.SuedeLines,
                booking.NubuckLines,
                booking.CanvasLines,
                booking.PriceKes);
            await emailService.SendAsync(customer.Email, customer.FullName, subject, html, ct);
            log.LogInformation("Delivery ready email sent to {Email} for order {Ref}", customer.Email, order.OrderReference);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not send delivery ready email for order {Ref}", order.OrderReference);
        }
    }

}

public record AssignRiderRequest(Guid RiderId);
public record OtpRequest(string Otp);
