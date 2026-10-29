
using ConflictMap.Infrastructure.Data;
using ConflictMap.Infrastructure.DataSources;
using ConflictMap.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConflictMap.Infrastructure.Import;

// Fetches UCDP rows and saves them as Conflict + ConflictYear (normalisation)
public class ConflictImportService(IConflictDataSource source, AppDbContext db)
{
    public async Task<int> ImportAsync(CancellationToken ct = default)
    {
        // Skip if data already exists, so we don't import twice
        if (await db.Conflicts.AnyAsync(ct))
            return 0;

        var rows = await source.GetConflictRowsAsync(ct);
        var conflicts = new Dictionary<int, Conflict>();

        foreach (var row in rows)
        {
            var conflictId = int.Parse(row.ConflictId);

            // The series: create each conflict only once
            if (!conflicts.TryGetValue(conflictId, out var conflict))
            {
                conflict = new Conflict
                {
                    ConflictId = conflictId,
                    Location = row.Location,
                    SideA = row.SideA,
                    SideB = row.SideB,
                    Type = UcdpMapper.ToConflictType(row.TypeOfConflict),
                    Region = UcdpMapper.ToRegion(row.Region),
                    StartYear = UcdpMapper.ToYear(row.StartDate)
                };
                conflicts.Add(conflictId, conflict);
            }

            // The episode: one ConflictYear per row
            conflict.Years.Add(new ConflictYear
            {
                Year = int.Parse(row.Year),
                Intensity = int.Parse(row.IntensityLevel)
            });
        }

        db.Conflicts.AddRange(conflicts.Values);
        await db.SaveChangesAsync(ct);

        return conflicts.Count;
    }
}