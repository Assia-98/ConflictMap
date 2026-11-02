using ConflictMap.Infrastructure.DataSources.Ucdp;

namespace ConflictMap.Infrastructure.DataSources;

// Strategy pattern: every data source can fetch conflict rows,
// but each one does it in its own way (API, CSV file, ...).
public interface IConflictDataSource
{
    Task<List<UcdpConflictRow>> GetConflictRowsAsync(CancellationToken ct = default);
    Task<List<UcdpDeathsRow>> GetBattleDeathsRowsAsync(CancellationToken ct = default);   // NEW

}
