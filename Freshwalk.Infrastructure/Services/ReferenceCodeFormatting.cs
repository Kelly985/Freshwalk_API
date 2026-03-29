namespace Freshwalk.Infrastructure.Services;

/// <summary>M-Pesa AccountReference is short; derive a stable 12-char value from the full booking reference.</summary>
public static class ReferenceCodeFormatting
{
    public static string ForMpesaAccountReference(string bookingReference, Guid bookingIdFallback)
    {
        var digits = new string(bookingReference.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0)
            return bookingIdFallback.ToString("N")[..12];
        if (digits.Length >= 12)
            return digits[^12..];
        return digits.PadLeft(12, '0');
    }
}
