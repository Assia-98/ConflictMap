using System.ComponentModel;
using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Services;

namespace ConflictMap.Api.Ai;

// A short, sorted row that is easy for a small AI model to read correctly
public record CountryDeaths(string Country, string IsoCode, int BattleDeaths, int ActiveConflicts, bool IsWar);

// The tools the AI model may call. Every number the AI uses comes from here, i.e. from the database.
// The class also remembers what it fetched, so the map can highlight those countries.
public class ConflictTools(IConflictService service)
{
    public HashSet<string> FetchedIsoCodes { get; } = [];
    public HashSet<int> FetchedConflictIds { get; } = [];

    [Description("Lists armed conflicts from UCDP data, one row per conflict and year. " +
                 "Can filter by year, country, region and type. Returns at most 50 rows.")]
    public async Task<List<ConflictSummaryDto>> GetConflicts(
        [Description("Year, e.g. 2020. Data covers 1946-2025.")] int? year = null,
        [Description("Country ISO code (e.g. 'ETH') or English name (e.g. 'Ethiopia').")] string? country = null,
        [Description("Region: Europe, MiddleEast, Asia, Africa or Americas.")] string? region = null,
        [Description("Type: Extrasystemic, Interstate, Intrastate or InternationalizedIntrastate.")] string? type = null,
        CancellationToken ct = default)
    {
        var query = new ConflictQuery(
            year, country, ParseEnum<Region>(region), ParseEnum<ConflictType>(type),
            Page: 1, PageSize: 50);

        var result = await service.GetConflictsAsync(query, ct);

        foreach (var item in result.Items)
            FetchedConflictIds.Add(item.ConflictId);

        return result.Items.ToList();
    }

    [Description("Gets one conflict with all its years, intensity and battle-related deaths.")]
    public async Task<ConflictDetailDto?> GetConflictDetails(
        [Description("The UCDP conflict id, e.g. 11342.")] int conflictId,
        CancellationToken ct = default)
    {
        var conflict = await service.GetConflictAsync(conflictId, ct);
        if (conflict is not null)
            FetchedConflictIds.Add(conflict.ConflictId);

        return conflict;
    }

    // CHANGED: replaces GetCountryStats. Fewer rows, already sorted by deaths.
    [Description("Gets the countries with the most battle-related deaths in one year, " +
                 "sorted from most to fewest. Use it for questions like " +
                 "'which countries had the most deaths' or 'where was the worst fighting'.")]
    public async Task<List<CountryDeaths>> GetTopCountriesByDeaths(
        [Description("Year, e.g. 2020. Battle deaths exist from 1989.")] int year,
        [Description("How many countries to return, 1-20. Default 10.")] int count = 10,
        CancellationToken ct = default)
    {
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

    // "africa" -> Region.Africa, unknown text -> null (no filter)
    private static T? ParseEnum<T>(string? value) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : null;
}