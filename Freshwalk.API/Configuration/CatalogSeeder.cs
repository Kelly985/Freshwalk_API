using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Configuration;

/// <summary>Seeds shoe service categories and tier prices when the catalog is empty (editable in DB afterward).</summary>
public static class CatalogSeeder
{
    public static async Task EnsureShoeCatalogAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.ShoeServiceCategories.AnyAsync())
            return;

        var categories = new List<ShoeServiceCategory>
        {
            new()
            {
                Key = "Sneakers",
                DisplayName = "Sneaker cleaning & care",
                Tagline = "Deep clean · sole whitening · deodorizing",
                ThemeColorHex = "#E31E24",
                ImageBeforeUrl = "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=640&q=80",
                ImageAfterUrl = "https://images.unsplash.com/photo-1608231387042-66d1773070a5?w=640&q=80",
                SortOrder = 1,
                TierPrices =
                [
                    new ShoeServiceTierPrice { ColorTierKey = "Adult", PriceKes = 300, SortOrder = 1, IsActive = true },
                    new ShoeServiceTierPrice { ColorTierKey = "Kids",  PriceKes = 250, SortOrder = 2, IsActive = true }
                ]
            },
            new()
            {
                Key = "Suede",
                DisplayName = "Suede shoe cleaning & care",
                Tagline = "Nap revival · color correction · deodorizing",
                ThemeColorHex = "#00ADEF",
                ImageBeforeUrl = "https://images.unsplash.com/photo-1551107696-a4b0c5a0d9a2?w=640&q=80",
                ImageAfterUrl = "https://images.unsplash.com/photo-1595950653106-6c9ebd614d3a?w=640&q=80",
                SortOrder = 2,
                TierPrices =
                [
                    new ShoeServiceTierPrice { ColorTierKey = "Adult", PriceKes = 300, SortOrder = 1, IsActive = true },
                    new ShoeServiceTierPrice { ColorTierKey = "Kids",  PriceKes = 250, SortOrder = 2, IsActive = true }
                ]
            },
            new()
            {
                Key = "Nubuck",
                DisplayName = "Nubuck shoe care",
                Tagline = "Texture-safe cleaning · nap restoration",
                ThemeColorHex = "#1D2B59",
                ImageBeforeUrl = "https://images.unsplash.com/photo-1533867617858-e7b97e060509?w=640&q=80",
                ImageAfterUrl = "https://images.unsplash.com/photo-1582897085656-c636d006a246?w=640&q=80",
                SortOrder = 3,
                TierPrices =
                [
                    new ShoeServiceTierPrice { ColorTierKey = "Adult", PriceKes = 300, SortOrder = 1, IsActive = true },
                    new ShoeServiceTierPrice { ColorTierKey = "Kids",  PriceKes = 250, SortOrder = 2, IsActive = true }
                ]
            },
            new()
            {
                Key = "Canvas",
                DisplayName = "Canvas shoe care",
                Tagline = "Deep clean · midsole refresh · deodorizing",
                ThemeColorHex = "#2E7D32",
                ImageBeforeUrl = "https://images.unsplash.com/photo-1606107557195-0e29a4b5b4aa?w=640&q=80",
                ImageAfterUrl = "https://images.unsplash.com/photo-1525966222134-fcfa99b8ae77?w=640&q=80",
                SortOrder = 4,
                TierPrices =
                [
                    new ShoeServiceTierPrice { ColorTierKey = "Adult", PriceKes = 300, SortOrder = 1, IsActive = true },
                    new ShoeServiceTierPrice { ColorTierKey = "Kids",  PriceKes = 250, SortOrder = 2, IsActive = true }
                ]
            }
        };

        db.ShoeServiceCategories.AddRange(categories);
        await db.SaveChangesAsync();
    }

    /// <summary>Seeds a sample time-boxed offer when <c>ShoeServicePromotions</c> is empty (edit/disable in DB anytime).</summary>
    public static async Task EnsureSamplePromotionsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.ShoeServicePromotions.AnyAsync())
            return;

        var sneaker = await db.ShoeServiceCategories
            .FirstOrDefaultAsync(c => c.Key == "Sneakers");
        if (sneaker is null)
            return;

        db.ShoeServicePromotions.Add(new ShoeServicePromotion
        {
            CategoryId = sneaker.Id,
            Label = "Easter",
            DiscountKind = PromotionDiscountKind.PercentOff,
            PercentOff = 10,
            ValidFrom = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            ValidTo = new DateTimeOffset(2026, 4, 30, 23, 59, 59, TimeSpan.Zero),
            IsActive = true,
            Priority = 10
        });
        await db.SaveChangesAsync();
    }
}
