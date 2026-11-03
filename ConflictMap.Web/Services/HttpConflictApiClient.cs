using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConflictMap.Contracts;

namespace ConflictMap.Web.Services;

/// <summary>Riktiga anrop till A:s API. Följer API-CONTRACT.md.</summary>
public sealed class HttpConflictApiClient(HttpClient http) : IConflictApiClient
{
    // API:et skickar enums som text ("Intrastate"), så vi måste läsa dem som text
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // 1. GET /api/conflicts
    public async Task<PagedResult<ConflictSummaryDto>> GetConflictsAsync(ConflictQuery query, CancellationToken ct = default)
    {
        var url = "api/conflicts" + ToQueryString(query);
        return await http.GetFromJsonAsync<PagedResult<ConflictSummaryDto>>(url, Json, ct)
               ?? throw new HttpRequestException("Tomt svar från API:et.");
    }

    // 2. GET /api/conflicts/{id}
    public async Task<ConflictDetailDto?> GetConflictAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.GetAsync($"api/conflicts/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ConflictDetailDto>(Json, ct);
    }

    // 3. GET /api/countries/stats?year=
    public async Task<IReadOnlyList<CountryStatDto>> GetCountryStatsAsync(int year, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<CountryStatDto>>($"api/countries/stats?year={year}", Json, ct)
               ?? [];
    }

    // 4. POST /api/chat (finns inte i A:s API än)
    public async Task<ChatResponseDto> AskAsync(ChatRequestDto request, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("api/chat", request, Json, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatResponseDto>(Json, ct)
               ?? throw new HttpRequestException("Tomt svar från AI-tjänsten.");
    }

    private static string ToQueryString(ConflictQuery q)
    {
        var parts = new List<string>();
        if (q.Year is int year) parts.Add($"year={year}");
        if (!string.IsNullOrWhiteSpace(q.Country)) parts.Add($"country={Uri.EscapeDataString(q.Country)}");
        if (q.Region is Region region) parts.Add($"region={region}");
        if (q.Type is ConflictType type) parts.Add($"type={type}");
        parts.Add($"page={q.Page}");
        parts.Add($"pageSize={q.PageSize}");
        return "?" + string.Join("&", parts);
    }
}