using Matchcota.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matchcota.Infrastructure.Persistence;

public sealed class MatchcotaDbContext(DbContextOptions<MatchcotaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Dog> Dogs => Set<Dog>();
    public DbSet<DogMedia> DogMedia => Set<DogMedia>();
    public DbSet<Swipe> Swipes => Set<Swipe>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MatchReadStatus> MatchReadStatuses => Set<MatchReadStatus>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<SafetyReport> SafetyReports => Set<SafetyReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(200);
            entity.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(x => x.DisplayName).IsRequired().HasMaxLength(120);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(200);
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(200);

            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.RevokedAtUtc });

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Dog>(entity =>
        {
            entity.ToTable("Dogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
            entity.Property(x => x.Breed).IsRequired().HasMaxLength(120);
            entity.Property(x => x.Bio).HasMaxLength(500);
            entity.Property(x => x.Latitude).HasColumnType("double precision");
            entity.Property(x => x.Longitude).HasColumnType("double precision");
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasOne(x => x.Owner)
                .WithMany(x => x.Dogs)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DogMedia>(entity =>
        {
            entity.ToTable("DogMedia");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MediaUrl).IsRequired().HasMaxLength(500);
            entity.Property(x => x.MediaType).IsRequired().HasMaxLength(40);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasOne(x => x.Dog)
                .WithMany(x => x.Media)
                .HasForeignKey(x => x.DogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Swipe>(entity =>
        {
            entity.ToTable("Swipes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasIndex(x => new { x.SourceDogId, x.TargetDogId }).IsUnique();
            entity.HasOne(x => x.SourceDog)
                .WithMany()
                .HasForeignKey(x => x.SourceDogId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TargetDog)
                .WithMany()
                .HasForeignKey(x => x.TargetDogId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("Matches");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MatchedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasIndex(x => new { x.DogAId, x.DogBId }).IsUnique();
            entity.HasOne(x => x.DogA)
                .WithMany()
                .HasForeignKey(x => x.DogAId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DogB)
                .WithMany()
                .HasForeignKey(x => x.DogBId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired().HasMaxLength(4000);
            entity.Property(x => x.SentAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasOne(x => x.Match)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SenderDog)
                .WithMany()
                .HasForeignKey(x => x.SenderDogId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MatchReadStatus>(entity =>
        {
            entity.ToTable("MatchReadStatuses");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.MatchId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Match)
                .WithMany()
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Block>(entity =>
        {
            entity.ToTable("Blocks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasIndex(x => new { x.BlockerUserId, x.BlockedDogId }).IsUnique();
            entity.HasOne(x => x.BlockerUser)
                .WithMany()
                .HasForeignKey(x => x.BlockerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.BlockedDog)
                .WithMany()
                .HasForeignKey(x => x.BlockedDogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SafetyReport>(entity =>
        {
            entity.ToTable("SafetyReports");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Category).IsRequired().HasMaxLength(60);
            entity.Property(x => x.Detail).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("timezone('utc', now())");
            entity.HasIndex(x => x.ReportedByUserId);
            entity.HasIndex(x => x.ReportedDogId);
            entity.HasOne(x => x.ReportedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReportedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ReportedDog)
                .WithMany()
                .HasForeignKey(x => x.ReportedDogId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
