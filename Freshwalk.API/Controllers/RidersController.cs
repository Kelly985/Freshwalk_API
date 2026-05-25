using System.Security.Cryptography;
using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/riders")]
[Authorize(Roles = "Agent,Admin")]
public class RidersController(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    ICloudinaryService cloudinary,
    IEmailService email,
    IReferenceCodeIssuer referenceIssuer) : ControllerBase
{
    private static readonly string[] AllowedPhotoExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxPhotoBytes = 10L * 1024 * 1024; // 10 MB

    // ── GET /api/riders ──────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var riders = await db.Users
            .AsNoTracking()
            .Where(u => db.UserRoles.Any(ur =>
                ur.UserId == u.Id &&
                db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Rider")))
            .Include(u => u.RiderProfile)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);

        return Ok(riders.Select(Map));
    }

    // ── POST /api/riders ─────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromForm] CreateRiderRequest request,
        IFormFile? selfieFile,
        IFormFile? idFrontFile,
        IFormFile? idBackFile,
        CancellationToken ct)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return BadRequest(new { message = "An account with this email already exists." });

        var existing = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, ct);
        if (existing is not null)
            return BadRequest(new { message = "An account with this phone number already exists." });

        // Validate optional files
        if (selfieFile is not null)
        {
            var err = ValidatePhoto(selfieFile, "selfieFile");
            if (err is not null) return BadRequest(new { message = err });
        }
        if (idFrontFile is not null)
        {
            var err = ValidatePhoto(idFrontFile, "idFrontFile");
            if (err is not null) return BadRequest(new { message = err });
        }
        if (idBackFile is not null)
        {
            var err = ValidatePhoto(idBackFile, "idBackFile");
            if (err is not null) return BadRequest(new { message = err });
        }

        // Generate password
        var plainPassword = GeneratePassword();

        // Create user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            CustomerReference = await referenceIssuer.IssueAsync(ReferencePrefixes.Customer),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, plainPassword);
        if (!createResult.Succeeded)
            return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(e => e.Description)) });

        await userManager.AddToRoleAsync(user, "Rider");

        // Upload documents (sequential; if one fails we still create the profile with what succeeded)
        string? selfieUrl = null, selfieId = null;
        string? idFrontUrl = null, idFrontId = null;
        string? idBackUrl = null, idBackId = null;

        const string folder = "freshwalk/riders/documents";

        if (selfieFile is not null)
        {
            await using var s = selfieFile.OpenReadStream();
            var r = await cloudinary.UploadAsync(s, selfieFile.FileName, folder, ct);
            if (r.Success) { selfieUrl = r.SecureUrl; selfieId = r.PublicId; }
        }
        if (idFrontFile is not null)
        {
            await using var s = idFrontFile.OpenReadStream();
            var r = await cloudinary.UploadAsync(s, idFrontFile.FileName, folder, ct);
            if (r.Success) { idFrontUrl = r.SecureUrl; idFrontId = r.PublicId; }
        }
        if (idBackFile is not null)
        {
            await using var s = idBackFile.OpenReadStream();
            var r = await cloudinary.UploadAsync(s, idBackFile.FileName, folder, ct);
            if (r.Success) { idBackUrl = r.SecureUrl; idBackId = r.PublicId; }
        }

        // Create rider profile
        var profile = new RiderProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BikeRegistration = request.BikeRegistration?.Trim() ?? string.Empty,
            IsAvailable = true,
            SelfieUrl = selfieUrl,
            CloudinaryPublicIdSelfie = selfieId,
            IdFrontUrl = idFrontUrl,
            CloudinaryPublicIdIdFront = idFrontId,
            IdBackUrl = idBackUrl,
            CloudinaryPublicIdIdBack = idBackId,
            EmergencyContactName = request.EmergencyContactName?.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone?.Trim(),
            Notes = request.Notes?.Trim()
        };

        db.RiderProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        // Send welcome email (non-fatal — rider is created regardless)
        try
        {
            var html = WelcomeEmail(request.FullName.Trim(), request.Email.Trim(), plainPassword);
            await email.SendAsync(request.Email.Trim(), request.FullName.Trim(),
                "Welcome to Freshwalk — Your Rider Account", html, ct);
        }
        catch
        {
            // Email failure is logged but does not roll back the rider creation
        }

        user.RiderProfile = profile;
        return Ok(Map(user));
    }

    // ── DELETE /api/riders/{id} — deactivate (soft delete) ───────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.RiderProfile)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        user.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string? ValidatePhoto(IFormFile file, string fieldName)
    {
        if (file.Length == 0) return $"{fieldName} cannot be empty.";
        if (file.Length > MaxPhotoBytes) return $"{fieldName} exceeds the 10 MB limit.";
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(ext))
            return $"{fieldName} type '{ext}' is not allowed. Use jpg, png, or webp.";
        return null;
    }

    private static string GeneratePassword()
    {
        const string upper  = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string lower  = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string all    = upper + lower + digits;

        var bytes = RandomNumberGenerator.GetBytes(12);
        var chars = new char[12];
        chars[0] = upper[bytes[0]  % upper.Length];
        chars[1] = lower[bytes[1]  % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        for (var i = 3; i < 12; i++) chars[i] = all[bytes[i] % all.Length];

        // Shuffle using Fisher-Yates
        var rng = RandomNumberGenerator.GetBytes(12);
        for (var i = 11; i > 0; i--)
        {
            var j = rng[i] % (i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static string WelcomeEmail(string name, string email, string password) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a;">
          <div style="background:#1E4332;padding:20px 24px;border-radius:8px 8px 0 0;">
            <h1 style="color:#fff;margin:0;font-size:22px;">Welcome to Freshwalk! 👟</h1>
          </div>
          <div style="border:1px solid #e0e0e0;border-top:none;padding:24px;border-radius:0 0 8px 8px;">
            <p>Hi <strong>{name}</strong>,</p>
            <p>You've been onboarded as a <strong>Freshwalk Rider</strong>. Here are your login credentials:</p>
            <div style="background:#f5f5f5;border-radius:6px;padding:16px;margin:16px 0;">
              <p style="margin:0 0 8px;"><strong>Email:</strong> {email}</p>
              <p style="margin:0;"><strong>Temporary Password:</strong> <code style="background:#e0e0e0;padding:2px 6px;border-radius:4px;font-size:16px;letter-spacing:1px;">{password}</code></p>
            </div>
            <p style="color:#c0392b;font-size:14px;">⚠️ Please <strong>change your password immediately</strong> after your first login via the Rider Portal settings.</p>
            <p>Log in at the Freshwalk Rider Portal using the email and temporary password above.</p>
            <hr style="border:none;border-top:1px solid #eee;margin:20px 0;">
            <p style="font-size:13px;color:#666;">If you have any questions, contact your operations manager.<br>— The Freshwalk Team</p>
          </div>
        </body>
        </html>
        """;

    private static RiderDto Map(ApplicationUser u) => new(
        u.Id,
        u.FullName,
        u.Email ?? "",
        u.PhoneNumber,
        u.Address,
        u.RiderProfile?.BikeRegistration,
        u.RiderProfile?.IsAvailable ?? true,
        u.IsActive,
        u.RiderProfile?.SelfieUrl,
        u.RiderProfile?.IdFrontUrl,
        u.RiderProfile?.IdBackUrl,
        u.RiderProfile?.EmergencyContactName,
        u.RiderProfile?.EmergencyContactPhone,
        u.RiderProfile?.Notes,
        u.CreatedAt);
}
