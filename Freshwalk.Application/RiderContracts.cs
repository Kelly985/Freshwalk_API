using Freshwalk.Domain;

namespace Freshwalk.Application;

public record RiderOrderListItemDto(
    Guid OrderId,
    Guid BookingId,
    string BookingReference,
    string? OrderReference,
    OrderStatus OrderStatus,
    AssignmentType AssignmentType,
    Guid AssignmentId,
    string PickupAddress,
    string? PickupLocationUrl,
    DateTimeOffset BookingCreatedAt,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? PayerPhoneNumber);

public record RiderOrderDetailDto(
    Guid OrderId,
    Guid BookingId,
    string BookingReference,
    string? OrderReference,
    OrderStatus OrderStatus,
    AssignmentType CurrentAssignmentType,
    Guid AssignmentId,
    string PickupAddress,
    string? PickupLocationUrl,
    double? PickupLatitude,
    double? PickupLongitude,
    /// <summary>Pickup leg only: OTP the rider shares with the customer.</summary>
    string? PickupOtpForRiderToShare,
    DateTimeOffset? PickupOtpExpiresAt,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? PayerPhoneNumber,
    IReadOnlyList<ShoeLineItemDto> SneakersLines,
    IReadOnlyList<ShoeLineItemDto> SuedeLines,
    IReadOnlyList<ShoeLineItemDto> NubuckLines,
    IReadOnlyList<ShoeLineItemDto> CanvasLines,
    IReadOnlyList<string> AddOns,
    int TotalPairs,
    decimal PriceKes,
    decimal ShoesSubtotalGrossKes,
    decimal PromotionalDiscountKes,
    IReadOnlyList<PromotionAppliedDto> AppliedPromotions,
    decimal SubtotalBeforeDiscountKes,
    decimal BundleDiscountPercent,
    decimal DiscountAmountKes);
