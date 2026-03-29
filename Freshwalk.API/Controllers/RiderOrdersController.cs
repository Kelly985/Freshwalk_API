using System.Security.Claims;
using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Freshwalk.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/rider")]
[Authorize(Roles = "Rider")]
public class RiderOrdersController(AppDbContext db, OrderOtpService otpService) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IActionResult> ActiveOrders(CancellationToken cancellationToken)
    {
        var riderId = ResolveUserId();
        if (riderId == Guid.Empty) return Unauthorized();

        var assignments = await db.OrderRiderAssignments
            .AsNoTracking()
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .Where(a => a.RiderId == riderId)
            .Where(a =>
                (a.AssignmentType == AssignmentType.Pickup &&
                 (a.Order.Status == OrderStatus.PickupRiderAssigned || a.Order.Status == OrderStatus.InTransitToShop)) ||
                (a.AssignmentType == AssignmentType.Delivery &&
                 (a.Order.Status == OrderStatus.DeliveryRiderAssigned || a.Order.Status == OrderStatus.InTransitToCustomer)))
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

        var dto = assignments.Select(a => new RiderOrderListItemDto(
            a.OrderId,
            a.Order.BookingId,
            a.Order.Booking.BookingReference,
            a.Order.OrderReference,
            a.Order.Status,
            a.AssignmentType,
            a.Id,
            a.Order.Booking.PickupAddress,
            a.Order.Booking.PickupLocationUrl,
            a.Order.Booking.CreatedAt)).ToList();

        return Ok(dto);
    }

    [HttpGet("orders/history")]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var riderId = ResolveUserId();
        if (riderId == Guid.Empty) return Unauthorized();

        var rows = await db.OrderRiderAssignments
            .AsNoTracking()
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .Where(a => a.RiderId == riderId)
            .Where(a =>
                a.Order.DeliveredAt != null ||
                (a.AssignmentType == AssignmentType.Pickup &&
                 (a.Order.Status == OrderStatus.AtShop ||
                  a.Order.Status == OrderStatus.CleaningInProgress ||
                  a.Order.Status == OrderStatus.AwaitingDeliveryRider ||
                  a.Order.Status == OrderStatus.DeliveryRiderAssigned ||
                  a.Order.Status == OrderStatus.Delivered)))
            .OrderByDescending(a => a.Order.DeliveredAt ?? a.Order.PickedUpAt ?? a.AssignedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var dto = rows.Select(a => new RiderOrderListItemDto(
            a.OrderId,
            a.Order.BookingId,
            a.Order.Booking.BookingReference,
            a.Order.OrderReference,
            a.Order.Status,
            a.AssignmentType,
            a.Id,
            a.Order.Booking.PickupAddress,
            a.Order.Booking.PickupLocationUrl,
            a.Order.Booking.CreatedAt)).ToList();

        return Ok(dto);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> OrderDetail([FromRoute] Guid orderId, CancellationToken cancellationToken)
    {
        var riderId = ResolveUserId();
        if (riderId == Guid.Empty) return Unauthorized();

        var assignment = await db.OrderRiderAssignments
            .AsNoTracking()
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .ThenInclude(b => b.Customer)
            .Where(a => a.OrderId == orderId && a.RiderId == riderId)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null) return NotFound();

        var b = assignment.Order.Booking;
        var o = assignment.Order;

        var pickupOtp = assignment.AssignmentType == AssignmentType.Pickup &&
                        o.Status == OrderStatus.PickupRiderAssigned
            ? o.PickupOtp
            : null;
        var pickupExp = assignment.AssignmentType == AssignmentType.Pickup &&
                          o.Status == OrderStatus.PickupRiderAssigned
            ? o.PickupOtpExpiresAt
            : null;

        var dto = new RiderOrderDetailDto(
            o.Id,
            b.Id,
            b.BookingReference,
            o.OrderReference,
            o.Status,
            assignment.AssignmentType,
            assignment.Id,
            b.PickupAddress,
            b.PickupLocationUrl,
            b.PickupLatitude,
            b.PickupLongitude,
            pickupOtp,
            pickupExp,
            b.Customer.FullName,
            b.Customer.PhoneNumber,
            b.SneakersLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.SuedeLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.NubuckLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.OfficialLeatherLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.AddOns,
            b.TotalPairs,
            b.PriceKes);

        return Ok(dto);
    }

    [HttpPost("orders/{orderId:guid}/verify-delivery-otp")]
    public async Task<IActionResult> VerifyDeliveryOtp([FromRoute] Guid orderId, [FromBody] OtpBody body)
    {
        var riderId = ResolveUserId();
        if (riderId == Guid.Empty) return Unauthorized();

        var assignment = await db.OrderRiderAssignments
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .Where(a => a.OrderId == orderId && a.RiderId == riderId && a.AssignmentType == AssignmentType.Delivery)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        if (assignment is null)
            return NotFound(new { message = "No delivery assignment found for this order." });

        var order = assignment.Order;
        if (order.Status != OrderStatus.DeliveryRiderAssigned)
            return BadRequest(new { message = "Delivery OTP can only be verified when you are assigned for delivery." });

        if (!await otpService.ValidateDeliveryAsync(orderId, body.Otp, HttpContext.RequestAborted))
            return BadRequest(new { message = "Invalid or expired OTP." });

        order.DeliveryOtpVerifiedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTimeOffset.UtcNow;
        assignment.Status = AssignmentStatus.Completed;
        assignment.CompletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Ok(new { order.Id, order.Status, message = "Delivery confirmed. Order completed." });
    }

    private Guid ResolveUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    public sealed class OtpBody
    {
        public string? Otp { get; set; }
    }
}
