namespace Freshwalk.Application;

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadAsync(Stream stream, string fileName, string folder, CancellationToken ct = default);
    Task DeleteAsync(string publicId, CancellationToken ct = default);
}

public record CloudinaryUploadResult(bool Success, string? SecureUrl, string? PublicId, string? Error);

public record UploadGalleryItemRequest(
    string Label,
    string Category,
    string MediaType,
    bool IsHero,
    string? AccentColor,
    int SortOrder);

public record GalleryItemDto(
    Guid Id,
    string Label,
    string Category,
    string MediaType,
    string BeforeUrl,
    string AfterUrl,
    bool IsHero,
    string? AccentColor,
    int SortOrder,
    DateTimeOffset CreatedAt);
