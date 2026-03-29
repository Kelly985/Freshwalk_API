namespace Freshwalk.Domain;

/// <summary>Authoritative pickup/delivery OTP for an order (synced with <see cref="Order"/> display fields).</summary>
public class OrderOtpCode
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    /// <summary>Pickup or Delivery.</summary>
    public string OtpKind { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Order Order { get; set; } = null!;
}
