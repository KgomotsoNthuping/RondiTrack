using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class RondiTrackDbContext : DbContext
{
    public RondiTrackDbContext(DbContextOptions<RondiTrackDbContext> options) : base(options)
    {

    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Stokvel> Stokvels => Set<Stokvel>();

    public DbSet<StokvelMember> StokvelMembers => Set<StokvelMember>();

    public DbSet<ContributionCycle> ContributionCycles => Set<ContributionCycle>();

    public DbSet<Contribution> Contributions => Set<Contribution>();

    public DbSet<Payout> Payouts => Set<Payout>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureStokvels(modelBuilder);
        ConfigureStokvelMembers(modelBuilder);
        ConfigureContributionCycles(modelBuilder);
        ConfigureContributions(modelBuilder);
        ConfigurePayouts(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        entity.HasKey(user => user.Id);

        entity.Property(user => user.FullName).IsRequired();

        entity.Property(user => user.Email).IsRequired();

        entity.Property(user => user.PhoneNumber).IsRequired();
    }

    private static void ConfigureStokvels(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Stokvel>();

        entity.HasKey(stokvel => stokvel.Id);

        entity.Property(stokvel => stokvel.Name).IsRequired();

        entity.Property(stokvel => stokvel.MonthlyContribution).HasPrecision(18, 2);

        entity.Ignore(stokvel => stokvel.Members);
    }

    private static void ConfigureStokvelMembers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<StokvelMember>();

        entity.HasKey(member => member.Id);

        entity.HasIndex(member => new
            {
                member.StokvelId,
                member.UserId
            }).IsUnique();
    }

    private static void ConfigureContributionCycles(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ContributionCycle>();

        entity.HasKey(cycle => cycle.Id);

        entity.Property(cycle => cycle.TargetAmount).HasPrecision(18, 2);
    }

    private static void ConfigureContributions(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Contribution>();

        entity.HasKey(contribution => contribution.Id);

        entity.Property(contribution => contribution.Amount).HasPrecision(18, 2);

        entity.HasIndex(contribution => new
            {
                contribution.StokvelId,
                contribution.UserId,
                contribution.ContributionCycleId
            }).IsUnique();
    }

    private static void ConfigurePayouts(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Payout>();

        entity.HasKey(payout => payout.Id);

        entity.Property(payout => payout.Amount).HasPrecision(18, 2);

        entity.HasIndex(payout => payout.ContributionCycleId).IsUnique();
    }
}