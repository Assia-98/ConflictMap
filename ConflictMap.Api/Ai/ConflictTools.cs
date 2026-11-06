using System.ComponentModel;
using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Services;
using ConflictMap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConflictMap.Api.Ai;

// A short, sorted row that is easy for a small AI model to read correctly
public record CountryDeaths(string Country, string IsoCode, int BattleDeaths, int ActiveConflicts, bool IsWar);

// The list of conflicts together with the exact total, counted by the database
public record ConflictList(int TotalCount, List<ConflictSummaryDto> Conflicts);

// The tools the AI model may call. Every number the AI uses comes from here, i.e. from the database.
// The class also remembers what it fetched, so the map can highlight those countries.
public class ConflictTools(IConflictService service, AppDbContext db, ILogger<ConflictTools> logger)
{
    public HashSet<string> FetchedIsoCodes { get; } = [];
    public HashSet<int> FetchedConflictIds { get; } = [];
    // UCDP only has five big regions. Here we decide which countries belong to each part,
// so the AI does not have to know it itself.
    private sealed record SubRegion(Region Region, string[] IsoCodes);

    private static readonly Dictionary<string, SubRegion> SubRegions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NorthAfrica"] = new(Region.Africa, ["DZA", "EGY", "LBY", "MAR", "TUN"]),
        ["WestAfrica"] = new(Region.Africa, ["BEN", "BFA", "CPV", "CIV", "GMB", "GHA", "GIN", "GNB", "LBR", "MLI", "MRT", "NER", "NGA", "SEN", "SLE", "TGO"]),
        ["CentralAfrica"] = new(Region.Africa, ["AGO", "CMR", "CAF", "TCD", "COG", "COD", "GNQ", "GAB", "STP"]),
        ["EastAfrica"] = new(Region.Africa, ["BDI", "COM", "DJI", "ERI", "ETH", "KEN", "MDG", "MWI", "MUS", "MOZ", "RWA", "SYC", "SOM", "SSD", "SDN", "TZA", "UGA", "ZMB", "ZWE"]),
        ["SouthernAfrica"] = new(Region.Africa, ["BWA", "SWZ", "LSO", "NAM", "ZAF"]),
        ["SouthAsia"] = new(Region.Asia, ["AFG", "BGD", "BTN", "IND", "MDV", "NPL", "PAK", "LKA"]),
        ["SoutheastAsia"] = new(Region.Asia, ["BRN", "KHM", "IDN", "LAO", "MYS", "MMR", "PHL", "SGP", "THA", "TLS", "VNM"]),
        ["EastAsia"] = new(Region.Asia, ["CHN", "JPN", "KOR", "PRK", "MNG", "TWN"]),
        ["CentralAsia"] = new(Region.Asia, ["KAZ", "KGZ", "TJK", "TKM", "UZB"]),
        ["NorthAmerica"] = new(Region.Americas, ["USA", "CAN", "MEX"]),
        ["CentralAmerica"] = new(Region.Americas, ["BLZ", "CRI", "SLV", "GTM", "HND", "NIC", "PAN"]),
        ["Caribbean"] = new(Region.Americas, ["CUB", "DOM", "HTI", "JAM", "TTO", "GRD", "BHS", "BRB"]),
        ["SouthAmerica"] = new(Region.Americas, ["ARG", "BOL", "BRA", "CHL", "COL", "ECU", "GUY", "PRY", "PER", "SUR", "URY", "VEN"])
    };

   [Description("Lists armed conflicts from UCDP data, one row per conflict and year. " +
             "Can filter by year, country, region, sub-region and type. Returns TotalCount (the exact number " +
             "of matching conflicts) and the rows.")]
