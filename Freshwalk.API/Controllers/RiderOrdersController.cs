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
    public async Task<IActionResult> ActiveOrders(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? assignmentType,
        CancellationToken cancellationToken)
    {
        var riderId = ResolveUserId();
        if (riderId == Guid.Empty) return Unauthorized();

        IQueryable<OrderRiderAssignment> q = db.OrderRiderAssignments
            .AsNoTracking()
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .ThenInclude(b => b.Customer)
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .ThenInclude(b => b.Payment)
            .Where(a => a.RiderId == riderId)
            .Where(a =>
                (a.AssignmentType == AssignmentType.Pickup &&
                 (a.Order.Status == OrderStatus.PickupRiderAssigned || a.Order.Status == OrderStatus.InTransitToShop)) ||
                (a.AssignmentType == AssignmentType.Delivery &&
                 (a.Order.Status == OrderStatus.DeliveryRiderAssigned || a.Order.Status == OrderStatus.InTransitToCustomer)));

        q = ApplyRiderListFilters(q, from, to, assignmentType);

        var assignments = await q
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(cancellationToken);

        return Ok(assignments.Select(MapListItem).ToList());
    }

    [HttpGet("orders/history")]
    public async Task<IActionResult> History(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? assignmentType,
        [FromQuery] string? bookingRef,
        CancellationToken cancellationToken)
    {
        var riderId = ResolveUserId();
        if (riderId == Guid.Empty) return Unauthorized();

        IQueryable<OrderRiderAssignment> q = db.OrderRiderAssignments
            .AsNoTracking()
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .ThenInclude(b => b.Customer)
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .ThenInclude(b => b.Payment)
            .Where(a => a.RiderId == riderId)
            .Where(a =>
                a.Order.DeliveredAt != null ||
                (a.AssignmentType == AssignmentType.Pickup &&
                 (a.Order.Status == OrderStatus.AtShop ||
                  a.Order.Status == OrderStatus.CleaningInProgress ||
                  a.Order.Status == OrderStatus.AwaitingDeliveryRider ||
                  a.Order.Status == OrderStatus.DeliveryRiderAssigned ||
                  a.Order.Status == OrderStatus.Delivered)));

        q = ApplyRiderListFilters(q, from, to, assignmentType);

        if (!string.IsNullOrWhiteSpace(bookingRef))
        {
            var br = bookingRef.Trim();
            q = q.Where(a => a.Order.Booking.BookingReference.Contains(br));
        }

        var rows = await q
            .OrderByDescending(a => a.Order.DeliveredAt ?? a.Order.PickedUpAt ?? a.AssignedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(MapListItem).ToList());
    }

    private static IQueryable<OrderRiderAssignment> ApplyRiderListFilters(
        IQueryable<OrderRiderAssignment> q,
        string? from,
        string? to,
        string? assignmentType)
    {
        if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var fromDate))
        {
            var start = new DateTimeOffset(fromDate, TimeOnly.MinValue, TimeSpan.Zero);
            q = q.Where(a => a.Order.Booking.CreatedAt >= start);
        }

        if (!string.IsNullOrWhiteSpace(to) && DateOnly.TryParse(to, out var toDate))
        {
            var end = new DateTimeOffset(toDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);
            q = q.Where(a => a.Order.Booking.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(assignmentType)
            && Enum.TryParse<AssignmentType>(assignmentType, true, out var at))
        {
            q = q.Where(a => a.AssignmentType == at);
        }

        return q;
    }

    private static RiderOrderListItemDto MapListItem(OrderRiderAssignment a)
    {
        var b = a.Order.Booking;
        var pay = b.Payment;

        return new RiderOrderListItemDto(
            a.OrderId,
            b.Id,
            b.BookingReference,
            a.Order.OrderReference,
            a.Order.Status,
            a.AssignmentType,
            a.Id,
            b.PickupAddress,
            b.PickupLocationUrl,
            b.CreatedAt,
            b.Customer.FullName,
            b.Customer.Email,
            b.Customer.PhoneNumber,
            pay?.PayerPhoneNumber);
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
            .Include(a => a.Order)
            .ThenInclude(o => o.Booking)
            .ThenInclude(b => b.Payment)
            .Where(a => a.OrderId == orderId && a.RiderId == riderId)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null) return NotFound();

        var b = assignment.Order.Booking;
        var o = assignment.Order;
        var pay = b.Payment;

        var pickupOtp = assignment.AssignmentType == AssignmentType.Pickup &&
                        o.Status == OrderStatus.PickupRiderAssigned
            ? o.PickupOtp
            : null;
        var pickupExp = assignment.AssignmentType == AssignmentType.Pickup &&
                          o.Status == OrderStatus.PickupRiderAssigned
            ? o.PickupOtpExpiresAt
            : null;

        var payerPhone = pay?.PayerPhoneNumber;

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
            b.Customer.Email,
            b.Customer.PhoneNumber,
            payerPhone,
            b.SneakersLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.SuedeLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.NubuckLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.CanvasLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.AddOns,
            b.TotalPairs,
            b.PriceKes,
            b.ShoesSubtotalGrossKes,
            b.PromotionalDiscountKes,
            b.AppliedPromotions.Select(x => new PromotionAppliedDto(x.CategoryKey, x.Label, x.AmountSavedKes)).ToList(),
            b.SubtotalBeforeDiscountKes,
            b.BundleDiscountPercent,
            b.DiscountAmountKes);

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
