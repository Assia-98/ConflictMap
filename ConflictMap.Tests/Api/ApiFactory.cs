using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Data;
using ConflictMap.Infrastructure.Entities;
using ConflictMap.Infrastructure.Import;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConflictMap.Tests.Api;

// Starts the whole API for tests, but with a test database and without the UCDP import
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            // 1. Do not import from UCDP while testing
            var importer = services.Single(d => d.ImplementationType == typeof(UcdpImportBackgroundService));
            services.Remove(importer);

            // 2. Use an in-memory database instead of conflictmap.db
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    // Creates the tables and a small amount of test data, once
    public void SeedOnce()
    {
        if (_seeded) return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        db.Conflicts.Add(new Conflict
        {
            ConflictId = 267, Location = "Ethiopia", Type = ConflictType.Intrastate,
            Region = Region.Africa, StartYear = 1976,
            Countries = [new Country { GwCode = 530, Name = "Ethiopia", IsoCode = "ETH" }],
            Years =
            [
                new ConflictYear { Year = 2020, SideA = "Government of Ethiopia", SideB = "TPLF",
                                   Intensity = 2, BattleDeathsBest = 20888 }
            ]
        });
        db.SaveChanges();

        _seeded = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}