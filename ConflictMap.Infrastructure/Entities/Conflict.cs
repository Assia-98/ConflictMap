using ConflictMap.Contracts;

namespace ConflictMap.Infrastructure.Entities;

public class Conflict
{
    public int ConflictId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string SideA { get; set; } = string.Empty;
    public string SideB { get; set; } = string.Empty;
    public ConflictType Type { get; set; }
    public Region Region { get; set; }
    public int StartYear { get; set; }

    // Navigation properties
    public List<ConflictYear> Years { get; set; } = [];
    public List<Country> Countries { get; set; } = [];
}
