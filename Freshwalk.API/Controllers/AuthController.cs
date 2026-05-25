using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Authentication;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IReferenceCodeIssuer referenceIssuer,
    IConfiguration configuration,
    IOptions<JwtSettings> jwtOptions) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token")]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        var creds = configuration.GetSection("ApiCredentials");
        if (request.Username != creds["Username"] || request.Password != creds["Password"])
            return Unauthorized(new { message = "Invalid API credentials." });

        var settings = jwtOptions.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var signingCreds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "freshwalk-api-client"),
            new(ClaimTypes.NameIdentifier, "freshwalk-api-client"),
            new(ClaimTypes.Name, request.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, "ApiClient")
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCreds);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            tokenType = "Bearer"
        });
    }

    [Authorize]
    [HttpGet("validate")]
    public IActionResult ValidateToken()
    {
        return Ok(new
        {
            valid = true,
            user = User.Identity?.Name,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var exists = await userManager.FindByEmailAsync(request.Email);
        if (exists is not null) return BadRequest(new { message = "Email is already in use." });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            CustomerReference = await referenceIssuer.IssueAsync(ReferencePrefixes.Customer),
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded) return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(e => e.Description)) });

        await userManager.AddToRoleAsync(user, "Customer");
        return Ok(new { message = "Account created successfully." });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return Unauthorized(new { message = "Invalid credentials." });

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials." });

        var token = await jwtTokenService.GenerateForUserAsync(user);
        return Ok(token);
    }

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> GoogleSignIn([FromBody] GoogleTokenRequest request)
    {
        var googleClientId = configuration["Google:ClientId"];

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { googleClientId } };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch
        {
            return Unauthorized(new { message = "Invalid Google token." });
        }

        var user = await userManager.FindByEmailAsync(payload.Email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                CustomerReference = await referenceIssuer.IssueAsync(ReferencePrefixes.Customer),
                FullName = payload.Name ?? payload.Email,
                Email = payload.Email,
                UserName = payload.Email,
                EmailConfirmed = payload.EmailVerified,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return BadRequest(new { message = string.Join(" ", createResult.Errors.Select(e => e.Description)) });

            await userManager.AddToRoleAsync(user, "Customer");
        }

        var token = await jwtTokenService.GenerateForUserAsync(user);
        return Ok(token);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return Ok(new { message = "Password changed successfully." });
    }
}

public record TokenRequest(string Username, string Password);
public record GoogleTokenRequest(string IdToken);
