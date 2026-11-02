using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConflictMap.Infrastructure.Services;

public interface IConflictService
{
    Task<PagedResult<ConflictSummaryDto>> GetConflictsAsync(ConflictQuery query, CancellationToken ct = default);
    Task<ConflictDetailDto?> GetConflictAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CountryStatDto>> GetCountryStatsAsync(int year, CancellationToken ct = default);   // NEW
}

// Reads from the database and returns DTOs from the contract
public class ConflictService(AppDbContext db) : IConflictService
{
    private const string Source =
        "UCDP/PRIO Armed Conflict Dataset + UCDP Battle-Related Deaths Dataset, version 26.1";

    public async Task<PagedResult<ConflictSummaryDto>> GetConflictsAsync(
        ConflictQuery query, CancellationToken ct = default)
    {
        // Start with all conflict years, then add filters one by one
        var rows = db.ConflictYears.AsNoTracking().AsQueryable();

        if (query.Year is not null)
            rows = rows.Where(cy => cy.Year == query.Year);
        
        // Country: accepts both an ISO code (from the map, e.g. "ETH") and a name (e.g. "Ethiopia")
        if (!string.IsNullOrWhiteSpace(query.Country))
            rows = rows.Where(cy => cy.Conflict.Countries.Any(c =>
                c.IsoCode == query.Country || c.Name.Contains(query.Country)));

        if (query.Region is not null)
            rows = rows.Where(cy => cy.Conflict.Region == query.Region);

        if (query.Type is not null)
            rows = rows.Where(cy => cy.Conflict.Type == query.Type);

        var totalCount = await rows.CountAsync(ct);

        // Paging: skip earlier pages, take one page
        var items = await rows
            .OrderByDescending(cy => cy.Year)
            .ThenBy(cy => cy.Conflict.Location)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(cy => new ConflictSummaryDto(
                cy.ConflictId,
                cy.Conflict.Location,
                cy.SideA,
                cy.SideB,
                cy.Year,
                cy.Conflict.Type,
                cy.Intensity,
                cy.BattleDeathsBest))
            .ToListAsync(ct);

        return new PagedResult<ConflictSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ConflictDetailDto?> GetConflictAsync(int id, CancellationToken ct = default)
    {
        var conflict = await db.Conflicts
            .AsNoTracking()
            .Include(c => c.Years)
            .FirstOrDefaultAsync(c => c.ConflictId == id, ct);

        if (conflict is null) return null;

        var years = conflict.Years.OrderBy(y => y.Year).ToList();

        // The detail page shows the sides from the most recent year
        var latest = years.Last();

        var yearDtos = years
            .Select(y => new ConflictYearDto(
                y.Year, y.Intensity, y.BattleDeathsBest, y.BattleDeathsLow, y.BattleDeathsHigh))
            .ToList();

        return new ConflictDetailDto(
            conflict.ConflictId, conflict.Location, latest.SideA, latest.SideB,
            conflict.Type, conflict.Region, conflict.StartYear, yearDtos, Source);
    }

    // NEW: one row per country with conflicts in the given year (used to colour the map)
    public async Task<IReadOnlyList<CountryStatDto>> GetCountryStatsAsync(
        int year, CancellationToken ct = default)
    {
        // 1. Get one row per (conflict year, country) from the database
        var rows = await db.ConflictYears
            .AsNoTracking()
            .Where(cy => cy.Year == year)
            .SelectMany(cy => cy.Conflict.Countries, (cy, country) => new
            {
                cy.ConflictId,
                country.IsoCode,
                country.Name,
                cy.Intensity,
                Deaths = cy.BattleDeathsBest ?? 0
            })
            .ToListAsync(ct);

        // 2. Group by ISO code in memory (several historical codes can share one ISO code)
        return rows
            .GroupBy(r => r.IsoCode)
            .Select(g => new CountryStatDto(
                IsoCode: g.Key,
                CountryName: g.First().Name,
                Year: year,
                ActiveConflicts: g.Select(r => r.ConflictId).Distinct().Count(),
                BattleDeaths: g.Sum(r => r.Deaths),
                MaxIntensity: g.Max(r => r.Intensity)))
            .OrderByDescending(s => s.MaxIntensity)
            .ThenByDescending(s => s.ActiveConflicts)
            .ToList();
    }
}