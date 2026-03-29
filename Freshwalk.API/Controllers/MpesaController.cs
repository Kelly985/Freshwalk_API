using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Freshwalk.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/mpesa")]
public class MpesaController(AppDbContext db, IMpesaStkService stkService, ILogger<MpesaController> log) : ControllerBase
{
    private static readonly JsonSerializerOptions CallbackJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Called by Safaricom after STK Push. Daraja often uses a path ending in <c>stkcallback</c>; <c>callback</c> is an alias for tools and ngrok.</summary>
    [HttpPost("stkcallback")]
    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> StkCallback(CancellationToken cancellationToken)
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
            body = await reader.ReadToEndAsync(cancellationToken);

        try
        {
            var envelope = JsonSerializer.Deserialize<StkCallbackEnvelope>(body, CallbackJson);
            var cb = envelope?.Body?.StkCallback;
            if (cb?.CheckoutRequestID is null)
            {
                log.LogWarning("M-Pesa callback missing CheckoutRequestID: {Body}", body);
                return Ok(new { ResultCode = 0, ResultDesc = "Accepted" });
            }

            var receipt = ExtractMetadata(cb.CallbackMetadata, "MpesaReceiptNumber");
            var payerPhone = ExtractMetadata(cb.CallbackMetadata, "PhoneNumber");
            await ApplyStkCallbackAsync(
                cb.CheckoutRequestID,
                cb.ResultCode,
                cb.MerchantRequestID,
                receipt,
                NormalizeMsisdn(payerPhone),
                cb.ResultDesc,
                cancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "M-Pesa callback processing error");
        }

