using Freshwalk.Application;
using Freshwalk.Domain;
using Microsoft.AspNetCore.Identity;

namespace Freshwalk.API.Configuration;

public static class AuthSeeder
{
    public static async Task SeedDefaultUsersAsync(IServiceProvider services, AuthSeedSettings settings)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var referenceIssuer = scope.ServiceProvider.GetRequiredService<IReferenceCodeIssuer>();

        await EnsureUserAsync(userManager, referenceIssuer, settings.Customer, "Customer");
        await EnsureUserAsync(userManager, referenceIssuer, settings.Agent, "Agent");
        await EnsureUserAsync(userManager, referenceIssuer, settings.Rider, "Rider");
        await EnsureUserAsync(userManager, referenceIssuer, settings.Admin, "Admin");
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        IReferenceCodeIssuer referenceIssuer,
        SeedUser seed,
        string role)
    {
        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password)) return;

        var existing = await userManager.FindByEmailAsync(seed.Email);
        if (existing is not null) return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            CustomerReference = await referenceIssuer.IssueAsync(ReferencePrefixes.Customer),
            FullName = seed.FullName,
            Email = seed.Email,
            UserName = seed.Email,
            PhoneNumber = seed.PhoneNumber,
            Address = seed.Address,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var create = await userManager.CreateAsync(user, seed.Password);
        if (create.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
