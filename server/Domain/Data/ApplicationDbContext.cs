using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<TokenInfo> TokenInfos { get; set; }
    public DbSet<Region> Regions { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<Community> Communities { get; set; }
    public DbSet<Settlement> Settlements { get; set; }
    public DbSet<Strategy> Strategies { get; set; }
    public DbSet<StrategicGoal> StrategicGoals { get; set; }
    public DbSet<OperationalGoal> OperationalGoals { get; set; }
    public DbSet<ProgramTask> ProgramTasks { get; set; }
    public DbSet<KeywordMetric> KeywordMetrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(e =>
        {
            ConfigureBaseEntity(e);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).IsRequired().HasMaxLength(50);
        });

        // TokenInfo
        modelBuilder.Entity<TokenInfo>(e =>
        {
            ConfigureBaseEntity(e);
            e.Property(x => x.RefreshToken).IsRequired().HasMaxLength(512);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Region
        modelBuilder.Entity<Region>(e =>
        {
            ConfigureAdministrativeEntity(e);
            e.HasMany(x => x.Strategies)
                .WithOne(x => x.Region)
                .HasForeignKey(x => x.RegionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.WebsiteUrl).HasMaxLength(512);
            e.Property(x => x.StrategiesUrl).HasMaxLength(512);
        });

        // District
        modelBuilder.Entity<District>(e =>
        {
            ConfigureAdministrativeEntity(e);
            e.HasOne(x => x.Region)
                .WithMany(x => x.Districts)
                .HasForeignKey(x => x.RegionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Strategies)
                .WithOne(x => x.District)
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Community
        modelBuilder.Entity<Community>(e =>
        {
            ConfigureAdministrativeEntity(e);
            e.HasOne(x => x.Region)
                .WithMany(x => x.Communities)
                .HasForeignKey(x => x.RegionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.District)
                .WithMany(x => x.Communities)
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Strategies)
                .WithOne(x => x.Community)
                .HasForeignKey(x => x.CommunityId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.WebsiteUrl).HasMaxLength(512);
            e.Property(x => x.StrategiesUrl).HasMaxLength(512);
        });

        // Settlement
        modelBuilder.Entity<Settlement>(e =>
        {
            ConfigureBaseEntity(e);
            e.HasIndex(x => x.KattotgId).IsUnique();
            e.HasOne(x => x.Region)
                .WithMany(x => x.Settlements)
                .HasForeignKey(x => x.RegionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Community)
                .WithMany(x => x.Settlements)
                .HasForeignKey(x => x.CommunityId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.District)
                .WithMany(x => x.Settlements)
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ParentSettlement)
                .WithMany(x => x.ChildSettlements)
                .HasForeignKey(x => x.ParentSettlementId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(x => x.KattotgId).IsRequired().HasMaxLength(20);
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.Property(x => x.NameFull).IsRequired().HasMaxLength(255);
            e.Property(x => x.ParentType).IsRequired().HasMaxLength(50);
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
            e.Property(x => x.StrategyUrl)
                .HasMaxLength(512);
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

        // Keyword Metric
        modelBuilder.Entity<KeywordMetric>(e =>
        {
            ConfigureBaseEntity(e);
            e.ToTable("keyword_metrics");

            e.Property(x => x.StrategyId)
                .HasColumnName("strategy_id")
                .IsRequired();
            e.Property(x => x.Keyword)
                .HasColumnName("keyword")
                .IsRequired()
                .HasMaxLength(255);
            e.Property(x => x.Count)
                .HasColumnName("count")
                .IsRequired();

            e.HasOne(x => x.Strategy)
                .WithMany(x => x.KeywordMetrics)
                .HasForeignKey(x => x.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.Keyword)
                .HasDatabaseName("idx_keyword_metrics_keyword");

            e.HasIndex(x => new { x.StrategyId, x.Keyword })
                .IsUnique()
                .HasDatabaseName("uq_strategy_keyword");
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Strategy && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (Strategy)entityEntry.Entity;
            entity.UpdatedAt = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                Entry(entity).Property(x => x.CreatedAt).IsModified = false;
            }
        }
    }

    private static void ConfigureBaseEntity<T>(EntityTypeBuilder<T> e)
        where T : BaseEntity
    {
        e.HasKey(x => x.Id);

        e.Property(x => x.Id)
            .HasDefaultValueSql("gen_random_uuid()");
    }

    private static void ConfigureAdministrativeEntity<T>(EntityTypeBuilder<T> e)
        where T : AdministrativeEntity
    {
        ConfigureBaseEntity(e);

        e.HasIndex(x => x.KattotgId).IsUnique();
        e.Property(x => x.KattotgId).IsRequired().HasMaxLength(20);
        e.Property(x => x.Name).IsRequired().HasMaxLength(150);
        e.Property(x => x.NameFull).IsRequired().HasMaxLength(255);
        e.Property(x => x.Category).IsRequired().HasMaxLength(50);
        e.Property(x => x.ImgUrl).HasMaxLength(512);
    }
}
