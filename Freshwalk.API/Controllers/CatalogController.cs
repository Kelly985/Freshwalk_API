using Freshwalk.Application;
using Freshwalk.Domain;
using Freshwalk.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController(AppDbContext db, IPricingService pricingService) : ControllerBase
{
    /// <summary>Public shoe categories, tier prices, active offers, and before/after imagery (database-driven).</summary>
    [HttpGet("shoe-services")]
    [AllowAnonymous]
    public async Task<IActionResult> ShoeServices(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await db.ShoeServiceCategories
            .AsNoTracking()
            .Include(c => c.TierPrices)
            .Include(c => c.Promotions)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);

        var dto = rows.Select(c => new ShoeServiceCategoryCatalogDto(
            c.Key,
            c.DisplayName,
            c.Tagline,
            c.ThemeColorHex,
            c.ImageBeforeUrl,
            c.ImageAfterUrl,
            c.SortOrder,
            c.TierPrices
                .OrderBy(t => t.SortOrder)
                .Select(t => new ShoeServiceTierPriceDto(t.ColorTierKey, t.PriceKes, t.SortOrder))
                .ToList(),
            MapActivePromotion(c, now))).ToList();

        return Ok(dto);
    }

    /// <summary>Server-side price preview (promotions + bundle) for the booking form.</summary>
    [HttpPost("preview-pricing")]
    [AllowAnonymous]
    public async Task<IActionResult> PreviewPricing([FromBody] PreviewPricingRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "At least one shoe item is required." });

        var addOns = BookingAddOns.Normalize(request.AddOns);
        var breakdown = await pricingService.CalculateBreakdownAsync(request.Items, addOns, cancellationToken);
        return Ok(breakdown);
    }

    private static ShoeServiceActivePromotionDto? MapActivePromotion(ShoeServiceCategory c, DateTimeOffset now)
    {
        var p = c.Promotions
            .Where(x => x.IsActive)
            .Where(x => !x.ValidFrom.HasValue || now >= x.ValidFrom.Value)
            .Where(x => !x.ValidTo.HasValue || now <= x.ValidTo.Value)
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault();

        if (p is null) return null;

        return new ShoeServiceActivePromotionDto(
            p.Label,
            p.DiscountKind.ToString(),
            p.PercentOff,
            p.FixedOffPerPairKes,
            p.ValidFrom,
            p.ValidTo);
    }
}
