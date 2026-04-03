using Freshwalk.Domain;

namespace Freshwalk.Application;

public static class BookingAddOns
{
    private static readonly HashSet<string> ValidAddOnNames =
        Enum.GetNames<AddOnType>().ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static List<string> Normalize(IEnumerable<string>? raw)
    {
        if (raw is null) return new List<string>();
        return raw
            .Where(a => !string.IsNullOrWhiteSpace(a) && ValidAddOnNames.Contains(a.Trim()))
            .Select(a => Enum.Parse<AddOnType>(a.Trim(), true).ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
