namespace Freshwalk.Infrastructure.Authentication;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Freshwalk.API";
    public string Audience { get; set; } = "Freshwalk.Client";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
}
