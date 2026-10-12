using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Domain.Repositories;

namespace Infrastructure.Data;

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

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureStokvels(modelBuilder);
        ConfigureStokvelMembers(modelBuilder);
        ConfigureContributionCycles(modelBuilder);
        ConfigureContributions(modelBuilder);
        ConfigurePayouts(modelBuilder);
        ConfigureRefreshTokens(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        entity.HasKey(user => user.Id);

        entity.Property(user => user.FullName).IsRequired();

        entity.Property(user => user.Email).IsRequired();

        entity.Property(user => user.PhoneNumber).IsRequired();

        entity.Property(user => user.Version).IsRowVersion();

        entity.Property(user => user.PasswordHash).IsRequired();

        entity.HasIndex(user => user.Email).IsUnique().HasDatabaseName("UX_Users_Email");

        entity.Property(user => user.PlatformRole).HasConversion<string>().HasDefaultValue(PlatformRole.User);
    }

    private static void ConfigureStokvels(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Stokvel>();

        entity.HasKey(stokvel => stokvel.Id);

        entity.Property(stokvel => stokvel.Name).IsRequired();

        entity.Property(stokvel => stokvel.MonthlyContribution).HasPrecision(18, 2);

        entity.Ignore(stokvel => stokvel.Members);

        entity.Property(stokvel => stokvel.Version).IsRowVersion();
    }

    private static void ConfigureStokvelMembers(ModelBuilder modelBuilder)
    {
            var entity = modelBuilder.Entity<StokvelMember>();

            entity.HasKey(member => new
            {
                member.UserId,
                member.StokvelId
            });

            entity.Property(member => member.Role)
                .HasConversion<string>()
                .HasDefaultValue(StokvelMemberRole.Member);

            entity.HasOne(member => member.User)
                .WithMany(user => user.Memberships)
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(member => member.Stokvel)
                .WithMany(stokvel => stokvel.Memberships)
                .HasForeignKey(member => member.StokvelId)
                .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureContributionCycles(ModelBuilder modelBuilder)
    {
        var entity =
            modelBuilder.Entity<ContributionCycle>();

        entity.HasKey(cycle => cycle.Id);

        entity.Property(cycle => cycle.TargetAmount)
            .HasPrecision(18, 2);

        entity.HasMany(cycle => cycle.Contributions)
            .WithOne(contribution =>
                contribution.ContributionCycle)
            .HasForeignKey(contribution =>
                contribution.ContributionCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(cycle => cycle.Version).IsRowVersion();
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
                contribution.ContributionCycleId,
                contribution.Id
            }).IsUnique().HasDatabaseName("IX_Contributions_PagedCycleQuery");

        entity.HasOne(contribution =>contribution.Member)
              .WithMany(member =>
                member.Contributions)
              .HasForeignKey(contribution => new
            {
                contribution.UserId,
                contribution.StokvelId
            })
              .HasPrincipalKey(member => new
            {
                member.UserId,
                member.StokvelId
            })
              .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePayouts(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Payout>();

        entity.HasKey(payout => payout.Id);

        entity.Property(payout => payout.Amount).HasPrecision(18, 2);

        entity.Property(payout => payout.Version).IsRowVersion();

        entity.HasIndex(payout => new
        {
            payout.StokvelId,
            payout.ContributionCycleId
        }).IsUnique().HasDatabaseName("UX_Payouts_Stokvel_Cycle");
    }

    private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RefreshToken>();

        entity.HasKey(token => token.Id);

        entity.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        entity.HasIndex(token => token.TokenHash)
            .IsUnique();

        entity.HasIndex(token => token.FamilyId);

        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(token => token.Version)
            .IsRowVersion();
    }

    
}