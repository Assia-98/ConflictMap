using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConflictMap.Infrastructure.Import;

// Runs once when the app starts and imports UCDP data if the database is empty
public class UcdpImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<UcdpImportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // The database (AppDbContext) is "scoped", so we create our own scope
            using var scope = scopeFactory.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<ConflictImportService>();

            logger.LogInformation("UCDP import starting...");
            var imported = await importer.ImportAsync(stoppingToken);

            if (imported == 0)
                logger.LogInformation("UCDP import skipped: the database already has data.");
            else
                logger.LogInformation("UCDP import finished: {Count} conflicts imported.", imported);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // The app keeps running even if the import fails, e.g. if UCDP is down
            logger.LogError(ex, "UCDP import failed.");
        }
    }
} 