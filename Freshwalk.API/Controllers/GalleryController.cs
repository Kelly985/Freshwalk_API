using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/gallery")]
public class GalleryController(AppDbContext db, ICloudinaryService cloudinary) : ControllerBase
{
    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".mp4", ".mov", ".webm"];

    private const long MaxFileSizeBytes = 100L * 1024 * 1024; // 100 MB per file

    // ── GET /api/gallery ─────────────────────────────────────── [AllowAnonymous]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var items = await db.GalleryMediaItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(items.Select(Map));
    }

    // ── GET /api/gallery/hero ─────────────────────────────────── [AllowAnonymous]
    [HttpGet("hero")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHero(CancellationToken ct)
    {
        var items = await db.GalleryMediaItems
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsHero && x.MediaType == GalleryMediaType.Image)
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Ok(items.Select(Map));
    }

    // ── POST /api/gallery/upload ──────────────────────────────── [Authorize Agent,Admin]
    [HttpPost("upload")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadGalleryItemRequest request,
        IFormFile beforeFile,
        IFormFile afterFile,
        CancellationToken ct)
    {
        var beforeErr = ValidateFile(beforeFile, "beforeFile");
        if (beforeErr is not null) return BadRequest(new { message = beforeErr });

        var afterErr = ValidateFile(afterFile, "afterFile");
        if (afterErr is not null) return BadRequest(new { message = afterErr });

        if (!Enum.TryParse<GalleryCategory>(request.Category, ignoreCase: true, out var category))
            return BadRequest(new { message = "Invalid category. Valid values: Sneakers, Canvas, Suede, Nubuck." });

        if (!Enum.TryParse<GalleryMediaType>(request.MediaType, ignoreCase: true, out var mediaType))
            return BadRequest(new { message = "Invalid mediaType. Valid values: Image, Video." });

        var folder = $"freshwalk/gallery/{request.Category.ToLowerInvariant()}";

        await using var beforeStream = beforeFile.OpenReadStream();
        var beforeResult = await cloudinary.UploadAsync(beforeStream, beforeFile.FileName, folder, ct);
        if (!beforeResult.Success)
            return StatusCode(502, new { message = $"Before-file upload failed: {beforeResult.Error}" });

        await using var afterStream = afterFile.OpenReadStream();
        var afterResult = await cloudinary.UploadAsync(afterStream, afterFile.FileName, folder, ct);
        if (!afterResult.Success)
        {
            if (beforeResult.PublicId is not null)
                await cloudinary.DeleteAsync(beforeResult.PublicId, ct);
            return StatusCode(502, new { message = $"After-file upload failed: {afterResult.Error}" });
        }

        var item = new GalleryMediaItem
        {
            Id                       = Guid.NewGuid(),
            Label                    = request.Label.Trim(),
            Category                 = category,
            MediaType                = mediaType,
            BeforeUrl                = beforeResult.SecureUrl!,
            AfterUrl                 = afterResult.SecureUrl!,
            CloudinaryPublicIdBefore = beforeResult.PublicId,
            CloudinaryPublicIdAfter  = afterResult.PublicId,
            IsHero                   = request.IsHero,
            AccentColor              = request.AccentColor?.Trim(),
            SortOrder                = request.SortOrder,
            IsActive                 = true,
            CreatedAt                = DateTimeOffset.UtcNow,
            UpdatedAt                = DateTimeOffset.UtcNow,
        };

        db.GalleryMediaItems.Add(item);
        await db.SaveChangesAsync(ct);

        return Ok(Map(item));
    }

    // ── PUT /api/gallery/{id} ────────────────────────────────── [Authorize Agent,Admin]
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGalleryItemRequest request, CancellationToken ct)
    {
        var item = await db.GalleryMediaItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        if (!Enum.TryParse<GalleryCategory>(request.Category, ignoreCase: true, out var category))
            return BadRequest(new { message = "Invalid category. Valid values: Sneakers, Canvas, Suede, Nubuck." });

        item.Label       = request.Label.Trim();
        item.Category    = category;
        item.IsHero      = request.IsHero;
        item.AccentColor = request.AccentColor?.Trim();
        item.SortOrder   = request.SortOrder;
        item.UpdatedAt   = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(Map(item));
    }

    // ── DELETE /api/gallery/{id} ──────────────────────────────── [Authorize Agent,Admin]
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await db.GalleryMediaItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        if (item.CloudinaryPublicIdBefore is not null)
            await cloudinary.DeleteAsync(item.CloudinaryPublicIdBefore, ct);
        if (item.CloudinaryPublicIdAfter is not null)
            await cloudinary.DeleteAsync(item.CloudinaryPublicIdAfter, ct);

        db.GalleryMediaItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static string? ValidateFile(IFormFile? file, string fieldName)
    {
        if (file is null || file.Length == 0)
            return $"{fieldName} is required and cannot be empty.";
        if (file.Length > MaxFileSizeBytes)
            return $"{fieldName} '{file.FileName}' exceeds the 100 MB limit.";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return $"{fieldName} type '{ext}' is not allowed. Supported: jpg, png, webp, mp4, mov, webm.";
        return null;
    }

    private static GalleryItemDto Map(GalleryMediaItem x) => new(
        x.Id,
        x.Label,
        x.Category.ToString(),
        x.MediaType.ToString(),
        x.BeforeUrl,
        x.AfterUrl,
        x.IsHero,
        x.AccentColor,
        x.SortOrder,
        x.CreatedAt);
}
