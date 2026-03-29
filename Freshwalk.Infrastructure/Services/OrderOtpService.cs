using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.Infrastructure.Services;

public static class OrderOtpKind
{
    public const string Pickup = "Pickup";
    public const string Delivery = "Delivery";
}

public class OrderOtpService(AppDbContext db)
{
    public static readonly TimeSpan Validity = TimeSpan.FromHours(1);

    public static DateTimeOffset DefaultExpiryUtc() => DateTimeOffset.UtcNow.Add(Validity);

    public async Task UpsertPickupAsync(Guid orderId, string code, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(code);
        await UpsertAsync(orderId, OrderOtpKind.Pickup, normalized, expiresAtUtc, cancellationToken);
    }

    public async Task UpsertDeliveryAsync(Guid orderId, string code, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(code);
        await UpsertAsync(orderId, OrderOtpKind.Delivery, normalized, expiresAtUtc, cancellationToken);
    }

    private async Task UpsertAsync(Guid orderId, string kind, string code, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
    {
        var old = await db.OrderOtpCodes.Where(x => x.OrderId == orderId && x.OtpKind == kind).ToListAsync(cancellationToken);
        db.OrderOtpCodes.RemoveRange(old);
        db.OrderOtpCodes.Add(new OrderOtpCode
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OtpKind = kind,
            Code = code,
            ExpiresAt = expiresAtUtc,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    /// <summary>Validates against <see cref="OrderOtpCode"/> first, then legacy <see cref="Order"/> columns.</summary>
    public async Task<bool> ValidatePickupAsync(Guid orderId, string? submitted, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(submitted);
        if (code.Length == 0) return false;

        var row = await db.OrderOtpCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.OtpKind == OrderOtpKind.Pickup, cancellationToken);
        if (row is not null)
            return row.Code == code && row.ExpiresAt >= DateTimeOffset.UtcNow;

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order?.PickupOtp is null) return false;
        return NormalizeCode(order.PickupOtp) == code && order.PickupOtpExpiresAt >= DateTimeOffset.UtcNow;
    }

    public async Task<bool> ValidateDeliveryAsync(Guid orderId, string? submitted, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(submitted);
        if (code.Length == 0) return false;

        var row = await db.OrderOtpCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.OtpKind == OrderOtpKind.Delivery, cancellationToken);
        if (row is not null)
            return row.Code == code && row.ExpiresAt >= DateTimeOffset.UtcNow;

        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order?.DeliveryOtp is null) return false;
        return NormalizeCode(order.DeliveryOtp) == code && order.DeliveryOtpExpiresAt >= DateTimeOffset.UtcNow;
    }

    private static string NormalizeCode(string? raw) => (raw ?? "").Trim().Replace(" ", "", StringComparison.Ordinal);
}
