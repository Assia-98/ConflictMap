using System.ComponentModel;
using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Services;

namespace ConflictMap.Api.Ai;

// A short, sorted row that is easy for a small AI model to read correctly
public record CountryDeaths(string Country, string IsoCode, int BattleDeaths, int ActiveConflicts, bool IsWar);

// NEW: the list of conflicts together with the exact total, counted by the database
public record ConflictList(int TotalCount, List<ConflictSummaryDto> Conflicts);

// The tools the AI model may call. Every number the AI uses comes from here, i.e. from the database.
// The class also remembers what it fetched, so the map can highlight those countries.
public class ConflictTools(IConflictService service)
{
    public HashSet<string> FetchedIsoCodes { get; } = [];
    public HashSet<int> FetchedConflictIds { get; } = [];

    [Description("Lists armed conflicts from UCDP data, one row per conflict and year. " +
                 "Can filter by year, country, region and type. Returns TotalCount (the exact number " +
                 "of matching conflicts) and at most 50 rows.")]
    public async Task<ConflictList> GetConflicts(
        [Description("Year, e.g. 2020. Data covers 1946-2025.")] int? year = null,
        [Description("Country ISO code (e.g. 'ETH') or English name (e.g. 'Ethiopia'). Leave out if not needed.")] string? country = null,
        [Description("Region: Europe, MiddleEast, Asia, Africa or Americas. Leave out if not needed.")] string? region = null,
        [Description("Type: Extrasystemic, Interstate, Intrastate or InternationalizedIntrastate. Leave out if not needed.")] string? type = null,
        CancellationToken ct = default)
    {
        // Small models sometimes send the text "null" instead of leaving a field out
        country = Clean(country);
        region = Clean(region);
        type = Clean(type);

        Console.WriteLine($"TOOL GetConflicts: year={year}, country={country}, region={region}, type={type}");

        var query = new ConflictQuery(
            year, country, ParseEnum<Region>(region), ParseEnum<ConflictType>(type),
            Page: 1, PageSize: 50);

        var result = await service.GetConflictsAsync(query, ct);

        foreach (var item in result.Items)
            FetchedConflictIds.Add(item.ConflictId);

        // CHANGED: the database's exact count goes along with the rows
        return new ConflictList(result.TotalCount, result.Items.ToList());
    }

    [Description("Gets one conflict with all its years, intensity and battle-related deaths.")]
    public async Task<ConflictDetailDto?> GetConflictDetails(
        [Description("The UCDP conflict id, e.g. 11342.")] int conflictId,
        CancellationToken ct = default)
    {
        Console.WriteLine($"TOOL GetConflictDetails: conflictId={conflictId}");

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
        Console.WriteLine($"TOOL GetTopCountriesByDeaths: year={year}, count={count}");

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