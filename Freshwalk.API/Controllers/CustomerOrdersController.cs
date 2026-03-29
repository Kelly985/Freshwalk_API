using System.Security.Claims;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Freshwalk.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

/// <summary>Customer-only order actions (pickup OTP confirms handoff to rider → in transit to shop).</summary>
[ApiController]
[Route("api/customer/orders")]
[Authorize]
public class CustomerOrdersController(AppDbContext db, OrderOtpService otpService) : ControllerBase
{
    [HttpPost("{orderId:guid}/verify-pickup-otp")]
    public async Task<IActionResult> VerifyPickupOtp([FromRoute] Guid orderId, [FromBody] OtpBody body)
    {
        var uid = ResolveUserId();
        if (uid == Guid.Empty) return Unauthorized();

        var order = await db.Orders
            .Include(o => o.Booking)
            .ThenInclude(b => b.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return NotFound();

        if (order.Booking.CustomerId != uid)
            return Forbid();

        if (order.Booking.Status != BookingStatus.PaymentConfirmed || order.Booking.Payment is not { Status: PaymentStatus.Success })
            return BadRequest(new { message = "Payment must be completed first." });

        if (order.Status != OrderStatus.PickupRiderAssigned)
            return BadRequest(new { message = "Pickup OTP can only be entered when a rider is assigned for pickup." });

        if (!await otpService.ValidatePickupAsync(orderId, body.Otp, HttpContext.RequestAborted))
            return BadRequest(new { message = "Invalid or expired OTP." });

        order.PickupOtpVerifiedAt = DateTimeOffset.UtcNow;
        order.Status = OrderStatus.InTransitToShop;
        order.PickedUpAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return Ok(new { order.Id, order.Status, message = "Pickup confirmed. Rider is bringing your shoes to the shop." });
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
