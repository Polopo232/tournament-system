using Microsoft.EntityFrameworkCore;
using TournamentSystem.Api.Models;

namespace TournamentSystem.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<RatingHistory> RatingHistory => Set<RatingHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            entity.Property(t => t.Game).HasMaxLength(50).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(1000);
            entity.Property(t => t.Format).HasConversion<string>().HasMaxLength(30);
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);

            entity.HasOne(t => t.Organizer)
                .WithMany(u => u.OrganizedTournaments)
                .HasForeignKey(t => t.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Registration>(entity =>
        {
            entity.HasIndex(r => new { r.TournamentId, r.PlayerId }).IsUnique();

            entity.HasOne(r => r.Tournament)
                .WithMany(t => t.Registrations)
                .HasForeignKey(r => r.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Player)
                .WithMany(u => u.Registrations)
                .HasForeignKey(r => r.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Player1)
                .WithMany()
                .HasForeignKey(m => m.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Player2)
                .WithMany()
                .HasForeignKey(m => m.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Winner)
                .WithMany()
                .HasForeignKey(m => m.WinnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RatingHistory>(entity =>
        {
            entity.HasOne(h => h.Player)
                .WithMany(u => u.RatingHistory)
                .HasForeignKey(h => h.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.Match)
                .WithMany()
                .HasForeignKey(h => h.MatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
