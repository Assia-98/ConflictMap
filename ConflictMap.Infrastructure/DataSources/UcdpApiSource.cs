using System.Net.Http.Json;
using ConflictMap.Infrastructure.DataSources.Ucdp;

namespace ConflictMap.Infrastructure.DataSources;

// Strategy #1: fetches conflict rows from the UCDP API
public class UcdpApiSource(HttpClient httpClient) : IConflictDataSource
{
    private const string Version = "26.1";
    private const int PageSize = 1000;
    private const int MaxPages = 20;

    public async Task<List<UcdpConflictRow>> GetConflictRowsAsync(CancellationToken ct = default)
    {
        var allRows = new List<UcdpConflictRow>();
        string? url = $"ucdpprioconflict/{Version}?pagesize={PageSize}";
        var pagesFetched = 0;

        // Keep fetching until there is no next page
        while (url is not null && pagesFetched < MaxPages)
        {
            var response = await httpClient
                .GetFromJsonAsync<UcdpPagedResponse<UcdpConflictRow>>(url, ct);

            if (response is null) break;

            allRows.AddRange(response.Result);   // add this page's rows
            url = response.NextPageUrl;           // null on the last page
            pagesFetched++;
        }

        return allRows;
    }
}