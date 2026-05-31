using Freshwalk.Domain;

namespace Freshwalk.Application;

public record AgentBookingListItemDto(
    Guid Id,
    string BookingReference,
    string? OrderReference,
    Guid? OrderId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string CustomerReference,
    BookingStatus BookingStatus,
    PaymentStatus? PaymentStatus,
    OrderStatus? OrderStatus,
    decimal PriceKes,
    string PickupAddress,
    string? PickupLocationUrl,
    DateTimeOffset CreatedAt);

public record AgentRiderAssignmentDto(
    Guid AssignmentId,
    string AssignmentReference,
    string AssignmentType,
    string Status,
    Guid RiderId,
    string RiderName,
    string? RiderEmail,
    string? RiderPhone);

public record AgentBookingDetailDto(
    Guid Id,
    string BookingReference,
    string? PaymentReference,
    string? OrderReference,
    Guid? OrderId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string CustomerReference,
    Guid CustomerId,
    BookingStatus BookingStatus,
    PaymentStatus? PaymentStatus,
    decimal? PaymentAmountKes,
    OrderStatus? OrderStatus,
    decimal PriceKes,
    decimal ShoesSubtotalGrossKes,
    decimal PromotionalDiscountKes,
    IReadOnlyList<PromotionAppliedDto> AppliedPromotions,
    decimal SubtotalBeforeDiscountKes,
    decimal BundleDiscountPercent,
    decimal DiscountAmountKes,
    int TotalPairs,
    string PickupAddress,
    string? PickupLocationUrl,
    double? PickupLatitude,
    double? PickupLongitude,
    IReadOnlyList<string> AddOns,
    IReadOnlyList<ShoeLineItemDto> SneakersLines,
    IReadOnlyList<ShoeLineItemDto> SuedeLines,
    IReadOnlyList<ShoeLineItemDto> NubuckLines,
    IReadOnlyList<ShoeLineItemDto> CanvasLines,
    IReadOnlyList<AgentRiderAssignmentDto> RiderAssignments,
    DateTimeOffset CreatedAt);

public record AgentUpdateBookingStatusRequest(string Status);

public record AgentCustomerRowDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string CustomerReference,
    DateTimeOffset CreatedAt,
    int BookingsCount);

public record AgentRiderRowDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? BikeRegistration,
    bool IsAvailable);

public record AgentReportSummaryDto(
    int TotalBookings,
    int PendingPaymentCount,
    int PaymentConfirmedBookingsCount,
    int ActiveOrdersCount,
    int DeliveredOrdersCount,
    decimal RevenueKesConfirmedPayments);

// ── Promotions ────────────────────────────────────────────────────────────────

public record PromotionDto(
    int Id,
    int CategoryId,
    string CategoryKey,
    string CategoryDisplayName,
    string? ThemeColorHex,
    string Label,
    string DiscountKind,
    decimal? PercentOff,
    decimal? FixedOffPerPairKes,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    int Priority);

/// <summary>
/// Pass <c>CategoryKey = null</c> to apply the promotion to every shoe category at once.
/// </summary>
public record CreatePromotionRequest(
    string? CategoryKey,
    string Label,
    string DiscountKind,
    decimal? PercentOff,
    decimal? FixedOffPerPairKes,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    int Priority);

public record UpdatePromotionRequest(
    string Label,
    string DiscountKind,
    decimal? PercentOff,
    decimal? FixedOffPerPairKes,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    int Priority);
