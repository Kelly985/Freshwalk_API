using System.Globalization;
using System.Security.Cryptography;
using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Freshwalk.Infrastructure.Services;

/// <summary>Issues codes like KE-20260328143052001-7382 (prefix, UTC timestamp with ms, 4-digit crypto random).</summary>
public class ReferenceCodeIssuer(AppDbContext db, ILogger<ReferenceCodeIssuer> log) : IReferenceCodeIssuer
{
    private const int MaxAttempts = 64;

    public async Task<string> IssueAsync(string prefix, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length > 6)
            throw new ArgumentException("Invalid reference prefix.", nameof(prefix));

        prefix = prefix.Trim().ToUpperInvariant();

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = BuildCandidate(prefix);
            if (!await ExistsAsync(prefix, code, cancellationToken))
                return code;

            log.LogDebug("Reference collision for {Prefix}, retry {Attempt}", prefix, attempt + 1);
        }

        throw new InvalidOperationException($"Could not issue a unique reference for prefix {prefix} after {MaxAttempts} attempts.");
    }

    private static string BuildCandidate(string prefix)
    {
        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        var n = RandomNumberGenerator.GetInt32(0, 10_000);
        return $"{prefix}-{ts}-{n:D4}";
    }

    private Task<bool> ExistsAsync(string prefix, string code, CancellationToken cancellationToken)
    {
        return prefix switch
        {
            ReferencePrefixes.Customer => db.Users.AnyAsync(u => u.CustomerReference == code, cancellationToken),
            ReferencePrefixes.Booking => db.Bookings.AnyAsync(b => b.BookingReference == code, cancellationToken),
            ReferencePrefixes.Payment => db.Payments.AnyAsync(p => p.PaymentReference == code, cancellationToken),
            ReferencePrefixes.RiderAssignment => db.OrderRiderAssignments.AnyAsync(
                a => a.AssignmentReference == code,
                cancellationToken),
            _ => throw new ArgumentException($"Unknown reference prefix: {prefix}", nameof(prefix))
        };
    }
}
