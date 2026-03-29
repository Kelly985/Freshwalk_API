using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/agent")]
[Authorize(Roles = "Agent,Admin")]
public class AgentController(AppDbContext db, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("bookings")]
    public async Task<IActionResult> ListBookings(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? location,
        [FromQuery] string? search)
    {
        var q = db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.Payment)
            .Include(b => b.Order)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var fromDate))
        {
            var start = new DateTimeOffset(fromDate, TimeOnly.MinValue, TimeSpan.Zero);
            q = q.Where(b => b.CreatedAt >= start);
        }

        if (!string.IsNullOrWhiteSpace(to) && DateOnly.TryParse(to, out var toDate))
        {
            var end = new DateTimeOffset(toDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);
            q = q.Where(b => b.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim().ToLower();
            q = q.Where(b => b.PickupAddress.ToLower().Contains(loc));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            var sCompact = s.Replace(" ", "", StringComparison.Ordinal);
            q = q.Where(b =>
                (b.Customer.Email != null && b.Customer.Email.ToLower().Contains(s)) ||
                (b.Customer.PhoneNumber != null &&
                 b.Customer.PhoneNumber.Replace(" ", "", StringComparison.Ordinal).ToLower().Contains(sCompact)));
        }

        var rows = await q.OrderByDescending(b => b.CreatedAt).ToListAsync();

        var dto = rows.Select(b => new AgentBookingListItemDto(
            b.Id,
            b.BookingReference,
            b.Order?.OrderReference,
            b.Order?.Id,
            b.Customer.FullName,
            b.Customer.Email ?? "",
            b.Customer.PhoneNumber,
            b.Customer.CustomerReference,
            b.Status,
            b.Payment?.Status,
            b.Order?.Status,
            b.PriceKes,
            b.PickupAddress,
            b.PickupLocationUrl,
            b.CreatedAt)).ToList();

        return Ok(dto);
    }

    [HttpGet("bookings/{id:guid}")]
    public async Task<IActionResult> GetBookingDetail([FromRoute] Guid id)
    {
        var b = await db.Bookings
            .AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Payment)
            .Include(x => x.Order)
            .ThenInclude(o => o!.RiderAssignments)
            .ThenInclude(a => a.Rider)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (b is null) return NotFound();

        var assignments = b.Order?.RiderAssignments
            .Select(a => new AgentRiderAssignmentDto(
                a.Id,
                a.AssignmentReference,
                a.AssignmentType.ToString(),
                a.Status.ToString(),
                a.RiderId,
                a.Rider.FullName,
                a.Rider.Email,
                a.Rider.PhoneNumber))
            .ToList() ?? new List<AgentRiderAssignmentDto>();

        var dto = new AgentBookingDetailDto(
            b.Id,
            b.BookingReference,
            b.Payment?.PaymentReference,
            b.Order?.OrderReference,
            b.Order?.Id,
            b.Customer.FullName,
            b.Customer.Email ?? "",
            b.Customer.PhoneNumber,
            b.Customer.CustomerReference,
            b.CustomerId,
            b.Status,
            b.Payment?.Status,
            b.Payment?.Amount,
            b.Order?.Status,
            b.PriceKes,
            b.SubtotalBeforeDiscountKes,
            b.BundleDiscountPercent,
            b.DiscountAmountKes,
            b.TotalPairs,
            b.PickupAddress,
            b.PickupLocationUrl,
            b.PickupLatitude,
            b.PickupLongitude,
            b.AddOns,
            b.SneakersLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.SuedeLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.NubuckLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.OfficialLeatherLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            assignments,
            b.CreatedAt);

        return Ok(dto);
    }

    [HttpPatch("bookings/{id:guid}/status")]
    public async Task<IActionResult> UpdateBookingStatus([FromRoute] Guid id, [FromBody] AgentUpdateBookingStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status)
            || !Enum.TryParse<BookingStatus>(request.Status, true, out var newStatus))
            return BadRequest(new { message = "Invalid booking status." });

        var booking = await db.Bookings
            .Include(b => b.Payment)
            .Include(b => b.Order)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();

        if (booking.Order?.Status == OrderStatus.Delivered)
            return BadRequest(new { message = "Cannot change booking after order is delivered." });

        if (newStatus == BookingStatus.Cancelled)
        {
            booking.Status = BookingStatus.Cancelled;
            if (booking.Payment is { Status: not PaymentStatus.Success })
                booking.Payment.Status = PaymentStatus.Failed;
            await db.SaveChangesAsync();
            return Ok(new { booking.Id, booking.Status });
        }

        if (newStatus == BookingStatus.PaymentConfirmed)
        {
            if (booking.Payment?.Status != PaymentStatus.Success)
                return BadRequest(new { message = "Payment must be successful before confirming booking." });
            booking.Status = BookingStatus.PaymentConfirmed;
            await db.SaveChangesAsync();
            return Ok(new { booking.Id, booking.Status });
        }

        return BadRequest(new { message = "Agents may set status to Cancelled or PaymentConfirmed (when paid)." });
    }

    [HttpGet("customers")]
    public async Task<IActionResult> ListCustomers()
    {
        var users = await userManager.GetUsersInRoleAsync("Customer");
        var counts = await db.Bookings.AsNoTracking()
            .GroupBy(b => b.CustomerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var dto = users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AgentCustomerRowDto(
                u.Id,
                u.FullName,
                u.Email ?? "",
                u.PhoneNumber,
                u.CustomerReference,
                u.CreatedAt,
                counts.GetValueOrDefault(u.Id, 0)))
            .ToList();

        return Ok(dto);
    }

    [HttpGet("riders")]
    public async Task<IActionResult> ListRiders()
    {
        var users = await userManager.GetUsersInRoleAsync("Rider");
        var profiles = await db.RiderProfiles.AsNoTracking().ToDictionaryAsync(p => p.UserId);

        var dto = users
            .OrderBy(u => u.FullName)
            .Select(u =>
            {
                profiles.TryGetValue(u.Id, out var p);
                return new AgentRiderRowDto(
                    u.Id,
                    u.FullName,
                    u.Email ?? "",
                    u.PhoneNumber,
                    p?.BikeRegistration,
                    p?.IsAvailable ?? true);
            })
            .ToList();

        return Ok(dto);
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> ReportSummary([FromQuery] string? from, [FromQuery] string? to)
    {
        var q = db.Bookings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var fromDate))
        {
            var start = new DateTimeOffset(fromDate, TimeOnly.MinValue, TimeSpan.Zero);
            q = q.Where(b => b.CreatedAt >= start);
        }

        if (!string.IsNullOrWhiteSpace(to) && DateOnly.TryParse(to, out var toDate))
        {
            var end = new DateTimeOffset(toDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);
            q = q.Where(b => b.CreatedAt < end);
        }

        var bookings = await q.ToListAsync();
        var bookingIds = bookings.Select(b => b.Id).ToHashSet();

        var orders = await db.Orders.AsNoTracking()
            .Where(o => bookingIds.Contains(o.BookingId))
            .ToListAsync();

        var revenue = await db.Payments.AsNoTracking()
            .Where(p => bookingIds.Contains(p.BookingId) && p.Status == PaymentStatus.Success)
            .SumAsync(p => p.Amount);

        var dto = new AgentReportSummaryDto(
            bookings.Count,
            bookings.Count(b => b.Status == BookingStatus.PendingPayment),
            bookings.Count(b => b.Status == BookingStatus.PaymentConfirmed),
            orders.Count(o => o.Status != OrderStatus.Delivered),
            orders.Count(o => o.Status == OrderStatus.Delivered),
            revenue);

        return Ok(dto);
    }
}
