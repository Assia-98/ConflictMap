using ConflictMap.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConflictMap.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conflict> Conflicts => Set<Conflict>();
    public DbSet<ConflictYear> ConflictYears => Set<ConflictYear>();
    public DbSet<Country> Countries => Set<Country>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ConflictId comes from UCDP, so the database must not generate it
        modelBuilder.Entity<Conflict>()
            .Property(c => c.ConflictId)
            .ValueGeneratedNever();

        // Only one row per conflict and year
        modelBuilder.Entity<ConflictYear>()
            .HasIndex(cy => new { cy.ConflictId, cy.Year })
            .IsUnique();
    }
}