        return Ok(new { ResultCode = 0, ResultDesc = "Accepted" });
    }

    /// <summary>Customer retries STK for a booking still awaiting payment. Amount is taken from the booking payment record.</summary>
    [HttpPost("initiatestkpush")]
    [Authorize]
    public async Task<IActionResult> InitiateStkPush([FromBody] InitiateBookingStkRequest req, CancellationToken cancellationToken)
    {
        if (req.PhoneNumber is null || string.IsNullOrWhiteSpace(req.PhoneNumber))
            return BadRequest(new { message = "Phone number is required." });

        if ((req.BookingId is null || req.BookingId == Guid.Empty) && string.IsNullOrWhiteSpace(req.BookingReference))
            return BadRequest(new { message = "Provide bookingId or bookingReference (e.g. FW-20260328145632001-9043)." });

        var userId = ResolveUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var bookingQuery = db.Bookings.Include(b => b.Payment).AsQueryable();
        Booking? booking = null;
        if (req.BookingId is { } bid && bid != Guid.Empty)
            booking = await bookingQuery.FirstOrDefaultAsync(b => b.Id == bid, cancellationToken);
        if (booking is null && !string.IsNullOrWhiteSpace(req.BookingReference))
        {
            var br = req.BookingReference.Trim();
            booking = await bookingQuery.FirstOrDefaultAsync(
                b => b.BookingReference.ToLower() == br.ToLower(),
                cancellationToken);
        }

        if (booking is null)
            return NotFound(new { message = "Booking not found." });

        if (booking.CustomerId != userId)
            return Forbid();

        if (booking.Status != BookingStatus.PendingPayment)
            return BadRequest(new { message = "This booking is not awaiting payment." });

        var payment = booking.Payment;
        if (payment is null)
            return BadRequest(new { message = "No payment record for this booking." });

        if (payment.Status == PaymentStatus.Success)
            return BadRequest(new { message = "Payment already completed." });

        var acct = ReferenceCodeFormatting.ForMpesaAccountReference(booking.BookingReference, booking.Id);

        var result = await stkService.InitiateAsync(
            payment.Amount,
            req.PhoneNumber,
            accountReference: acct,
            description: "Freshwalk shoe cleaning",
            cancellationToken);

        if (!result.Ok)
            return StatusCode(500, new { message = result.Error ?? "STK Push failed." });

        payment.MpesaMerchantRequestId = result.MerchantRequestId;
        payment.MpesaCheckoutRequestId = result.CheckoutRequestId;
        payment.PayerPhoneNumber = NormalizeMsisdn(req.PhoneNumber);
        payment.Status = PaymentStatus.Initiated;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = result.CustomerMessage ?? "STK Push sent. Check your phone.",
            merchantRequestId = result.MerchantRequestId,
            checkoutRequestId = result.CheckoutRequestId
        });
    }

    /// <summary>
    /// Identifies the payment by <paramref name="checkoutRequestId"/> alone.
    /// That id is stored when STK is initiated (<see cref="MpesaController.InitiateStkPush"/>); Safaricom returns a unique value per push, so duplicate amounts do not collide.
    /// </summary>
    private static string? NormalizeMsisdn(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var d = new string(raw.Where(char.IsDigit).ToArray());
        return d.Length == 0 ? null : d;
    }

    private async Task ApplyStkCallbackAsync(
        string checkoutRequestId,
        int resultCode,
        string? merchantRequestId,
        string? mpesaReceiptNumber,
        string? payerPhoneNormalized,
        string? resultDesc,
        CancellationToken cancellationToken)
    {
        var payment = await db.Payments
            .Include(p => p.Booking)
            .ThenInclude(b => b.Order)
            .FirstOrDefaultAsync(p => p.MpesaCheckoutRequestId == checkoutRequestId, cancellationToken);

        if (payment is null)
        {
            log.LogWarning("M-Pesa callback: no payment for CheckoutRequestID {Id}", checkoutRequestId);
            return;
        }

        if (payment.Status == PaymentStatus.Success)
            return;

        if (resultCode == 0)
        {
            payment.Status = PaymentStatus.Success;
            payment.MpesaReceipt = mpesaReceiptNumber ?? payment.MpesaReceipt;
            payment.MpesaMerchantRequestId = merchantRequestId ?? payment.MpesaMerchantRequestId;
            if (!string.IsNullOrEmpty(payerPhoneNormalized))
                payment.PayerPhoneNumber = payerPhoneNormalized;

            var booking = payment.Booking;
            booking.Status = BookingStatus.PaymentConfirmed;

            if (booking.Order is null)
            {
                db.Orders.Add(new Order
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    CustomerId = booking.CustomerId,
                    OrderReference = booking.BookingReference,
                    Status = OrderStatus.AwaitingPickupRider
                });
            }
            else if (string.IsNullOrWhiteSpace(booking.Order.OrderReference)
                     && !string.IsNullOrWhiteSpace(booking.BookingReference))
            {
                booking.Order.OrderReference = booking.BookingReference;
            }

            await db.SaveChangesAsync(cancellationToken);
            log.LogInformation("M-Pesa payment confirmed for booking {BookingId}", booking.Id);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            log.LogInformation(
                "M-Pesa payment failed for CheckoutRequestID {Id}: {Code} {Desc}",
                checkoutRequestId,
                resultCode,
                resultDesc);
        }
    }

    private static string? ExtractMetadata(CallbackMetadataDto? meta, string name)
    {
        if (meta?.Item is null) return null;
        foreach (var item in meta.Item)
        {
            if (!string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (item.Value.ValueKind == JsonValueKind.String)
                return item.Value.GetString();
            if (item.Value.ValueKind == JsonValueKind.Number)
                return item.Value.ToString();
        }
        return null;
    }

    private Guid ResolveUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private sealed class StkCallbackEnvelope
    {
        public StkCallbackBodyDto? Body { get; set; }
    }

    private sealed class StkCallbackBodyDto
    {
        [JsonPropertyName("stkCallback")]
        public StkCallbackInnerDto? StkCallback { get; set; }
    }

    private sealed class StkCallbackInnerDto
    {
        public string? MerchantRequestID { get; set; }
        public string? CheckoutRequestID { get; set; }
        public int ResultCode { get; set; }
        public string? ResultDesc { get; set; }
        public CallbackMetadataDto? CallbackMetadata { get; set; }
    }

    private sealed class CallbackMetadataDto
    {
        public List<MetadataItemDto>? Item { get; set; }
    }

    private sealed class MetadataItemDto
    {
        public string? Name { get; set; }
        public JsonElement Value { get; set; }
    }
}
