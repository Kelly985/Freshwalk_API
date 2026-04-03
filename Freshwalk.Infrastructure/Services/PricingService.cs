using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.Infrastructure.Services;

public class PricingService(AppDbContext db) : IPricingService
{
    private static readonly Dictionary<string, Dictionary<string, decimal>> FallbackPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sneakers"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BlackDark"] = 350, ["MixedColored"] = 450, ["WhiteLight"] = 550
        },
        ["Suede"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BlackDark"] = 350, ["MixedColored"] = 400, ["WhiteLight"] = 500
        },
        ["Nubuck"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BlackDark"] = 350, ["MixedColored"] = 400, ["WhiteLight"] = 550
        },
        ["Canvas"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BlackDark"] = 350, ["MixedColored"] = 400, ["WhiteLight"] = 550
        }
    };

    public async Task<PricingBreakdown> CalculateBreakdownAsync(
        List<BookingItemDto> items,
        IReadOnlyList<string> addOns,
        CancellationToken cancellationToken = default)
    {
        static bool Has(IReadOnlyList<string> list, string name) =>
            list.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));

        var categories = await db.ShoeServiceCategories
            .AsNoTracking()
            .Include(c => c.TierPrices)
            .Include(c => c.Promotions)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var tierMap = new Dictionary<(string Shoe, string Color), decimal>();
        var promoByShoe = new Dictionary<string, ShoeServicePromotion?>(StringComparer.OrdinalIgnoreCase);
        var categoryKeyByShoe = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in categories)
        {
            var shoe = NormalizeShoeType(c.Key);
            categoryKeyByShoe[shoe] = c.Key;
            foreach (var t in c.TierPrices)
                tierMap[(shoe, t.ColorTierKey.Trim())] = t.PriceKes;

            var active = PickActivePromotion(c.Promotions, now);
            promoByShoe[shoe] = active;
        }

        var totalPairs = items.Sum(i => i.PairCount);

        decimal addOnPerPair = 0;
        if (Has(addOns, nameof(AddOnType.Waterproofing))) addOnPerPair += 100;
        if (Has(addOns, nameof(AddOnType.ShadeChanging))) addOnPerPair += 200;
        if (Has(addOns, nameof(AddOnType.ExpressService))) addOnPerPair += 150;

        decimal deliveryFee = 0;
        if (Has(addOns, nameof(AddOnType.PickupDelivery)) && totalPairs < 3) deliveryFee = 150;

        var promoAgg = new Dictionary<(string CategoryKey, string Label), decimal>();

        decimal shoesGross = 0;
        decimal shoesAfterPromo = 0;

        foreach (var item in items)
        {
            var shoe = NormalizeShoeType(item.ShoeType);
            var color = item.ColorTier.Trim();
            var unit = ResolveUnitPrice(tierMap, shoe, color);
            var qty = item.PairCount;
            shoesGross += unit * qty;

            var promo = promoByShoe.GetValueOrDefault(shoe);
            var discountedUnit = ApplyPromoToUnit(unit, promo, out var savePerPair);
            shoesAfterPromo += discountedUnit * qty;

            if (savePerPair > 0 && promo != null)
            {
                var catKey = categoryKeyByShoe.GetValueOrDefault(shoe) ?? shoe;
                var key = (catKey, promo.Label);
                promoAgg[key] = promoAgg.GetValueOrDefault(key) + savePerPair * qty;
            }
        }

        // Whole KES for M-Pesa: floor promotional savings, rebuild subtotal from gross, then floor bundle discount.
        var promotionalDiscountRaw = decimal.Round(Math.Max(0, shoesGross - shoesAfterPromo), 2);
        var promotionalDiscount = decimal.Floor(promotionalDiscountRaw);
        var applied = promoAgg
            .Select(kv => new PromotionAppliedDto(kv.Key.CategoryKey, kv.Key.Label, decimal.Round(kv.Value, 2)))
            .OrderBy(x => x.CategoryKey)
            .ToList();

        var addonTotal = addOnPerPair * totalPairs;
        var subtotalBeforeBundle = decimal.Round(shoesGross - promotionalDiscount + addonTotal + deliveryFee, 2);

        var discountRate = totalPairs switch
        {
            2 => 0.10m,
            3 or 4 => 0.15m,
            >= 5 => 0.20m,
            _ => 0m
        };

        var bundleDiscountRaw = decimal.Round(subtotalBeforeBundle * discountRate, 2);
        var bundleDiscountAmount = decimal.Floor(bundleDiscountRaw);
        var finalPrice = subtotalBeforeBundle - bundleDiscountAmount;

        return new PricingBreakdown(
            decimal.Round(shoesGross, 2),
            promotionalDiscount,
            applied,
            subtotalBeforeBundle,
            discountRate,
            bundleDiscountAmount,
            finalPrice);
    }

    private static ShoeServicePromotion? PickActivePromotion(
        IEnumerable<ShoeServicePromotion> promos,
        DateTimeOffset now) =>
        promos
            .Where(p => p.IsActive)
            .Where(p => !p.ValidFrom.HasValue || now >= p.ValidFrom.Value)
            .Where(p => !p.ValidTo.HasValue || now <= p.ValidTo.Value)
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();

    private static decimal ApplyPromoToUnit(decimal unit, ShoeServicePromotion? promo, out decimal savingsPerPair)
    {
        savingsPerPair = 0;
        if (promo is null || unit <= 0) return unit;

        if (promo.DiscountKind == PromotionDiscountKind.PercentOff && promo.PercentOff is { } pct && pct > 0 && pct < 100)
        {
            var discounted = unit * (1 - pct / 100m);
            discounted = decimal.Round(Math.Max(0, discounted), 2);
            savingsPerPair = decimal.Round(unit - discounted, 2);
            return discounted;
        }

        if (promo.DiscountKind == PromotionDiscountKind.FixedAmountPerPair && promo.FixedOffPerPairKes is { } fix && fix > 0)
        {
            var discounted = decimal.Round(Math.Max(0, unit - fix), 2);
            savingsPerPair = decimal.Round(unit - discounted, 2);
            return discounted;
        }

        return unit;
    }

    private static string NormalizeShoeType(string? shoeType)
    {
        if (string.IsNullOrWhiteSpace(shoeType)) return "";
        var s = shoeType.Trim();
        if (s.Equals("OfficialLeather", StringComparison.OrdinalIgnoreCase))
            return "Canvas";
        return s;
    }

    private static decimal ResolveUnitPrice(
        Dictionary<(string Shoe, string Color), decimal> map,
        string shoe,
        string color)
    {
        foreach (var kv in map)
        {
            if (string.Equals(kv.Key.Shoe, shoe, StringComparison.OrdinalIgnoreCase)
                && string.Equals(kv.Key.Color, color, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }

        if (FallbackPrices.TryGetValue(shoe, out var byColor))
        {
            if (byColor.TryGetValue(color, out var fb))
                return fb;
            foreach (var k in byColor.Keys)
            {
                if (string.Equals(k, color, StringComparison.OrdinalIgnoreCase))
                    return byColor[k];
            }
        }

        return 350m;
    }
}
