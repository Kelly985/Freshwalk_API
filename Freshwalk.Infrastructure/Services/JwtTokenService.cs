using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Freshwalk.Infrastructure.Services;

public class JwtTokenService(
    UserManager<ApplicationUser> userManager,
    IReferenceCodeIssuer referenceIssuer,
    IOptions<JwtSettings> jwtOptions) : IJwtTokenService
{
    public async Task<AuthResponse> GenerateForUserAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.CustomerReference))
        {
            user.CustomerReference = await referenceIssuer.IssueAsync(ReferencePrefixes.Customer);
            var updated = await userManager.UpdateAsync(user);
            if (!updated.Succeeded)
                throw new InvalidOperationException(
                    "Could not assign customer id: " + string.Join(" ", updated.Errors.Select(e => e.Description)));
        }

        var roles = await userManager.GetRolesAsync(user);
        var settings = jwtOptions.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("full_name", user.FullName),
            new("customer_ref", user.CustomerReference)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            user.Email ?? string.Empty,
            user.FullName,
            user.CustomerReference,
            roles);
    }
}
