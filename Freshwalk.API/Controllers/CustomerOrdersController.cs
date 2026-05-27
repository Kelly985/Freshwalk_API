using System.Security.Claims;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Freshwalk.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Freshwalk.API.Controllers;

/// <summary>Customer-only order actions (pickup OTP confirms handoff to rider → in transit to shop; receipt download).</summary>
[ApiController]
[Route("api/customer/orders")]
[Authorize]
public class CustomerOrdersController(AppDbContext db, OrderOtpService otpService, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private const string LogoUrl      = "https://res.cloudinary.com/dlvaqdgqe/image/upload/v1779909319/FRESHWALK_LOGO_yndm3w.png";
    private const string ContactPhone = "+254 (0) 118 081 414";
    private const string ContactEmail = "freshwalkshoecare@gmail.com";
    private const string ContactAddr  = "J's Arcade, Thome Road, Nairobi, Kenya";
    private const string ContactHours = "Mon–Sat, 8:00 AM – 7:00 PM";

    // Cache logo bytes for the process lifetime (Cloudinary CDN, so this is safe).
    private static byte[]? _logoBytesCache;
    private static readonly SemaphoreSlim _logoLock = new(1, 1);

    [HttpPost("{orderId:guid}/verify-pickup-otp")]
    public async Task<IActionResult> VerifyPickupOtp([FromRoute] Guid orderId, [FromBody] OtpBody body)
    {
        var uid = ResolveUserId();
        if (uid == Guid.Empty) return Unauthorized();

        var order = await db.Orders
            .Include(o => o.Booking)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null) return NotFound();

        if (order.Booking.CustomerId != uid)
            return Forbid();

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

    /// <summary>
    /// Downloads a PDF receipt for a delivered order.
    /// Accessible to the order owner (Customer role) or any Agent/Admin.
    /// </summary>
    [HttpGet("{orderId:guid}/receipt")]
    public async Task<IActionResult> DownloadReceipt([FromRoute] Guid orderId, CancellationToken ct)
    {
        var uid = ResolveUserId();
        if (uid == Guid.Empty) return Unauthorized();

        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Booking)
            .ThenInclude(b => b.Customer)
            .Include(o => o.Booking)
            .ThenInclude(b => b.Payment)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null) return NotFound();

        // Only the customer who owns the order, or an agent/admin, can download
        var isStaff = User.IsInRole("Agent") || User.IsInRole("Admin");
        if (!isStaff && order.Booking.CustomerId != uid)
            return Forbid();

        if (order.Status != OrderStatus.Delivered)
            return BadRequest(new { message = "Receipt is only available once the order has been delivered." });

        var logoBytes = await FetchLogoBytesAsync(ct);
        var pdf = GenerateReceiptPdf(order, order.Booking, order.Booking.Payment, order.Booking.Customer, logoBytes);
        var fileName = $"Freshwalk-Receipt-{order.OrderReference ?? orderId.ToString()[..8].ToUpper()}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PDF generation
    // ──────────────────────────────────────────────────────────────────────────
    private static byte[] GenerateReceiptPdf(Order order, Booking booking, Payment? payment, ApplicationUser customer, byte[]? logoBytes)
    {
        const string Brand     = "#E31E24";
        const string Grey1     = "#555555";
        const string Grey2     = "#888888";
        const string Green     = "#27ae60";
        const string White     = "#ffffff";
        const string LightGrey = "#f5f5f5";
        const string MidGrey   = "#e0e0e0";

        var receiptNumber = order.OrderReference ?? order.Id.ToString()[..8].ToUpper();
        var serviceDate   = order.DeliveredAt ?? DateTimeOffset.UtcNow;

        // Build flat list of shoe lines with their unit prices so we can show subtotals.
        // The booking stores line items, but not per-line prices. We can compute the
        // subtotal per-category by distributing proportionally — simpler: just show
        // quantity and total; individual-line prices aren't stored on the booking.
        var shoeLines = new List<(string Category, string Color, int Qty)>();
        foreach (var l in booking.SneakersLines) shoeLines.Add(("Sneakers", l.Color, l.Quantity));
        foreach (var l in booking.SuedeLines)    shoeLines.Add(("Suede",    l.Color, l.Quantity));
        foreach (var l in booking.NubuckLines)   shoeLines.Add(("Nubuck",   l.Color, l.Quantity));
        foreach (var l in booking.CanvasLines)   shoeLines.Add(("Canvas",   l.Color, l.Quantity));

        var addOnsAmount = booking.SubtotalBeforeDiscountKes
                           - (booking.ShoesSubtotalGrossKes - booking.PromotionalDiscountKes);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(2.2f, Unit.Centimetre);
                page.MarginVertical(2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Content().Column(col =>
                {
                    // ── HEADER ─────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // Left: logo or fallback text
                        row.RelativeItem().Column(c =>
                        {
                            if (logoBytes is { Length: > 0 })
                            {
                                c.Item().MaxHeight(52).Image(logoBytes);
                                c.Item().PaddingTop(3).Text("Shoe Care & Restoration").FontSize(9).FontColor(Grey1);
                            }
                            else
                            {
                                c.Item().Text("FRESHWALK").FontSize(26).Bold().FontColor(Brand);
                                c.Item().Text("Shoe Care & Restoration").FontSize(10).FontColor(Grey1);
                            }
                            c.Item().PaddingTop(2).Text("Nairobi, Kenya").FontSize(9).FontColor(Grey2);
                        });

                        // Right: receipt number + date
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("RECEIPT").FontSize(20).Bold();
                            c.Item().PaddingTop(2).Text($"#{receiptNumber}").FontSize(10).FontColor(Grey1);
                            c.Item().PaddingTop(5).Text(serviceDate.ToString("dd MMMM yyyy")).FontSize(10).FontColor(Grey1);
                        });
                    });

                    col.Item().PaddingVertical(12).LineHorizontal(1.5f).LineColor(Brand);

                    // ── BILLED TO / REFERENCES ─────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILLED TO").FontSize(8).Bold().FontColor(Grey2);
                            c.Item().PaddingTop(4).Text(customer.FullName).FontSize(13).Bold();
                            if (!string.IsNullOrEmpty(customer.Email))
                                c.Item().PaddingTop(2).Text(customer.Email).FontSize(10).FontColor(Grey1);
                            if (!string.IsNullOrEmpty(customer.PhoneNumber))
                                c.Item().PaddingTop(2).Text(customer.PhoneNumber).FontSize(10).FontColor(Grey1);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("ORDER REFERENCE").FontSize(8).Bold().FontColor(Grey2);
                            c.Item().PaddingTop(3).Text(booking.BookingReference).FontSize(10).FontColor(Grey1);
                            c.Item().PaddingTop(8).Text("SERVICE DATE").FontSize(8).Bold().FontColor(Grey2);
                            c.Item().PaddingTop(3).Text(serviceDate.ToString("dd MMM yyyy")).FontSize(10).FontColor(Grey1);
                        });
                    });

                    col.Item().PaddingVertical(14).LineHorizontal(1).LineColor(MidGrey);

                    // ── ITEMS TABLE ────────────────────────────────────────
                    col.Item().Text("ORDER DETAILS").FontSize(9).Bold().FontColor(Grey2);
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Brand).Padding(7)
                                .Text("DESCRIPTION").FontSize(9).Bold().FontColor(Colors.White);
                            h.Cell().Background(Brand).Padding(7).AlignRight()
                                .Text("PAIRS").FontSize(9).Bold().FontColor(Colors.White);
                        });

                        bool alt = false;
                        foreach (var (cat, clr, qty) in shoeLines)
                        {
                            string bg = alt ? LightGrey : White;
                            table.Cell().Background(bg).PaddingVertical(7).PaddingHorizontal(8)
                                .Text($"{cat} — {clr}").FontSize(10);
                            table.Cell().Background(bg).PaddingVertical(7).PaddingHorizontal(8)
                                .AlignRight().Text($"{qty}").FontSize(10);
                            alt = !alt;
                        }

                        if (booking.AddOns.Count > 0)
                        {
                            table.Cell().ColumnSpan(2).Background("#eeeeee").PaddingVertical(7).PaddingHorizontal(8)
                                .Text($"Add-ons: {string.Join(", ", booking.AddOns)}")
                                .FontSize(10).Italic().FontColor(Grey1);
                        }
                    });

                    col.Item().PaddingTop(16).LineHorizontal(1).LineColor(MidGrey);

                    // ── PRICING ────────────────────────────────────────────
                    col.Item().PaddingTop(14).Text("PRICING BREAKDOWN").FontSize(9).Bold().FontColor(Grey2);
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn(2);
                        });

                        void PriceRow(string label, decimal amount, bool saving = false, bool total = false)
                        {
                            var col1 = table.Cell().PaddingVertical(5).PaddingHorizontal(4);
                            var col2 = table.Cell().PaddingVertical(5).PaddingHorizontal(4).AlignRight();
                            var txtAmount = $"{(saving ? "−" : "")}KES {Math.Abs(amount):N0}";
                            if (total)
                            {
                                col1.Text(label).FontSize(13).Bold().FontColor(Brand);
                                col2.Text(txtAmount).FontSize(13).Bold().FontColor(Brand);
                            }
                            else if (saving)
                            {
                                col1.Text(label).FontSize(10).FontColor(Green);
                                col2.Text(txtAmount).FontSize(10).FontColor(Green);
                            }
                            else
                            {
                                col1.Text(label).FontSize(10).FontColor(Grey1);
                                col2.Text(txtAmount).FontSize(10).FontColor(Grey1);
                            }
                        }

                        PriceRow("Shoe cleaning services", booking.ShoesSubtotalGrossKes);
                        foreach (var p in booking.AppliedPromotions)
                            PriceRow($"Category saving — {p.Label} ({p.CategoryKey})", p.AmountSavedKes, saving: true);
                        if (addOnsAmount > 0)
                            PriceRow("Add-on services", addOnsAmount);
                        if (booking.DiscountAmountKes > 0)
                            PriceRow($"Bundle discount ({booking.BundleDiscountPercent * 100:0}% — {booking.TotalPairs} pairs)", booking.DiscountAmountKes, saving: true);

                        table.Cell().ColumnSpan(2).PaddingTop(6).LineHorizontal(1.5f).LineColor(MidGrey);
                        table.Cell().ColumnSpan(2).PaddingBottom(4).Text(string.Empty);
                        PriceRow("TOTAL CHARGED", booking.PriceKes, total: true);
                    });

                    col.Item().PaddingTop(16).LineHorizontal(1).LineColor(MidGrey);

                    // ── PAYMENT ────────────────────────────────────────────
                    col.Item().PaddingTop(14).Text("PAYMENT DETAILS").FontSize(9).Bold().FontColor(Grey2);
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        void InfoRow(string label, string value)
                        {
                            table.Cell().PaddingVertical(4).Text(label).FontSize(9).FontColor(Grey2);
                            table.Cell().PaddingVertical(4).Text(value).FontSize(10).Bold();
                        }

                        InfoRow("Method:", "M-Pesa STK Push");
                        if (!string.IsNullOrEmpty(payment?.MpesaReceipt))
                            InfoRow("M-Pesa Receipt:", payment.MpesaReceipt);
                        if (!string.IsNullOrEmpty(payment?.PayerPhoneNumber))
                            InfoRow("Payer Number:", FormatPhone(payment.PayerPhoneNumber));
                        InfoRow("Amount Paid:", $"KES {booking.PriceKes:N0}");
                        InfoRow("Status:", "Confirmed ✓");
                    });

                    col.Item().PaddingTop(20).LineHorizontal(1).LineColor(MidGrey);

                    // ── CONTACT / FOOTER ───────────────────────────────────
                    col.Item().PaddingTop(16).Column(footer =>
                    {
                        footer.Item().Text("Thank you for choosing Freshwalk!")
                            .FontSize(12).Bold().FontColor(Brand);

                        // Contact grid
                        footer.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Location").FontSize(8).Bold().FontColor(Grey2);
                                c.Item().PaddingTop(2).Text(ContactAddr).FontSize(9).FontColor(Grey1);
                                c.Item().PaddingTop(6).Text("Hours").FontSize(8).Bold().FontColor(Grey2);
                                c.Item().PaddingTop(2).Text(ContactHours).FontSize(9).FontColor(Grey1);
                            });
                            row.ConstantItem(1).Background(MidGrey);
                            row.RelativeItem().PaddingLeft(12).Column(c =>
                            {
                                c.Item().Text("Phone / WhatsApp").FontSize(8).Bold().FontColor(Grey2);
                                c.Item().PaddingTop(2).Text(ContactPhone).FontSize(9).FontColor(Grey1);
                                c.Item().PaddingTop(6).Text("Email").FontSize(8).Bold().FontColor(Grey2);
                                c.Item().PaddingTop(2).Text(ContactEmail).FontSize(9).FontColor(Grey1);
                            });
                        });

                        footer.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(MidGrey);
                        footer.Item().PaddingTop(8).Text(
                            "This receipt is your proof of purchase. " +
                            "All prices are inclusive of applicable charges. " +
                            "Disputes must be raised within 7 days of delivery.")
                            .FontSize(8).FontColor(Grey2).LineHeight(1.5f);
                        footer.Item().PaddingTop(6).Text("Freshwalk Shoe Care  ·  Nairobi, Kenya")
                            .FontSize(8).FontColor(Grey2).Italic();
                    });
                });
            });
        }).GeneratePdf();
    }

    /// <summary>Downloads the Freshwalk logo from Cloudinary once and caches it for the process lifetime.</summary>
    private async Task<byte[]?> FetchLogoBytesAsync(CancellationToken ct)
    {
        if (_logoBytesCache is not null) return _logoBytesCache;
        await _logoLock.WaitAsync(ct);
        try
        {
            if (_logoBytesCache is not null) return _logoBytesCache;
            var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(10);
            _logoBytesCache = await http.GetByteArrayAsync(LogoUrl, ct);
            return _logoBytesCache;
        }
        catch
        {
            return null; // graceful — PDF renders with text fallback
        }
        finally
        {
            _logoLock.Release();
        }
    }

    private static string FormatPhone(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return "—";
        var d = new string(raw.Where(char.IsDigit).ToArray());
        if (d.StartsWith("254") && d.Length == 12)
            return $"+254 {d[3..6]} {d[6..9]} {d[9..]}";
        if (d.StartsWith("0") && d.Length == 10)
            return $"{d[..4]} {d[4..7]} {d[7..]}";
        return raw;
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
