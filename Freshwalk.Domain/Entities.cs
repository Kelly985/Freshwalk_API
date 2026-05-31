using Microsoft.AspNetCore.Identity;

namespace Freshwalk.Domain;

public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Customer-facing id: KE-yyyyMMddHHmmssfff-#### (UTC + random suffix).</summary>
    public string CustomerReference { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public RiderProfile? RiderProfile { get; set; }
}

public class RiderProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BikeRegistration { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;

    public string? SelfieUrl { get; set; }
    public string? CloudinaryPublicIdSelfie { get; set; }
    public string? IdFrontUrl { get; set; }
    public string? CloudinaryPublicIdIdFront { get; set; }
    public string? IdBackUrl { get; set; }
    public string? CloudinaryPublicIdIdBack { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

/// <summary>One color tier line for a shoe type (stored as JSON inside the per-type column).</summary>
public class ShoeLineItem
{
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

/// <summary>JSON snapshot on <see cref="Booking"/> of promo savings at checkout.</summary>
public class PromotionAppliedSnapshot
{
    public string CategoryKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal AmountSavedKes { get; set; }
}

public class Booking
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }

    /// <summary>Customer-facing booking id: FW-yyyyMMddHHmmssfff-####.</summary>
    public string BookingReference { get; set; } = string.Empty;

    /// <summary>Selected add-on enum names, e.g. Waterproofing, ExpressService (JSON array in DB).</summary>
    public List<string> AddOns { get; set; } = new();

    /// <summary>JSON array of color/quantity objects per shoe category.</summary>
    public List<ShoeLineItem> SneakersLines { get; set; } = new();
    public List<ShoeLineItem> SuedeLines { get; set; } = new();
    public List<ShoeLineItem> NubuckLines { get; set; } = new();
    public List<ShoeLineItem> CanvasLines { get; set; } = new();

    public int TotalPairs { get; set; }

    /// <summary>Sum of catalog shoe line prices × pairs before category promotions (add-ons excluded).</summary>
    public decimal ShoesSubtotalGrossKes { get; set; }

    /// <summary>Line items + add-ons + delivery, before bundle discount.</summary>
    public decimal SubtotalBeforeDiscountKes { get; set; }

    /// <summary>Bundle tier: 0, 0.10, 0.15, or 0.20.</summary>
    public decimal BundleDiscountPercent { get; set; }

    /// <summary>KES removed by bundle discount (subtotal × percent, aligned with final total).</summary>
    public decimal DiscountAmountKes { get; set; }

    /// <summary>KES saved from active shoe-category promotions (add-ons not discounted).</summary>
    public decimal PromotionalDiscountKes { get; set; }

    /// <summary>Snapshot of per-category promo savings at booking time (JSON array).</summary>
    public List<PromotionAppliedSnapshot> AppliedPromotions { get; set; } = new();

    /// <summary>Amount payable after discount (matches Payment.Amount).</summary>
    public decimal PriceKes { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
    public string? PickupLocationUrl { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser Customer { get; set; } = null!;
    public Payment? Payment { get; set; }
    public Order? Order { get; set; }
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }

    /// <summary>Payment reference: PM-yyyyMMddHHmmssfff-####.</summary>
    public string PaymentReference { get; set; } = string.Empty;

    public string? MpesaCheckoutRequestId { get; set; }
    public string? MpesaMerchantRequestId { get; set; }
    public string? MpesaReceipt { get; set; }

    /// <summary>MSISDN used for STK / confirmed in callback (normalized digits).</summary>
    public string? PayerPhoneNumber { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Booking Booking { get; set; } = null!;
}

public class Order
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? AgentId { get; set; }

    /// <summary>Same FW-yyyyMMddHHmmssfff-#### as the parent booking (order id = booking id format).</summary>
    public string? OrderReference { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.PaymentConfirmed;
    public string? PickupOtp { get; set; }
    public DateTimeOffset? PickupOtpExpiresAt { get; set; }
    public DateTimeOffset? PickupOtpVerifiedAt { get; set; }
    public string? DeliveryOtp { get; set; }
    public DateTimeOffset? DeliveryOtpExpiresAt { get; set; }
    public DateTimeOffset? DeliveryOtpVerifiedAt { get; set; }
    public DateTimeOffset? PickedUpAt { get; set; }
    public DateTimeOffset? CleaningStartedAt { get; set; }
    public DateTimeOffset? CleaningCompletedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public ApplicationUser Customer { get; set; } = null!;
    public ICollection<OrderRiderAssignment> RiderAssignments { get; set; } = new List<OrderRiderAssignment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<OrderAuditLog> AuditLogs { get; set; } = new List<OrderAuditLog>();
}

public class OrderRiderAssignment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid RiderId { get; set; }

    /// <summary>Rider assignment reference, e.g. RA-00000001.</summary>
    public string AssignmentReference { get; set; } = string.Empty;

    public AssignmentType AssignmentType { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public ApplicationUser Rider { get; set; } = null!;
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Order? Order { get; set; }
}

public class OrderAuditLog
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ActorId { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Order Order { get; set; } = null!;
    public ApplicationUser? Actor { get; set; }
}

/// <summary>Public-facing shoe category (before/after imagery, theme, display copy).</summary>
public class ShoeServiceCategory
{
    public int Id { get; set; }

    /// <summary>API key matching booking shoe type, e.g. Sneakers, Canvas.</summary>
    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string? Tagline { get; set; }

    /// <summary>Accent hex for cards (e.g. #E31E24).</summary>
    public string? ThemeColorHex { get; set; }

    public string? ImageBeforeUrl { get; set; }
    public string? ImageAfterUrl { get; set; }
    public int SortOrder { get; set; }

    public ICollection<ShoeServiceTierPrice> TierPrices { get; set; } = new List<ShoeServiceTierPrice>();

    public ICollection<ShoeServicePromotion> Promotions { get; set; } = new List<ShoeServicePromotion>();
}

/// <summary>Time-boxed offer on a shoe category (e.g. Easter). Drives catalog badges and checkout math.</summary>
public class ShoeServicePromotion
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    /// <summary>Shown on service cards and checkout, e.g. &quot;Easter&quot;.</summary>
    public string Label { get; set; } = string.Empty;

    public PromotionDiscountKind DiscountKind { get; set; }

    /// <summary>When <see cref="DiscountKind"/> is <see cref="PromotionDiscountKind.PercentOff"/>.</summary>
    public decimal? PercentOff { get; set; }

    /// <summary>When <see cref="DiscountKind"/> is <see cref="PromotionDiscountKind.FixedAmountPerPair"/> — KES off per pair for that category only.</summary>
    public decimal? FixedOffPerPairKes { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Higher runs first when multiple rows match; only the top match applies per category.</summary>
    public int Priority { get; set; }

    public ShoeServiceCategory Category { get; set; } = null!;
}

/// <summary>Per-tier price for a category. Only rows where IsActive=true are shown to customers and used in pricing.</summary>
public class ShoeServiceTierPrice
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    /// <summary>Tier key sent by the booking form — e.g. Adult, Kids. Legacy values (BlackDark, MixedColored, WhiteLight) kept with IsActive=false.</summary>
    public string ColorTierKey { get; set; } = string.Empty;

    public decimal PriceKes { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Only active tiers are returned by the catalog API and used in pricing. Inactive tiers are preserved for historical reference.</summary>
    public bool IsActive { get; set; } = false;

    public ShoeServiceCategory Category { get; set; } = null!;
}

public enum GalleryCategory  { Sneakers = 1, Canvas = 2, Suede = 3, Nubuck = 4 }
public enum GalleryMediaType { Image = 1, Video = 2 }

/// <summary>Before/after media pair uploaded via AgentPortal and served to the public gallery.</summary>
public class GalleryMediaItem
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public GalleryCategory Category { get; set; }
    public GalleryMediaType MediaType { get; set; } = GalleryMediaType.Image;
    public string BeforeUrl { get; set; } = string.Empty;
    public string AfterUrl  { get; set; } = string.Empty;
    public string? CloudinaryPublicIdBefore { get; set; }
    public string? CloudinaryPublicIdAfter  { get; set; }

    /// <summary>When true, this image pair is included in the home hero cycling carousel.</summary>
    public bool IsHero { get; set; } = false;

    /// <summary>Hex accent colour for the gallery card (e.g. #E31E24).</summary>
    public string? AccentColor { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
