using ConflictMap.Infrastructure.Data;
using ConflictMap.Infrastructure.DataSources;
using ConflictMap.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConflictMap.Infrastructure.Import;

// Fetches UCDP rows and saves them as Conflict + ConflictYear + Country (normalisation)
public class ConflictImportService(IConflictDataSource source, AppDbContext db)
{
    public async Task<int> ImportAsync(CancellationToken ct = default)
    {
        // Skip if data already exists, so we don't import twice
        if (await db.Conflicts.AnyAsync(ct))
            return 0;

        var rows = await source.GetConflictRowsAsync(ct);
        var conflicts = new Dictionary<int, Conflict>();
        var countries = new Dictionary<int, Country>();   // one entry per GW code

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
                    Type = UcdpMapper.ToConflictType(row.TypeOfConflict),
                    Region = UcdpMapper.ToRegion(row.Region),
                    StartYear = UcdpMapper.ToYear(row.StartDate)
                };
                conflicts.Add(conflictId, conflict);
            }

            // The episode: one ConflictYear per row, with that year's sides
            conflict.Years.Add(new ConflictYear
            {
                Year = int.Parse(row.Year),
                SideA = row.SideA,
                SideB = row.SideB,
                Intensity = int.Parse(row.IntensityLevel)
            });

            // The countries. Names are only taken from rows with ONE country,
            // because the order of names and codes can differ in multi-country rows.
            var gwCodes = UcdpMapper.ToGwCodes(row.GwnoLoc);
            var isSingleCountry = gwCodes.Count == 1;

            foreach (var gwCode in gwCodes)
            {
                if (!countries.TryGetValue(gwCode, out var country))
                {
                    country = new Country
                    {
                        GwCode = gwCode,
                        Name = isSingleCountry ? row.Location.Trim() : $"GW {gwCode}",
                        IsoCode = GwIsoCodes.ToIso(gwCode)
                    };
                    countries.Add(gwCode, country);
                }
                else if (isSingleCountry && country.Name.StartsWith("GW "))
                {
                    // We now have a safe name for a country that only had a placeholder
                    country.Name = row.Location.Trim();
                }

                // Link the country to the conflict (only once)
                if (!conflict.Countries.Contains(country))
                    conflict.Countries.Add(country);
            }
        }

        // Countries that never had a single-country row get a name from the fallback list
        foreach (var country in countries.Values.Where(c => c.Name.StartsWith("GW ")))
            country.Name = GwIsoCodes.FallbackName(country.GwCode) ?? country.Name;

        // NEW: battle deaths (BRD). Several dyads can share a conflict and year, so we sum them.
        var deathRows = await source.GetBattleDeathsRowsAsync(ct);

        var deathsPerConflictYear = deathRows
            .GroupBy(d => (ConflictId: int.Parse(d.ConflictId), Year: int.Parse(d.Year)))
            .ToDictionary(
                g => g.Key,
                g => (Best: g.Sum(d => ParseOrZero(d.BdBest)),
                      Low: g.Sum(d => ParseOrZero(d.BdLow)),
                      High: g.Sum(d => ParseOrZero(d.BdHigh))));

        // NEW: put the deaths on the matching episode (conflict + year)
        foreach (var conflict in conflicts.Values)
        {
            foreach (var conflictYear in conflict.Years)
            {
                if (deathsPerConflictYear.TryGetValue((conflict.ConflictId, conflictYear.Year), out var deaths))
                {
                    conflictYear.BattleDeathsBest = deaths.Best;
                    conflictYear.BattleDeathsLow = deaths.Low;
                    conflictYear.BattleDeathsHigh = deaths.High;
                }
            }
        }

        db.Conflicts.AddRange(conflicts.Values);
        await db.SaveChangesAsync(ct);

        return conflicts.Count;
    }

    // "27" -> 27, "" -> 0
    private static int ParseOrZero(string value) => int.TryParse(value, out var n) ? n : 0;
}