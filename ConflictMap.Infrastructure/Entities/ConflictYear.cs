namespace ConflictMap.Infrastructure.Entities;

public class ConflictYear
{
    public int Id { get; set; }
    public int ConflictId { get; set; }          // foreign key
    public int Year { get; set; }
    public int Intensity { get; set; }            // 1 = minor, 2 = war
    public int? BattleDeathsBest { get; set; }
    public int? BattleDeathsLow { get; set; }
    public int? BattleDeathsHigh { get; set; }

    // Navigation property
    public Conflict Conflict { get; set; } = null!;
}
