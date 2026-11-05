using ConflictMap.Contracts;
using ConflictMap.Infrastructure.Data;
using ConflictMap.Infrastructure.Entities;
using ConflictMap.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConflictMap.Tests.Unit;

public class ConflictServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ConflictService _service;

    // Runs before EVERY test: a fresh in-memory database with test data
    public ConflictServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        SeedTestData();

        _service = new ConflictService(_db);
    }

    private void SeedTestData()
    {
        var ethiopia = new Country { GwCode = 530, Name = "Ethiopia", IsoCode = "ETH" };
        var india = new Country { GwCode = 750, Name = "India", IsoCode = "IND" };

        _db.Conflicts.AddRange(
            new Conflict
            {
                ConflictId = 267, Location = "Ethiopia", Type = ConflictType.Intrastate,
                Region = Region.Africa, StartYear = 1976, Countries = [ethiopia],
                Years =
                [
                    new ConflictYear { Year = 2020, SideA = "Government of Ethiopia", SideB = "TPLF",
                                       Intensity = 2, BattleDeathsBest = 20888 }
                ]
            },
            new Conflict
            {
                ConflictId = 11342, Location = "India", Type = ConflictType.Intrastate,
                Region = Region.Asia, StartYear = 1997, Countries = [india],
                Years =
                [
                    new ConflictYear { Year = 2012, SideA = "Government of India", SideB = "GNLA",
                                       Intensity = 1, BattleDeathsBest = 27 },
                    new ConflictYear { Year = 2020, SideA = "Government of India", SideB = "CPI-Maoist",
                                       Intensity = 1, BattleDeathsBest = 100 }
                ]
            });

        _db.SaveChanges();
        _db.ChangeTracker.Clear();   // forget the objects, so the tests really read from the database
    }

    [Fact]
    public async Task GetConflictsAsync_FilterOnYearAndRegion_ReturnsOnlyMatching()
    {
        var result = await _service.GetConflictsAsync(new ConflictQuery(Year: 2020, Region: Region.Africa));

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Ethiopia", result.Items[0].Location);
    }

    [Theory]
    [InlineData("ETH")]        // as the map sends it
    [InlineData("Ethiopia")]   // as a name
    public async Task GetConflictsAsync_CountryAsIsoOrName_FindsSameConflict(string country)
    {
        var result = await _service.GetConflictsAsync(new ConflictQuery(Year: 2020, Country: country));

        var item = Assert.Single(result.Items);
        Assert.Equal(267, item.ConflictId);
    }

    [Fact]
    public async Task GetConflictsAsync_SmallPageSize_ReturnsOnePageButFullTotal()
    {
        var result = await _service.GetConflictsAsync(new ConflictQuery(Year: 2020, PageSize: 1));

        Assert.Equal(2, result.TotalCount);   // two conflicts in 2020 in total
        Assert.Single(result.Items);          // but only one on this page
    }

    [Fact]
    public async Task GetConflictAsync_UnknownId_ReturnsNull()
    {
        Assert.Null(await _service.GetConflictAsync(99999));
    }

    [Fact]
    public async Task GetConflictAsync_SeveralYears_ShowsSidesFromLatestYear()
    {
        // Regression test: the sides used to be stored once, from the first year
        var conflict = await _service.GetConflictAsync(11342);

        Assert.NotNull(conflict);
        Assert.Equal(2, conflict.Years.Count);
        Assert.Equal("CPI-Maoist", conflict.SideB);
    }

    [Fact]
    public async Task GetCountryStatsAsync_Year2020_MostAffectedCountryFirst()
    {
        var stats = await _service.GetCountryStatsAsync(2020);

        Assert.Equal(2, stats.Count);
        Assert.Equal("ETH", stats[0].IsoCode);
        Assert.Equal(20888, stats[0].BattleDeaths);
        Assert.Equal(2, stats[0].MaxIntensity);
    }

    // Runs after EVERY test: throw the database away
    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}