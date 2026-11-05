using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConflictMap.Contracts;

namespace ConflictMap.Tests.Api;

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    // The API sends enums as text ("Intrastate"), so we read them as text
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiTests(ApiFactory factory)
    {
        factory.SeedOnce();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetConflicts_Year2020_Returns200WithData()
    {
        var response = await _client.GetAsync("/api/conflicts?year=2020");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ConflictSummaryDto>>(Json);
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TPLF", result.Items[0].SideB);
    }

    [Fact]
    public async Task GetConflicts_TooLargePageSize_Returns400()
    {
        var response = await _client.GetAsync("/api/conflicts?pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetConflict_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/conflicts/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCountryStats_Year2020_ReturnsEthiopia()
    {
        var stats = await _client.GetFromJsonAsync<List<CountryStatDto>>("/api/countries/stats?year=2020", Json);

        Assert.NotNull(stats);
        var ethiopia = Assert.Single(stats);
        Assert.Equal("ETH", ethiopia.IsoCode);
        Assert.Equal(20888, ethiopia.BattleDeaths);
    }

    [Fact]
    public async Task PostChat_EmptyQuestion_Returns400()
    {
        // An empty question is rejected before the AI is called, so Ollama is not needed
        var response = await _client.PostAsJsonAsync("/api/chat", new ChatRequestDto(""), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}