using ConflictMap.Api.Ai;
using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Data;
using ConflictMap.Infrastructure.Entities;
using ConflictMap.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConflictMap.Tests.Unit;

public class ConflictToolsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ConflictTools _tools;

    // Runs before EVERY test: a fresh in-memory database with test data
    public ConflictToolsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        SeedTestData();

        _tools = new ConflictTools(new ConflictService(_db), _db, NullLogger<ConflictTools>.Instance);
    }

    // Three conflicts in Africa (two in East Africa, one in West Africa) and one in Asia, all in 2020
    private void SeedTestData()
    {
        var ethiopia = new Country { GwCode = 530, Name = "Ethiopia", IsoCode = "ETH" };
        var somalia = new Country { GwCode = 520, Name = "Somalia", IsoCode = "SOM" };
        var nigeria = new Country { GwCode = 475, Name = "Nigeria", IsoCode = "NGA" };
        var india = new Country { GwCode = 750, Name = "India", IsoCode = "IND" };

        _db.Conflicts.AddRange(
            CreateConflict(267, "Ethiopia", Region.Africa, ethiopia, "Government of Ethiopia", "TPLF", 20888),
            CreateConflict(337, "Somalia", Region.Africa, somalia, "Government of Somalia", "Al-Shabaab", 1901),
            CreateConflict(13641, "Nigeria", Region.Africa, nigeria, "Government of Nigeria", "IS", 1559),
            CreateConflict(11342, "India", Region.Asia, india, "Government of India", "CPI-Maoist", 100));

        _db.SaveChanges();
        _db.ChangeTracker.Clear();   // forget the objects, so the tests really read from the database
    }

    private static Conflict CreateConflict(
        int id, string location, Region region, Country country, string sideA, string sideB, int deaths) =>
        new()
        {
            ConflictId = id, Location = location, Type = ConflictType.Intrastate,
            Region = region, StartYear = 2000, Countries = [country],
            Years =
            [
                new ConflictYear { Year = 2020, SideA = sideA, SideB = sideB,
                                   Intensity = deaths >= 1000 ? 2 : 1, BattleDeathsBest = deaths }
            ]
        };

    [Fact]
    public async Task GetConflicts_SubRegionEastAfrica_ReturnsOnlyEastAfrica()
    {
        var result = await _tools.GetConflicts(year: 2020, subRegion: "EastAfrica");

        Assert.Equal(2, result.TotalCount);   // the count after filtering, not the whole of Africa
        Assert.Equal([267, 337], result.Conflicts.Select(c => c.ConflictId).Order());
    }

    [Fact]
    public async Task GetConflicts_SubRegion_HighlightsOnlyReturnedConflicts()
    {
        // Regression test: the map used to highlight every conflict in the whole region
        await _tools.GetConflicts(year: 2020, subRegion: "EastAfrica");

        Assert.Equal([267, 337], _tools.FetchedConflictIds.Order());
    }

    [Theory]
    [InlineData("eastafrica")]   // lower case
    [InlineData("EASTAFRICA")]   // upper case
    public async Task GetConflicts_SubRegionAnyCase_IsUnderstood(string subRegion)
    {
        var result = await _tools.GetConflicts(year: 2020, subRegion: subRegion);

        Assert.Equal(2, result.TotalCount);
    }

    [Theory]
    [InlineData("Mars")]   // unknown sub-region
    [InlineData("null")]   // small models sometimes send the text "null"
    public async Task GetConflicts_UnknownSubRegion_GivesNoFilter(string subRegion)
    {
        var result = await _tools.GetConflicts(year: 2020, subRegion: subRegion);

        Assert.Equal(4, result.TotalCount);   // all conflicts in 2020
    }

    [Fact]
    public async Task GetConflicts_RegionWithoutSubRegion_ReturnsWholeRegion()
    {
        var result = await _tools.GetConflicts(year: 2020, region: "Africa");

        Assert.Equal(3, result.TotalCount);   // Ethiopia, Somalia and Nigeria
    }

    // Runs after EVERY test: throw the database away
    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}