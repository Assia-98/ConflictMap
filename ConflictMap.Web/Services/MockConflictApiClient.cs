using System.Globalization;
using System.Text.RegularExpressions;
using ConflictMap.Contracts;
 
namespace ConflictMap.Web.Services;
 
/// <summary>
/// Låtsasdata som följer Contracts.cs, så att UI:t kan byggas innan backend är klar.
/// Konfliktnamn och länder liknar UCDP:s, men ALLA SIFFROR ÄR PÅHITTADE.
/// Byts mot en riktig klient med en rad i Program.cs.
///
/// Tips: skriv "/fel" i chatten för att testa felhantering.
/// </summary>
public sealed class MockConflictApiClient : IConflictApiClient
{
    private const string MockSource = "Låtsasdata (inte UCDP)";
 
    public const int MinYear = 2018;
    public const int MaxYear = 2023;
 
    /// <summary>Fördröjning som liknar ett nätverksanrop, så att laddningslägen syns. Sätt till 0 i tester.</summary>
    public TimeSpan Latency { get; init; } = TimeSpan.FromMilliseconds(400);
 
    private sealed record MockConflict(
        int Id, string[] Iso, string SideA, string SideB,
        ConflictType Type, Region Region, int FromYear, int ToYear, int BaseDeaths);
 
    private static readonly Dictionary<string, string> CountryNames = new()
    {
        ["ETH"] = "Ethiopia", ["SOM"] = "Somalia", ["MLI"] = "Mali", ["NGA"] = "Nigeria",
        ["SDN"] = "Sudan", ["RUS"] = "Russia", ["UKR"] = "Ukraine", ["SYR"] = "Syria",
        ["YEM"] = "Yemen", ["AFG"] = "Afghanistan", ["MMR"] = "Myanmar", ["COL"] = "Colombia",
        ["ARM"] = "Armenia", ["AZE"] = "Azerbaijan", ["MOZ"] = "Mozambique", ["COD"] = "DR Congo"
    };
 
    private static readonly MockConflict[] Conflicts =
    [
        new(1,  ["ETH"],        "Government of Ethiopia",   "TPLF",                  ConflictType.Intrastate,                   Region.Africa,     2020, 2022, 9000),
        new(2,  ["SOM"],        "Government of Somalia",    "Al-Shabaab",            ConflictType.InternationalizedIntrastate,  Region.Africa,     2018, 2023, 1800),
        new(3,  ["MLI"],        "Government of Mali",       "JNIM",                  ConflictType.InternationalizedIntrastate,  Region.Africa,     2018, 2023, 900),
        new(4,  ["NGA"],        "Government of Nigeria",    "IS",                    ConflictType.InternationalizedIntrastate,  Region.Africa,     2018, 2023, 1200),
        new(5,  ["SDN"],        "Government of Sudan",      "RSF",                   ConflictType.Intrastate,                   Region.Africa,     2023, 2023, 7000),
        new(6,  ["RUS", "UKR"], "Government of Russia",     "Government of Ukraine", ConflictType.Interstate,                   Region.Europe,     2022, 2023, 40000),
        new(7,  ["UKR"],        "Government of Ukraine",    "DPR, LPR",              ConflictType.InternationalizedIntrastate,  Region.Europe,     2018, 2021, 150),
        new(8,  ["SYR"],        "Government of Syria",      "Syrian insurgents",     ConflictType.InternationalizedIntrastate,  Region.MiddleEast, 2018, 2023, 5000),
        new(9,  ["YEM"],        "Government of Yemen",      "Ansarallah",            ConflictType.InternationalizedIntrastate,  Region.MiddleEast, 2018, 2023, 6000),
        new(10, ["AFG"],        "Government of Afghanistan","Taleban",               ConflictType.InternationalizedIntrastate,  Region.Asia,       2018, 2021, 15000),
        new(11, ["MMR"],        "Government of Myanmar",    "PDF",                   ConflictType.Intrastate,                   Region.Asia,       2021, 2023, 3500),
        new(12, ["COL"],        "Government of Colombia",   "ELN",                   ConflictType.Intrastate,                   Region.Americas,   2018, 2023, 120),
        new(13, ["ARM", "AZE"], "Government of Armenia",    "Government of Azerbaijan", ConflictType.Interstate,                Region.Europe,     2020, 2020, 6500),
        new(14, ["MOZ"],        "Government of Mozambique", "IS",                    ConflictType.InternationalizedIntrastate,  Region.Africa,     2019, 2023, 600),
        new(15, ["COD"],        "Government of DR Congo",   "M23",                   ConflictType.InternationalizedIntrastate,  Region.Africa,     2022, 2023, 800),
    ];
 
