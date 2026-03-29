namespace Freshwalk.API.Configuration;

public class AuthSeedSettings
{
    public SeedUser Customer { get; set; } = new();
    public SeedUser Agent { get; set; } = new();
    public SeedUser Rider { get; set; } = new();
    public SeedUser Admin { get; set; } = new();
}

public class SeedUser
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Address { get; set; }
}
