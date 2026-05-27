using System.Security.Claims;
using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Freshwalk.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(
    AppDbContext db,
    IPricingService pricingService,
    UserManager<ApplicationUser> userManager,
    IReferenceCodeIssuer referenceIssuer,
    IEmailService emailService,
    ILogger<BookingsController> log) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "At least one shoe item is required." });

        var addOns = BookingAddOns.Normalize(request.AddOns);
        var customerId = ResolveUserId();
        var pricing = await pricingService.CalculateBreakdownAsync(request.Items, addOns, cancellationToken);
        var totalPairs = request.Items.Sum(i => i.PairCount);

        string? locationUrl = null;
        if (request.PickupLatitude.HasValue && request.PickupLongitude.HasValue)
            locationUrl = $"https://www.google.com/maps?q={request.PickupLatitude.Value},{request.PickupLongitude.Value}";

        var bookingRef = await referenceIssuer.IssueAsync(ReferencePrefixes.Booking, cancellationToken);
        var paymentRef = await referenceIssuer.IssueAsync(ReferencePrefixes.Payment, cancellationToken);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            BookingReference = bookingRef,
            TotalPairs = totalPairs,
            PickupAddress = request.PickupAddress,
            PickupLatitude = request.PickupLatitude,
            PickupLongitude = request.PickupLongitude,
            PickupLocationUrl = locationUrl,
            AddOns = addOns,
            ShoesSubtotalGrossKes = pricing.ShoesSubtotalGrossKes,
            PromotionalDiscountKes = pricing.PromotionalDiscountKes,
            AppliedPromotions = pricing.AppliedPromotions
                .Select(p => new PromotionAppliedSnapshot
                {
                    CategoryKey = p.CategoryKey,
                    Label = p.Label,
                    AmountSavedKes = p.AmountSavedKes
                })
                .ToList(),
            SubtotalBeforeDiscountKes = pricing.SubtotalBeforeDiscountKes,
            BundleDiscountPercent = pricing.BundleDiscountPercent,
            DiscountAmountKes = pricing.DiscountAmountKes,
            PriceKes = pricing.FinalPriceKes,
            Status = BookingStatus.PendingPayment,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var item in request.Items)
        {
            var line = new ShoeLineItem { Color = item.ColorTier, Quantity = item.PairCount };
            switch (item.ShoeType)
            {
                case "Sneakers": booking.SneakersLines.Add(line); break;
                case "Suede": booking.SuedeLines.Add(line); break;
                case "Nubuck": booking.NubuckLines.Add(line); break;
                case "Canvas":
                case "OfficialLeather":
                    booking.CanvasLines.Add(line);
                    break;
            }
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            PaymentReference = paymentRef,
            Amount = pricing.FinalPriceKes,
            Status = PaymentStatus.Pending
        };

        db.Bookings.Add(booking);
        db.Payments.Add(payment);

        // Create the order immediately — payment is collected by the delivery rider at the door.
        var order = new Order
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            CustomerId = booking.CustomerId,
            OrderReference = booking.BookingReference,
            Status = OrderStatus.AwaitingPickupRider
        };
        db.Orders.Add(order);

        await db.SaveChangesAsync(cancellationToken);

        var customer = await userManager.FindByIdAsync(customerId.ToString());

        // Email 1 — order acknowledgment (non-fatal; fires at booking time, not M-Pesa callback)
        _ = TrySendOrderAcknowledgmentAsync(booking, customer, pricing.Lines, cancellationToken);

        return Ok(ToResponse(booking, customer?.CustomerReference));
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? bookingStatus,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? orderStatus,
        [FromQuery] string? from,
        [FromQuery] string? to)
    {
        var customerId = ResolveUserId();
        if (customerId == Guid.Empty)
            return Unauthorized();

        var customerRef = await db.Users.AsNoTracking()
            .Where(u => u.Id == customerId)
            .Select(u => u.CustomerReference)
            .FirstOrDefaultAsync() ?? string.Empty;

        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Payment)
            .Include(b => b.Order)
            .Where(b => b.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(bookingStatus)
            && Enum.TryParse<BookingStatus>(bookingStatus, true, out var bs))
            query = query.Where(b => b.Status == bs);

        if (!string.IsNullOrWhiteSpace(paymentStatus)
            && Enum.TryParse<PaymentStatus>(paymentStatus, true, out var ps))
            query = query.Where(b => b.Payment != null && b.Payment.Status == ps);

        if (!string.IsNullOrWhiteSpace(orderStatus)
            && Enum.TryParse<OrderStatus>(orderStatus, true, out var os))
            query = query.Where(b => b.Order != null && b.Order.Status == os);

        if (!string.IsNullOrWhiteSpace(from) && DateOnly.TryParse(from, out var fromDate))
        {
            var start = new DateTimeOffset(fromDate, TimeOnly.MinValue, TimeSpan.Zero);
            query = query.Where(b => b.CreatedAt >= start);
        }

        if (!string.IsNullOrWhiteSpace(to) && DateOnly.TryParse(to, out var toDate))
        {
            var end = new DateTimeOffset(toDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);
            query = query.Where(b => b.CreatedAt < end);
        }

        var rows = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var dto = rows.Select(b => new CustomerBookingTrackingDto(
            b.Id,
            b.BookingReference,
            b.Order?.OrderReference,
            b.Payment?.PaymentReference,
            customerRef,
            b.PriceKes,
            b.Status,
            b.Payment?.Status,
            b.Payment?.Amount,
            b.Order?.Status,
            b.Order?.Id,
            b.CreatedAt,
            b.PickupAddress,
            b.PickupLocationUrl,
            b.Order != null && b.Order.Status == OrderStatus.PickupRiderAssigned,
            b.Order != null && b.Order.Status == OrderStatus.DeliveryRiderAssigned ? b.Order.DeliveryOtp : null,
            b.Order?.Status == OrderStatus.Delivered)).ToList();

        return Ok(dto);
    }

    [HttpGet]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> GetAll()
    {
        var bookings = await db.Bookings
            .Include(x => x.Customer)
            .Include(x => x.Order)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return Ok(bookings.Select(b => ToResponse(b)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var b = await db.Bookings
            .Include(x => x.Customer)
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();

        var uid = ResolveUserId();
        if (!User.IsInRole("Admin") && !User.IsInRole("Agent") && b.CustomerId != uid)
            return Forbid();

        return Ok(ToResponse(b));
    }

    private async Task TrySendOrderAcknowledgmentAsync(
        Booking booking,
        ApplicationUser? customer,
        IReadOnlyList<LineBreakdownDto> lineBreakdowns,
        CancellationToken ct)
    {
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email)) return;
        try
        {
            var (subject, html) = EmailTemplates.OrderConfirmation(
                customer.FullName,
                booking.BookingReference,
                booking.PickupAddress,
                lineBreakdowns,
                booking.AddOns,
                booking.ShoesSubtotalGrossKes,
                booking.PromotionalDiscountKes,
                booking.AppliedPromotions,
                booking.SubtotalBeforeDiscountKes,
                booking.BundleDiscountPercent,
                booking.DiscountAmountKes,
                booking.PriceKes);
            await emailService.SendAsync(customer.Email, customer.FullName, subject, html, ct);
            log.LogInformation("Order acknowledgment email sent to {Email} for booking {Ref}", customer.Email, booking.BookingReference);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not send order acknowledgment email for booking {Ref}", booking.BookingReference);
        }
    }

    private static BookingResponse ToResponse(Booking b, string? customerReferenceOverride = null)
    {
        var customerRef = customerReferenceOverride
            ?? b.Customer?.CustomerReference
            ?? string.Empty;
        return new BookingResponse(
            b.Id,
            customerRef,
            b.BookingReference,
            b.Order?.OrderReference,
            b.PriceKes,
            b.ShoesSubtotalGrossKes,
            b.PromotionalDiscountKes,
            b.AppliedPromotions.Select(x => new PromotionAppliedDto(x.CategoryKey, x.Label, x.AmountSavedKes)).ToList(),
            b.SubtotalBeforeDiscountKes,
            b.BundleDiscountPercent,
            b.DiscountAmountKes,
            b.TotalPairs,
            b.Status,
            b.CreatedAt,
            b.PickupAddress,
            b.PickupLocationUrl,
            b.AddOns,
            b.SneakersLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.SuedeLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.NubuckLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList(),
            b.CanvasLines.Select(x => new ShoeLineItemDto(x.Color, x.Quantity)).ToList());
    }

    private Guid ResolveUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
