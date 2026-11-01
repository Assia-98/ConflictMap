using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ConflictMap.Infrastructure.Services;

public interface IConflictService
{
    Task<PagedResult<ConflictSummaryDto>> GetConflictsAsync(ConflictQuery query, CancellationToken ct = default);
    Task<ConflictDetailDto?> GetConflictAsync(int id, CancellationToken ct = default);
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

        if (!string.IsNullOrWhiteSpace(query.Country))
            rows = rows.Where(cy => cy.Conflict.Location.Contains(query.Country));

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
                cy.SideA,                 // CHANGED: the sides of this year
                cy.SideB,                 // CHANGED
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

        // CHANGED: the detail page shows the sides from the most recent year
        var latest = years.Last();

        var yearDtos = years
            .Select(y => new ConflictYearDto(
                y.Year, y.Intensity, y.BattleDeathsBest, y.BattleDeathsLow, y.BattleDeathsHigh))
            .ToList();

        return new ConflictDetailDto(
            conflict.ConflictId, conflict.Location, latest.SideA, latest.SideB,
            conflict.Type, conflict.Region, conflict.StartYear, yearDtos, Source);
    }
}