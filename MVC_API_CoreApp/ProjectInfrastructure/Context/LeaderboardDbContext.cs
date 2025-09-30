using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectInfrastructure.Models;

namespace ProjectInfrastructure.Context;

public partial class LeaderboardDbContext : IdentityDbContext<User>
{
    public DbSet<LeaderboardModel?> Leaderboards { get; set; }
    public DbSet<LeaderboardRecordModel> LeaderboardsRecords { get; set; }
    
    public LeaderboardDbContext()
    {
    }

    public LeaderboardDbContext(DbContextOptions<LeaderboardDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeaderboardModel>()
            .HasMany(l => l.Records)
            .WithOne(r => r.Leaderboard)
            .HasForeignKey(r => r.LeaderboardId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeaderboardRecordModel>()
            .Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(30);

        modelBuilder.Entity<LeaderboardRecordModel>()
            .Property(r => r.UpdatedTime)
            .IsRequired();

        modelBuilder.Entity<LeaderboardRecordModel>()
            .Property(r => r.Place)
            .IsRequired();

        modelBuilder.Entity<LeaderboardRecordModel>()
            .Property(r => r.Value)
            .IsRequired();

        // Fix for NormalizedUserName
        modelBuilder.Entity<User>(b =>
        {
            b.Property(u => u.NormalizedUserName)
                .HasMaxLength(256)
                .IsRequired(false);
        });

        base.OnModelCreating(modelBuilder);
    }

}
