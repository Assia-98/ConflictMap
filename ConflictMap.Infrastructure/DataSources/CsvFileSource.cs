using System.Globalization;
using ConflictMap.Infrastructure.DataSources.Ucdp;
using CsvHelper;
using CsvHelper.Configuration;

namespace ConflictMap.Infrastructure.DataSources;

// Strategy pattern: same contract as the API source,
// but the data is read from local CSV files instead.
public class CsvFileSource : IConflictDataSource
{
    private readonly string _conflictsFilePath;
    private readonly string _deathsFilePath;

    public CsvFileSource(string conflictsFilePath, string deathsFilePath)
    {
        _conflictsFilePath = conflictsFilePath;
        _deathsFilePath = deathsFilePath;
    }

    public Task<List<UcdpConflictRow>> GetConflictRowsAsync(CancellationToken ct = default)
        => ReadCsvAsync<UcdpConflictRow>(_conflictsFilePath, ct);

    public Task<List<UcdpDeathsRow>> GetBattleDeathsRowsAsync(CancellationToken ct = default)
        => ReadCsvAsync<UcdpDeathsRow>(_deathsFilePath, ct);

    // Generic helper: reads any CSV file into a list of T.
    private static async Task<List<T>> ReadCsvAsync<T>(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}", filePath);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // Makes snake_case headers match PascalCase properties:
            // "conflict_id" -> "conflictid" and ConflictId -> "conflictid"
            PrepareHeaderForMatch = args => args.Header.Replace("_", "").ToLowerInvariant()
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        var rows = new List<T>();

        // Columns in the file that have no matching property are simply ignored.
        await foreach (var row in csv.GetRecordsAsync<T>(ct))
        {
            rows.Add(row);
        }

        return rows;
    }
}