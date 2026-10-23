// API-kontrakt för Konfliktkartan.
// Lägg filen i projektet ConflictMap.Contracts.
// Både ConflictMap.Api (Person A) och ConflictMap.Web (Person B) refererar till ConflictMap.Contracts.
// Ändringar görs bara via pull request som båda godkänner. Se API-CONTRACT.md.

namespace ConflictMap.Contracts;

// ---------- Gemensamt ----------

/// <summary>Svar för alla listor som pagineras.</summary>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public enum ConflictType
{
    Extrasystemic,
    Interstate,
    Intrastate,
    InternationalizedIntrastate
}

public enum Region
{
    Europe,
    MiddleEast,
    Asia,
    Africa,
    Americas
}

// ---------- 1. Lista med konflikter ----------

/// <summary>Filter för GET /api/conflicts. Alla fält är valfria.</summary>
public record ConflictQuery(
    int? Year = null,
    string? Country = null,
    Region? Region = null,
    ConflictType? Type = null,
    int Page = 1,
    int PageSize = 20);

/// <summary>En rad i listan: en konflikt ett visst år.</summary>
public record ConflictSummaryDto(
    int ConflictId,
    string Location,
    string SideA,
    string SideB,
    int Year,
    ConflictType Type,
    int Intensity,          // 1 = 25–999 dödsfall, 2 = 1000+ (krig)
    int? BattleDeaths);     // null om BRD saknar värde

// ---------- 2. En enskild konflikt ----------

public record ConflictDetailDto(
    int ConflictId,
    string Location,
    string SideA,
    string SideB,
    ConflictType Type,
    Region Region,
    int StartYear,
    IReadOnlyList<ConflictYearDto> Years,
    string Source);

public record ConflictYearDto(
    int Year,
    int Intensity,
    int? BattleDeathsBest,
    int? BattleDeathsLow,
    int? BattleDeathsHigh);

// ---------- 3. Siffror per land (kartan) ----------

public record CountryStatDto(
    string IsoCode,         // ISO 3166-1 alpha-3, t.ex. "ETH"
    string CountryName,
    int Year,
    int ActiveConflicts,
    int BattleDeaths,
    int MaxIntensity);

// ---------- 4. AI-chatten ----------

public record ChatRequestDto(
    string Question,
    int? Year = null);

public record ChatResponseDto(
    string Answer,
    IReadOnlyList<string> HighlightCountries,   // samma ISO-koder som CountryStatDto
    string Source);

// ---------- Klient-interface ----------

/// <summary>
/// Det frontend använder för att prata med API:et.
/// Person B gör en MockConflictApiClient med låtsasdata,
/// och byter senare till en riktig HttpClient-version utan att ändra komponenterna.
/// </summary>
public interface IConflictApiClient
{
    Task<PagedResult<ConflictSummaryDto>> GetConflictsAsync(ConflictQuery query, CancellationToken ct = default);
    Task<ConflictDetailDto?> GetConflictAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CountryStatDto>> GetCountryStatsAsync(int year, CancellationToken ct = default);
    Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken ct = default);
}
