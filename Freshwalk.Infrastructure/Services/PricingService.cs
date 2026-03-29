using Freshwalk.Application;
using Freshwalk.Domain;

namespace Freshwalk.Infrastructure.Services;

public class PricingService : IPricingService
{
    private static readonly Dictionary<string, Dictionary<string, decimal>> Prices = new()
    {
        ["Sneakers"] = new() { ["BlackDark"] = 350, ["MixedColored"] = 450, ["WhiteLight"] = 550 },
        ["Suede"] = new() { ["BlackDark"] = 350, ["MixedColored"] = 400, ["WhiteLight"] = 500 },
        ["Nubuck"] = new() { ["BlackDark"] = 350, ["MixedColored"] = 400, ["WhiteLight"] = 550 },
        ["OfficialLeather"] = new() { ["BlackDark"] = 350, ["MixedColored"] = 400, ["WhiteLight"] = 450 }
    };

    public PricingBreakdown CalculateBreakdown(List<BookingItemDto> items, IReadOnlyList<string> addOns)
    {
        static bool Has(IReadOnlyList<string> list, string name) =>
            list.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));

        var totalPairs = items.Sum(i => i.PairCount);

        decimal addOnPerPair = 0;
        if (Has(addOns, nameof(AddOnType.Waterproofing))) addOnPerPair += 100;
        if (Has(addOns, nameof(AddOnType.ShadeChanging))) addOnPerPair += 200;
        if (Has(addOns, nameof(AddOnType.ExpressService))) addOnPerPair += 150;

        decimal deliveryFee = 0;
        if (Has(addOns, nameof(AddOnType.PickupDelivery)) && totalPairs < 3) deliveryFee = 150;

        decimal subtotal = 0;
        foreach (var item in items)
        {
            var price = Prices.GetValueOrDefault(item.ShoeType)?.GetValueOrDefault(item.ColorTier) ?? 350;
            subtotal += (price + addOnPerPair) * item.PairCount;
        }
        subtotal += deliveryFee;

        var discountRate = totalPairs switch
        {
            2 => 0.10m,
            3 or 4 => 0.15m,
            >= 5 => 0.20m,
            _ => 0m
        };

        var finalPrice = decimal.Round(subtotal * (1 - discountRate), 2);
        var discountAmount = decimal.Round(subtotal - finalPrice, 2);

        return new PricingBreakdown(
            decimal.Round(subtotal, 2),
            discountRate,
            discountAmount,
            finalPrice);
    }
}
