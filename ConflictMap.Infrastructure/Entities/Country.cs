namespace ConflictMap.Infrastructure.Entities;

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GwCode { get; set; }               // UCDP country code
    public string IsoCode { get; set; } = string.Empty;  // e.g. "ETH", for the map

    // Navigation property
    public List<Conflict> Conflicts { get; set; } = [];
}