public async Task<ConflictList> GetConflicts(
    [Description("Year, e.g. 2020. Data covers 1946-2025.")] int? year = null,
    [Description("Country ISO code (e.g. 'ETH') or English name (e.g. 'Ethiopia'). Leave out if not needed.")] string? country = null,
    [Description("Region: Europe, MiddleEast, Asia, Africa or Americas. Leave out if not needed.")] string? region = null,
    [Description("Part of a region: NorthAfrica, WestAfrica, CentralAfrica, EastAfrica, SouthernAfrica, " +
                 "SouthAsia, SoutheastAsia, EastAsia, CentralAsia, NorthAmerica, CentralAmerica, Caribbean " +
                 "or SouthAmerica. Use this instead of region when the user asks about a part of a region.")] string? subRegion = null,
    [Description("Type: Extrasystemic, Interstate, Intrastate or InternationalizedIntrastate. Leave out if not needed.")] string? type = null,
    CancellationToken ct = default)
{
    // Small models sometimes send the text "null" instead of leaving a field out
    country = Clean(country);
    region = Clean(region);
    subRegion = Clean(subRegion);
    type = Clean(type);

    logger.LogInformation(
        "AI tool GetConflicts: year={Year}, country={Country}, region={Region}, subRegion={SubRegion}, type={Type}",
        year, country, region, subRegion, type);

    SubRegion? sub = subRegion is not null && SubRegions.TryGetValue(subRegion, out var found) ? found : null;

    // With a sub-region we fetch the whole region and filter afterwards, so we take more rows
    var query = new ConflictQuery(
        year, country, sub?.Region ?? ParseEnum<Region>(region), ParseEnum<ConflictType>(type),
        Page: 1, PageSize: sub is null ? 50 : 100);

    var result = await service.GetConflictsAsync(query, ct);
    var items = result.Items.ToList();
    var totalCount = result.TotalCount;

    if (sub is not null)
    {
        // Keep only conflicts that take place in a country in the sub-region
        var ids = items.Select(i => i.ConflictId).Distinct().ToList();

        var idsInSubRegion = await db.Conflicts
            .Where(c => ids.Contains(c.ConflictId)
                        && c.Countries.Any(x => sub.IsoCodes.Contains(x.IsoCode)))
            .Select(c => c.ConflictId)
            .ToListAsync(ct);

        items = items.Where(i => idsInSubRegion.Contains(i.ConflictId)).ToList();
        totalCount = items.Count;
    }

    // Only the conflicts we actually return are highlighted on the map
    foreach (var item in items)
        FetchedConflictIds.Add(item.ConflictId);

    return new ConflictList(totalCount, items);
}

    [Description("Gets one conflict with all its years, intensity and battle-related deaths.")]
    public async Task<ConflictDetailDto?> GetConflictDetails(
        [Description("The UCDP conflict id, e.g. 11342.")] int conflictId,
        CancellationToken ct = default)
    {
        logger.LogInformation("AI tool GetConflictDetails: conflictId={ConflictId}", conflictId);

        var conflict = await service.GetConflictAsync(conflictId, ct);
        if (conflict is not null)
            FetchedConflictIds.Add(conflict.ConflictId);

        return conflict;
    }

    [Description("Gets the countries with the most battle-related deaths in one year, " +
                 "sorted from most to fewest. Use it for questions like " +
                 "'which countries had the most deaths' or 'where was the worst fighting'.")]
    public async Task<List<CountryDeaths>> GetTopCountriesByDeaths(
        [Description("Year, e.g. 2020. Battle deaths exist from 1989.")] int year,
        [Description("How many countries to return, 1-20. Default 10.")] int count = 10,
        CancellationToken ct = default)
    {
        logger.LogInformation("AI tool GetTopCountriesByDeaths: year={Year}, count={Count}", year, count);

        count = Math.Clamp(count, 1, 20);
        var stats = await service.GetCountryStatsAsync(year, ct);

        var top = stats
            .OrderByDescending(s => s.BattleDeaths)
            .Take(count)
            .Select(s => new CountryDeaths(
                s.CountryName, s.IsoCode, s.BattleDeaths, s.ActiveConflicts, IsWar: s.MaxIntensity == 2))
            .ToList();

        foreach (var row in top)
            FetchedIsoCodes.Add(row.IsoCode);

        return top;
    }

    // "null", "" or "  " from the AI means "no filter"
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Equals("null", StringComparison.OrdinalIgnoreCase)
            ? null
            : value.Trim();

    // "africa" -> Region.Africa, unknown text -> null (no filter)
    private static T? ParseEnum<T>(string? value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : null;
} 