namespace Freshwalk.Application;

public record CreateRiderRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string? Address,
    string? BikeRegistration,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes);

public record RiderDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? Address,
    string? BikeRegistration,
    bool IsAvailable,
    bool IsActive,
    string? SelfieUrl,
    string? IdFrontUrl,
    string? IdBackUrl,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    DateTimeOffset CreatedAt);