    // ---------- 1. GET /api/conflicts ----------
 
    public async Task<PagedResult<ConflictSummaryDto>> GetConflictsAsync(ConflictQuery query, CancellationToken ct = default)
    {
        await Task.Delay(Latency, ct);
 
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var country = query.Country?.Trim().ToUpperInvariant();
 
        // En rad = en konflikt ett visst år (som i kontraktet)
        var rows = Conflicts
            .Where(c => query.Region is null || c.Region == query.Region)
            .Where(c => query.Type is null || c.Type == query.Type)
            .Where(c => string.IsNullOrEmpty(country) || c.Iso.Contains(country))
            .SelectMany(c => YearsOf(c).Select(year => (c, year)))
            .Where(x => query.Year is null || x.year == query.Year)
            .Select(x => ToSummary(x.c, x.year))
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.BattleDeaths ?? 0)
            .ToList();
 
        var items = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<ConflictSummaryDto>(items, page, pageSize, rows.Count);
    }
 
    // ---------- 2. GET /api/conflicts/{id} ----------
 
    public async Task<ConflictDetailDto?> GetConflictAsync(int id, CancellationToken ct = default)
    {
        await Task.Delay(Latency, ct);
 
        var c = Conflicts.FirstOrDefault(x => x.Id == id);
        if (c is null) return null;   // motsvarar 404
 
        var years = YearsOf(c)
            .Select(year =>
            {
                var best = Deaths(c, year);
                return new ConflictYearDto(year, IntensityFor(best), best, (int)(best * 0.8), (int)(best * 1.3));
            })
            .ToList();
 
        return new ConflictDetailDto(c.Id, LocationOf(c), c.SideA, c.SideB,
            c.Type, c.Region, c.FromYear, years, MockSource);
    }
 
    // ---------- 3. GET /api/countries/stats?year= ----------
 
    public async Task<IReadOnlyList<CountryStatDto>> GetCountryStatsAsync(int year, CancellationToken ct = default)
    {
        await Task.Delay(Latency, ct);
 
        // Samma regel som vi föreslår för backend: en konflikt räknas i alla länder i dess location.
        return Conflicts
            .Where(c => IsActive(c, year))
            .SelectMany(c => c.Iso.Select(iso => (iso, deaths: Deaths(c, year))))
            .GroupBy(x => x.iso)
            .Select(g => new CountryStatDto(
                g.Key,
                CountryNames[g.Key],
                year,
                g.Count(),
                g.Sum(x => x.deaths),
                g.Max(x => IntensityFor(x.deaths))))
            .ToList();
    }
 
    // ---------- 4. POST /api/chat ----------
 
    // En enkel "AI" för utveckling: letar efter år, länder och regioner i frågan.
    private static readonly Dictionary<string, string[]> SubRegions = new()
    {
        ["östafrika"] = ["ETH", "SOM", "SDN", "MOZ"],
        ["västafrika"] = ["MLI", "NGA"],
        ["sahel"] = ["MLI", "NGA"],
    };
 
    private static readonly Dictionary<string, Region> RegionWords = new()
    {
        ["afrika"] = Region.Africa, ["europa"] = Region.Europe, ["mellanöstern"] = Region.MiddleEast,
        ["asien"] = Region.Asia, ["amerika"] = Region.Americas,
    };
 
    private static readonly Dictionary<string, string> SwedishCountryNames = new()
    {
        ["etiopien"] = "ETH", ["somalia"] = "SOM", ["mali"] = "MLI", ["nigeria"] = "NGA",
        ["sudan"] = "SDN", ["ryssland"] = "RUS", ["ukraina"] = "UKR", ["syrien"] = "SYR",
        ["jemen"] = "YEM", ["afghanistan"] = "AFG", ["myanmar"] = "MMR", ["colombia"] = "COL",
        ["armenien"] = "ARM", ["azerbajdzjan"] = "AZE", ["moçambique"] = "MOZ", ["mocambique"] = "MOZ",
        ["kongo"] = "COD",
    };
 
    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken ct = default)
    {
        await Task.Delay(Latency * 3, ct);
 
        var text = request.Question.Trim().ToLowerInvariant();
        if (text.StartsWith("/fel"))
            throw new HttpRequestException("Låtsasfel från MockConflictApiClient (för att testa felhantering).");
 
        // År: först i frågan, annars från requesten, annars senaste året
        var yearMatch = Regex.Match(text, @"\b(19|20)\d{2}\b");
        var year = yearMatch.Success
            ? int.Parse(yearMatch.Value, CultureInfo.InvariantCulture)
            : request.Year ?? MaxYear;
 
        IEnumerable<MockConflict> found = Conflicts.Where(c => IsActive(c, year));
 
        var countries = SwedishCountryNames.Where(kv => text.Contains(kv.Key)).Select(kv => kv.Value).ToHashSet();
        var subRegion = SubRegions.FirstOrDefault(kv => text.Contains(kv.Key));
        var region = RegionWords.FirstOrDefault(kv => text.Contains(kv.Key));
 
        if (countries.Count > 0)
            found = found.Where(c => c.Iso.Any(countries.Contains));
        else if (subRegion.Value is not null)
            found = found.Where(c => c.Iso.Any(subRegion.Value.Contains));
        else if (region.Key is not null)
            found = found.Where(c => c.Region == region.Value);
 
        var list = found.OrderByDescending(c => Deaths(c, year)).ToList();
 
        if (list.Count == 0)
            return new ChatResponseDto($"(Låtsassvar) Jag hittade inga konflikter som matchar frågan för {year}.", [], MockSource);
 
        var sv = CultureInfo.GetCultureInfo("sv-SE");
        var lines = list.Select(c => $"{LocationOf(c)}: {c.SideA} mot {c.SideB}, {Deaths(c, year).ToString("N0", sv)} dödsfall");
        var answer = $"(Låtsassvar) År {year} hittade jag {list.Count} konflikt(er). " + string.Join(". ", lines) + ".";
 
        var highlight = list.SelectMany(c => c.Iso).Distinct().ToList();
        return new ChatResponseDto(answer, highlight, MockSource);
    }
 
    // ---------- Hjälpmetoder ----------
 
    private static IEnumerable<int> YearsOf(MockConflict c) => Enumerable.Range(c.FromYear, c.ToYear - c.FromYear + 1);
 
    private static bool IsActive(MockConflict c, int year) => year >= c.FromYear && year <= c.ToYear;
 
    private static string LocationOf(MockConflict c) => string.Join(", ", c.Iso.Select(i => CountryNames[i]));
 
    /// <summary>Påhittad men stabil siffra: samma konflikt och år ger alltid samma värde.</summary>
    private static int Deaths(MockConflict c, int year)
    {
        var factor = 0.6 + ((c.Id * 31 + year * 17) % 9) / 10.0;
        return (int)(c.BaseDeaths * factor);
    }
 
    /// <summary>1 = 25–999 dödsfall, 2 = 1000+ (krig), som i kontraktet.</summary>
    private static int IntensityFor(int deaths) => deaths >= 1000 ? 2 : 1;
 
    private static ConflictSummaryDto ToSummary(MockConflict c, int year)
    {
        var deaths = Deaths(c, year);
        return new ConflictSummaryDto(c.Id, LocationOf(c), c.SideA, c.SideB, year, c.Type, IntensityFor(deaths), deaths);
    }
}