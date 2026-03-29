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
    DateTimeOffset BookingCreatedAt);

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
    string? CustomerPhone,
    IReadOnlyList<ShoeLineItemDto> SneakersLines,
    IReadOnlyList<ShoeLineItemDto> SuedeLines,
    IReadOnlyList<ShoeLineItemDto> NubuckLines,
    IReadOnlyList<ShoeLineItemDto> OfficialLeatherLines,
    IReadOnlyList<string> AddOns,
    int TotalPairs,
    decimal PriceKes);
