namespace ConflictMap.Infrastructure.Entities;

// The episode: what can change from year to year
public class ConflictYear
{
    public int Id { get; set; }
    public int ConflictId { get; set; }          // foreign key
    public int Year { get; set; }
    public string SideA { get; set; } = string.Empty;   // NEW: the sides can change
    public string SideB { get; set; } = string.Empty;   // NEW
    public int Intensity { get; set; }            // 1 = minor, 2 = war
    public int? BattleDeathsBest { get; set; }
    public int? BattleDeathsLow { get; set; }
    public int? BattleDeathsHigh { get; set; }

    // Navigation property
    public Conflict Conflict { get; set; } = null!;
}
