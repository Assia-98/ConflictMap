using System.Text.Json.Serialization;

namespace ConflictMap.Infrastructure.DataSources.Ucdp;

// One row from UCDP's Armed Conflict Dataset = one conflict in one year.
// All values are strings, because that is how UCDP sends them.
public class UcdpConflictRow
{
    [JsonPropertyName("conflict_id")]
    public string ConflictId { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("side_a")]
    public string SideA { get; set; } = string.Empty;

    [JsonPropertyName("side_b")]
    public string SideB { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;

    [JsonPropertyName("intensity_level")]
    public string IntensityLevel { get; set; } = string.Empty;

    [JsonPropertyName("type_of_conflict")]
    public string TypeOfConflict { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("gwno_loc")]
    public string GwnoLoc { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;
}
