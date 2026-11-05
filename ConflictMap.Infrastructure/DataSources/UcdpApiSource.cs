using System.Net.Http.Json;
using ConflictMap.Infrastructure.DataSources.Ucdp;
using Microsoft.Extensions.Logging;

namespace ConflictMap.Infrastructure.DataSources;

// Strategy #1: fetches rows from the UCDP API
public class UcdpApiSource(HttpClient httpClient, ILogger<UcdpApiSource> logger) : IConflictDataSource
{
    private const string Version = "26.1";
    private const int PageSize = 1000;
    private const int MaxPages = 20;

    public Task<List<UcdpConflictRow>> GetConflictRowsAsync(CancellationToken ct = default) =>
        GetAllPagesAsync<UcdpConflictRow>("ucdpprioconflict", ct);

    public Task<List<UcdpDeathsRow>> GetBattleDeathsRowsAsync(CancellationToken ct = default) =>
        GetAllPagesAsync<UcdpDeathsRow>("battledeaths", ct);

    // Shared paging logic for every UCDP resource
    private async Task<List<T>> GetAllPagesAsync<T>(string resource, CancellationToken ct)
    {
        var allRows = new List<T>();
        string? url = $"{resource}/{Version}?pagesize={PageSize}";
        var pagesFetched = 0;

        // Keep fetching until there is no next page (null or empty)
        while (!string.IsNullOrWhiteSpace(url) && pagesFetched < MaxPages)
        {
            logger.LogInformation("Fetching UCDP page: {Url}", url);

            var response = await httpClient.GetFromJsonAsync<UcdpPagedResponse<T>>(url, ct);
            if (response is null) break;

            allRows.AddRange(response.Result);
            url = response.NextPageUrl;
            pagesFetched++;
        }

        return allRows;
    }
}