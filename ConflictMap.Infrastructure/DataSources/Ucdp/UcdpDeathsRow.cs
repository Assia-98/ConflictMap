using System.Text.Json.Serialization;

namespace ConflictMap.Infrastructure.DataSources.Ucdp;

// One row from UCDP's Battle-Related Deaths Dataset (one dyad in one year)
public class UcdpDeathsRow
{
    [JsonPropertyName("conflict_id")]
    public string ConflictId { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;

    [JsonPropertyName("bd_best")]
    public string BdBest { get; set; } = string.Empty;

    [JsonPropertyName("bd_low")]
    public string BdLow { get; set; } = string.Empty;

    [JsonPropertyName("bd_high")]
    public string BdHigh { get; set; } = string.Empty;
}