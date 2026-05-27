using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AdministrativeUnit> AdministrativeUnits { get; set; }
    public DbSet<Strategy> Strategies { get; set; }
    public DbSet<StrategicGoal> StrategicGoals { get; set; }
    public DbSet<OperationalGoal> OperationalGoals { get; set; }
    public DbSet<ProgramTask> ProgramTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Administrative Unit
        modelBuilder.Entity<AdministrativeUnit>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Strategies)
                .WithOne(x => x.AdministrativeUnit)
                .HasForeignKey(x => x.AdministrativeUnitId);

            ConfigureBaseEntity(e);
            e.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);
            e.Property(x => x.Type)
                .HasConversion<string>();
        });

        // Strategy
        modelBuilder.Entity<Strategy>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.StrategicGoals)
                .WithOne(x => x.Strategy)
                .HasForeignKey(x => x.StrategyId);

            ConfigureBaseEntity(e);
            e.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);
        });

        // Strategic Goal
        modelBuilder.Entity<StrategicGoal>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.OperationalGoals)
                .WithOne(x => x.StrategicGoal)
                .HasForeignKey(x => x.StrategicGoalId);

            ConfigureBaseEntity(e);
            e.Property(x => x.Label)
                .IsRequired()
                .HasMaxLength(20);
            e.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);
        });

        // Operational Goal
        modelBuilder.Entity<OperationalGoal>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.ProgramTasks)
                .WithOne(x => x.OperationalGoal)
                .HasForeignKey(x => x.OperationalGoalId);

            ConfigureBaseEntity(e);
            e.Property(x => x.Label)
                .IsRequired()
                .HasMaxLength(20);
            e.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);
        });

        // Program Task
        modelBuilder.Entity<ProgramTask>(e =>
        {
            e.HasKey(x => x.Id);

            ConfigureBaseEntity(e);
            e.Property(x => x.Label)
                .IsRequired()
                .HasMaxLength(20);
            e.Property(x => x.Description)
                .IsRequired()
                .HasColumnType("text");
        });
    }

    private static void ConfigureBaseEntity<T>(EntityTypeBuilder<T> e)
        where T : BaseEntity
    {
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");
    }
}
