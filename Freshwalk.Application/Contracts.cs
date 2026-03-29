using Freshwalk.Domain;

namespace Freshwalk.Application;

public record RegisterRequest(string FullName, string Email, string PhoneNumber, string Password, string? Address);
public record LoginRequest(string Email, string Password);
public record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string Email,
    string FullName,
    string CustomerReference,
    IEnumerable<string> Roles);

public record BookingItemDto(string ShoeType, string ColorTier, int PairCount);

public record ShoeLineItemDto(string Color, int Quantity);

public record CreateBookingRequest(
    List<BookingItemDto> Items,
    List<string>? AddOns,
    string PickupAddress,
    double? PickupLatitude,
    double? PickupLongitude);

public record PricingBreakdown(
    decimal SubtotalBeforeDiscountKes,
    decimal BundleDiscountPercent,
    decimal DiscountAmountKes,
    decimal FinalPriceKes);

public record BookingResponse(
    Guid Id,
    string CustomerReference,
    string BookingReference,
    string? OrderReference,
    decimal PriceKes,
    decimal SubtotalBeforeDiscountKes,
    decimal BundleDiscountPercent,
    decimal DiscountAmountKes,
    int TotalPairs,
    BookingStatus Status,
    DateTimeOffset CreatedAt,
    string PickupAddress,
    string? PickupLocationUrl,
    IReadOnlyList<string> AddOns,
    IReadOnlyList<ShoeLineItemDto> SneakersLines,
    IReadOnlyList<ShoeLineItemDto> SuedeLines,
    IReadOnlyList<ShoeLineItemDto> NubuckLines,
    IReadOnlyList<ShoeLineItemDto> OfficialLeatherLines);

/// <summary>Customer-facing booking list for order tracking (payment + fulfilment).</summary>
public record CustomerBookingTrackingDto(
    Guid Id,
    string BookingReference,
    string? OrderReference,
    string? PaymentReference,
    string CustomerReference,
    decimal PriceKes,
    BookingStatus BookingStatus,
    PaymentStatus? PaymentStatus,
    decimal? PaymentAmountKes,
    OrderStatus? OrderStatus,
    Guid? OrderId,
    DateTimeOffset CreatedAt,
    string PickupAddress,
    string? PickupLocationUrl,
    /// <summary>True when rider is assigned for pickup; customer enters OTP from the rider to confirm handoff.</summary>
    bool CanEnterPickupOtp,
    /// <summary>Shown only when order is out for delivery; customer shares this with the rider who confirms in the rider app.</summary>
    string? DeliveryOtpForCustomer);

/// <summary>Provide either <see cref="BookingId"/> or <see cref="BookingReference"/> (e.g. FW-20260328145632001-9043).</summary>
public record InitiateBookingStkRequest(string PhoneNumber, Guid? BookingId, string? BookingReference);

public interface IJwtTokenService
{
    Task<AuthResponse> GenerateForUserAsync(ApplicationUser user);
}

public interface IPricingService
{
    PricingBreakdown CalculateBreakdown(List<BookingItemDto> items, IReadOnlyList<string> addOns);
}
