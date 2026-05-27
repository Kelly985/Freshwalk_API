using Freshwalk.Application;
using Freshwalk.Domain;
using System.Text;

namespace Freshwalk.API;

/// <summary>Shared HTML email builders. All styles are inline (email-client compatible).</summary>
internal static class EmailTemplates
{
    private const string Brand      = "#E31E24";
    private const string LogoUrl    = "https://res.cloudinary.com/dlvaqdgqe/image/upload/v1779909319/FRESHWALK_LOGO_yndm3w.png";
    private const string Phone      = "+254 (0) 118 081 414";
    private const string PhoneTel   = "+254118081414";
    private const string SupportEmail = "freshwalkshoecare@gmail.com";
    private const string Address    = "J&apos;s Arcade, Thome Road, Nairobi, Kenya";
    private const string Hours      = "Mon–Sat · 8:00 AM – 7:00 PM";

    // ──────────────────────────────────────────────────────────────────────────
    // Email 1 — Order acknowledgment (sent immediately at booking time)
    // ──────────────────────────────────────────────────────────────────────────
    public static (string Subject, string Html) OrderConfirmation(
        string customerName,
        string bookingReference,
        string pickupAddress,
        IEnumerable<LineBreakdownDto> lineBreakdowns,
        IEnumerable<string> addOns,
        decimal shoesSubtotalGross,
        decimal promotionalDiscount,
        IEnumerable<PromotionAppliedSnapshot> appliedPromotions,
        decimal subtotalBeforeDiscount,
        decimal bundleDiscountPercent,
        decimal discountAmount,
        decimal totalKes)
    {
        var subject = $"Order Received – {bookingReference}";

        var items = BuildItemsTable(lineBreakdowns);

        var addOnRows = addOns.Any()
            ? $"<tr><td colspan='3' style='padding:8px 16px;border-bottom:1px solid #f0f0f0;font-size:13px;color:#777;font-style:italic;'>Add-ons: {string.Join(", ", addOns)}</td></tr>"
            : "";

        var promoRows = new StringBuilder();
        foreach (var p in appliedPromotions)
        {
            promoRows.Append($"<tr><td style='padding:4px 0;color:#27ae60;font-size:13px;'>✓ {p.Label} ({p.CategoryKey})</td><td style='padding:4px 0;color:#27ae60;font-size:13px;text-align:right;'>−KES {p.AmountSavedKes:N0}</td></tr>");
        }

        var bundleRow = bundleDiscountPercent > 0
            ? $"<tr><td style='padding:4px 0;color:#27ae60;font-size:13px;'>Bundle discount ({bundleDiscountPercent * 100:0}%)</td><td style='padding:4px 0;color:#27ae60;font-size:13px;text-align:right;'>−KES {discountAmount:N0}</td></tr>"
            : "";

        var html = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;font-family:Helvetica Neue,Helvetica,Arial,sans-serif;background:#f4f4f4;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f4;padding:32px 0;'>
<tr><td align='center'>
<table width='600' cellpadding='0' cellspacing='0' style='background:#fff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>

{Header()}

<!-- BODY -->
<tr><td style='padding:36px 40px 20px;'>
  <h2 style='color:#222;margin:0 0 6px;font-size:22px;'>Order Received! 🎉</h2>
  <p style='color:#555;margin:0 0 28px;font-size:15px;line-height:1.6;'>
    Hi {Esc(customerName)}, your order has been received! One of our riders will be in touch shortly to collect your shoes.
    <strong>Payment is made conveniently via M-Pesa at delivery</strong> — no upfront payment needed.
  </p>

  <!-- Reference box -->
  <div style='background:#f9f9f9;border:1px solid #eee;border-radius:8px;padding:16px 20px;margin-bottom:28px;'>
    <p style='margin:0 0 4px;font-size:11px;color:#999;text-transform:uppercase;letter-spacing:0.8px;'>Booking Reference</p>
    <p style='margin:0;font-size:22px;font-weight:700;color:{Brand};letter-spacing:1px;font-family:Courier New,monospace;'>{Esc(bookingReference)}</p>
    <p style='margin:6px 0 0;font-size:12px;color:#999;'>Use this code to track your order in the Freshwalk app.</p>
  </div>

  <!-- Items -->
  <h3 style='color:#222;margin:0 0 10px;font-size:15px;font-weight:700;text-transform:uppercase;letter-spacing:0.4px;'>Order Items</h3>
  <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:20px;border:1px solid #eee;border-radius:6px;overflow:hidden;'>
    <tr style='background:#f5f5f5;'>
      <td style='padding:8px 16px;font-size:11px;font-weight:700;color:#999;text-transform:uppercase;letter-spacing:0.5px;'>Item</td>
      <td style='padding:8px 16px;font-size:11px;font-weight:700;color:#999;text-transform:uppercase;letter-spacing:0.5px;text-align:center;'>Pairs</td>
      <td style='padding:8px 16px;font-size:11px;font-weight:700;color:#999;text-transform:uppercase;letter-spacing:0.5px;text-align:right;'>Subtotal</td>
    </tr>
    {items}
    {addOnRows}
  </table>

  <!-- Pricing -->
  <h3 style='color:#222;margin:0 0 10px;font-size:15px;font-weight:700;text-transform:uppercase;letter-spacing:0.4px;'>Pricing Breakdown</h3>
  <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:24px;'>
    <tr><td style='padding:5px 0;color:#555;font-size:13px;'>Shoe cleaning (list price)</td><td style='padding:5px 0;color:#555;font-size:13px;text-align:right;'>KES {shoesSubtotalGross:N0}</td></tr>
    {promoRows}
    {bundleRow}
    <tr><td colspan='2' style='padding:6px 0;border-top:1px solid #eee;'></td></tr>
    <tr><td style='padding:6px 0;font-size:16px;font-weight:700;color:#222;'>Total Due at Delivery</td>
        <td style='padding:6px 0;font-size:16px;font-weight:700;color:{Brand};text-align:right;'>KES {totalKes:N0}</td></tr>
  </table>

  <!-- Pickup address -->
  <div style='background:#f9f9f9;border-left:4px solid {Brand};padding:12px 16px;margin-bottom:28px;border-radius:0 6px 6px 0;'>
    <p style='margin:0 0 4px;font-size:11px;color:#999;text-transform:uppercase;letter-spacing:0.8px;'>Pickup Address</p>
    <p style='margin:0;font-size:14px;color:#333;'>{Esc(pickupAddress)}</p>
  </div>

  <!-- What happens next -->
  <h3 style='color:#222;margin:0 0 14px;font-size:15px;font-weight:700;text-transform:uppercase;letter-spacing:0.4px;'>What happens next?</h3>
  {Step("1", "Rider on the way", "We&apos;ll send one of our verified riders to your pickup address to collect your shoes.")}
  {Step("2", "Share the pickup code", "The rider will give you a <strong>6-digit pickup code</strong>. Open <strong>Freshwalk → My Bookings</strong> and enter it to confirm the handover.")}
  {Step("3", "Expert cleaning", "Your shoes are brought to our studio for professional care — typically 24–48 hours.")}
  {Step("4", "Pay &amp; receive", "A rider brings your freshly cleaned shoes back. You&apos;ll pay via M-Pesa STK Push at the door before handing over your <strong>delivery code</strong>.")}

  <!-- OTP warning box -->
  <div style='background:#fff8e1;border:1px solid #ffe082;border-radius:8px;padding:16px 20px;margin-top:24px;'>
    <p style='margin:0;font-size:14px;color:#5d4037;line-height:1.6;'>
      📱 <strong>Important:</strong> When the rider arrives, they will show you a 6-digit code.
      Always verify this code in the <strong>Freshwalk app (My Bookings)</strong> before handing over your shoes.
      Do <em>not</em> hand over shoes without confirming the code.
    </p>
  </div>
</td></tr>

{Footer()}
</table>
</td></tr>
</table>
</body>
</html>";

        return (subject, html);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Email 2 — Shoes ready for delivery + delivery OTP
    // ──────────────────────────────────────────────────────────────────────────
    public static (string Subject, string Html) DeliveryReady(
        string customerName,
        string bookingReference,
        string orderReference,
        string deliveryOtp,
        string pickupAddress,
        IEnumerable<ShoeLineItem> sneakers,
        IEnumerable<ShoeLineItem> suede,
        IEnumerable<ShoeLineItem> nubuck,
        IEnumerable<ShoeLineItem> canvas,
        decimal totalKes)
    {
        var subject = $"Your Shoes Are Ready for Delivery – {bookingReference}";

        var items = BuildItemsList(sneakers, "Sneakers")
                  + BuildItemsList(suede, "Suede")
                  + BuildItemsList(nubuck, "Nubuck")
                  + BuildItemsList(canvas, "Canvas");

        var html = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;font-family:Helvetica Neue,Helvetica,Arial,sans-serif;background:#f4f4f4;'>
<table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f4f4;padding:32px 0;'>
<tr><td align='center'>
<table width='600' cellpadding='0' cellspacing='0' style='background:#fff;border-radius:10px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);'>

{Header()}

<!-- BODY -->
<tr><td style='padding:36px 40px 20px;'>
  <h2 style='color:#222;margin:0 0 6px;font-size:22px;'>Your Shoes Are Ready! ✨</h2>
  <p style='color:#555;margin:0 0 28px;font-size:15px;line-height:1.6;'>
    Hi {Esc(customerName)}, great news — your shoes have been expertly cleaned and a rider is on their way to deliver them to you!
  </p>

  <!-- Delivery OTP — prominent -->
  <div style='background:{Brand};border-radius:10px;padding:24px;text-align:center;margin-bottom:28px;'>
    <p style='color:rgba(255,255,255,0.85);margin:0 0 8px;font-size:12px;letter-spacing:1px;text-transform:uppercase;'>Your Delivery Code</p>
    <p style='color:#fff;margin:0;font-size:42px;font-weight:800;letter-spacing:8px;font-family:Courier New,monospace;'>{Esc(deliveryOtp)}</p>
    <p style='color:rgba(255,255,255,0.80);margin:10px 0 0;font-size:13px;'>Share this with the rider when they arrive</p>
  </div>

  <!-- What to do -->
  <h3 style='color:#222;margin:0 0 14px;font-size:15px;font-weight:700;text-transform:uppercase;letter-spacing:0.4px;'>When the rider arrives</h3>
  {Step("1", "Payment first", "The rider will send you an M-Pesa payment prompt — complete it before accepting your shoes.")}
  {Step("2", "Share your code", "Show or tell the rider your <strong>6-digit delivery code</strong> above. They&apos;ll enter it in their app to complete delivery.")}
  {Step("3", "Inspect &amp; enjoy", "Check your shoes before the rider leaves. All good? Enjoy your freshened kicks!")}

  <!-- Warning box -->
  <div style='background:#fff8e1;border:1px solid #ffe082;border-radius:8px;padding:16px 20px;margin:24px 0 28px;'>
    <p style='margin:0;font-size:14px;color:#5d4037;line-height:1.6;'>
      🔒 <strong>Keep this code safe.</strong> Only share it with the Freshwalk rider after payment is confirmed.
      The code is also visible in <strong>Freshwalk → My Bookings</strong>.
    </p>
  </div>

  <!-- Order summary -->
  <h3 style='color:#222;margin:0 0 10px;font-size:15px;font-weight:700;text-transform:uppercase;letter-spacing:0.4px;'>Order Summary</h3>
  <div style='background:#f9f9f9;border:1px solid #eee;border-radius:6px;padding:4px 0;margin-bottom:16px;'>
    {items}
  </div>

  <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:8px;'>
    <tr>
      <td style='padding:4px 0;font-size:13px;color:#999;'>Booking ref</td>
      <td style='padding:4px 0;font-size:13px;color:#555;text-align:right;font-family:Courier New,monospace;'>{Esc(bookingReference)}</td>
    </tr>
    <tr>
      <td style='padding:4px 0;font-size:13px;color:#999;'>Delivery address</td>
      <td style='padding:4px 0;font-size:13px;color:#555;text-align:right;'>{Esc(pickupAddress)}</td>
    </tr>
    <tr>
      <td colspan='2' style='padding:8px 0;border-top:1px solid #eee;'></td>
    </tr>
    <tr>
      <td style='font-size:15px;font-weight:700;color:#222;'>Total Due at Delivery</td>
      <td style='font-size:15px;font-weight:700;color:{Brand};text-align:right;'>KES {totalKes:N0}</td>
    </tr>
  </table>
</td></tr>

{Footer()}
</table>
</td></tr>
</table>
</body>
</html>";

        return (subject, html);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Shared structural blocks
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Logo on white + red brand accent bar. Shared by all emails.</summary>
    private static string Header() => $@"
<!-- LOGO -->
<tr><td style='background:#ffffff;padding:28px 40px 16px;text-align:center;'>
  <img src='{LogoUrl}' alt='Freshwalk' width='160' height='auto'
       style='display:block;margin:0 auto;max-width:160px;border:0;'>
</td></tr>
<!-- RED ACCENT BAR -->
<tr><td style='background:{Brand};padding:9px 40px;text-align:center;'>
  <span style='color:rgba(255,255,255,0.90);font-size:11px;letter-spacing:1.4px;font-weight:700;font-family:Helvetica Neue,Arial,sans-serif;'>
    SHOE CARE &amp; RESTORATION &nbsp;·&nbsp; NAIROBI, KENYA
  </span>
</td></tr>";

    private static string Footer() => $@"
<tr><td style='background:#f9f9f9;padding:24px 40px 28px;border-top:1px solid #eee;'>
  <!-- Contact grid -->
  <table width='100%' cellpadding='0' cellspacing='0' style='margin-bottom:16px;'>
    <tr>
      <td valign='top' style='width:50%;padding-right:16px;'>
        <p style='margin:0 0 3px;font-size:12px;font-weight:700;color:#555;'>📍 Location</p>
        <p style='margin:0 0 10px;font-size:12px;color:#888;line-height:1.5;'>{Address}</p>
        <p style='margin:0 0 3px;font-size:12px;font-weight:700;color:#555;'>🕐 Hours</p>
        <p style='margin:0;font-size:12px;color:#888;'>{Hours}</p>
      </td>
      <td valign='top' style='width:50%;padding-left:16px;border-left:1px solid #e8e8e8;'>
        <p style='margin:0 0 3px;font-size:12px;font-weight:700;color:#555;'>📞 Phone / WhatsApp</p>
        <p style='margin:0 0 10px;font-size:12px;'>
          <a href='tel:{PhoneTel}' style='color:{Brand};text-decoration:none;font-weight:600;'>{Phone}</a>
        </p>
        <p style='margin:0 0 3px;font-size:12px;font-weight:700;color:#555;'>✉️ Email</p>
        <p style='margin:0;font-size:12px;'>
          <a href='mailto:{SupportEmail}' style='color:{Brand};text-decoration:none;font-weight:600;'>{SupportEmail}</a>
        </p>
      </td>
    </tr>
  </table>
  <p style='margin:12px 0 0;font-size:11px;color:#bbb;text-align:center;'>
    Freshwalk Shoe Care &nbsp;·&nbsp; Nairobi, Kenya &nbsp;·&nbsp; freshwalk.biz
  </p>
</td></tr>";

    // ──────────────────────────────────────────────────────────────────────────
    // Item row helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Builds item rows with 3 columns: description, quantity, KES subtotal.</summary>
    private static string BuildItemsTable(IEnumerable<LineBreakdownDto> lines)
    {
        var sb = new StringBuilder();
        bool alt = false;
        foreach (var line in lines)
        {
            var bg = alt ? "#f9f9f9" : "#ffffff";
            var subtotal = line.UnitPrice * line.Quantity;
            sb.Append($@"<tr style='background:{bg};'>
  <td style='padding:10px 16px;border-bottom:1px solid #f0f0f0;font-size:14px;color:#333;'>{Esc(line.Category)} — {Esc(line.Color)}</td>
  <td style='padding:10px 16px;border-bottom:1px solid #f0f0f0;font-size:13px;color:#777;text-align:center;'>{line.Quantity} pair{(line.Quantity != 1 ? "s" : "")}</td>
  <td style='padding:10px 16px;border-bottom:1px solid #f0f0f0;font-size:14px;color:#333;font-weight:600;text-align:right;'>KES {subtotal:N0}</td>
</tr>");
            alt = !alt;
        }
        return sb.ToString();
    }

    /// <summary>Builds simple 2-column item rows used in Email 2 (delivery ready).</summary>
    private static string BuildItemsList(IEnumerable<ShoeLineItem> lines, string category)
    {
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            sb.Append($"<tr><td style='padding:10px 16px;border-bottom:1px solid #f0f0f0;font-size:14px;color:#333;'>{Esc(category)} — {Esc(line.Color)}</td><td style='padding:10px 16px;border-bottom:1px solid #f0f0f0;font-size:14px;color:#555;text-align:right;'>{line.Quantity} pair{(line.Quantity != 1 ? "s" : "")}</td></tr>");
        }
        return sb.ToString();
    }

    private static string Step(string num, string title, string body) =>
        $@"<table cellpadding='0' cellspacing='0' width='100%' style='margin-bottom:14px;'>
          <tr>
            <td valign='top' style='width:32px;'>
              <div style='width:26px;height:26px;border-radius:50%;background:{Brand};color:#fff;font-size:12px;font-weight:700;text-align:center;line-height:26px;'>{Esc(num)}</div>
            </td>
            <td valign='top' style='padding-left:10px;'>
              <p style='margin:0 0 2px;font-size:14px;font-weight:700;color:#222;'>{Esc(title)}</p>
              <p style='margin:0;font-size:13px;color:#666;line-height:1.5;'>{body}</p>
            </td>
          </tr>
        </table>";

    private static string Esc(string? s) =>
        (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
