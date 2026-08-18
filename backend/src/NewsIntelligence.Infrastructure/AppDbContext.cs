using Microsoft.EntityFrameworkCore;
using NewsIntelligence.Domain;

namespace NewsIntelligence.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<NewsMonitor> Monitors => Set<NewsMonitor>();
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.ToTable("Users");
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable("RefreshTokens");
        });

        modelBuilder.Entity<NewsMonitor>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Query).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Language).HasMaxLength(40);
            e.HasOne(x => x.User).WithMany(x => x.Monitors).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable("Monitors");
        });

        modelBuilder.Entity<Article>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ExternalId).HasMaxLength(300).IsRequired();
            e.Property(x => x.Title).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.Property(x => x.ImageUrl).HasMaxLength(2048);
            e.Property(x => x.Source).HasMaxLength(256).IsRequired();
            e.Property(x => x.Language).HasMaxLength(40);
            e.Property(x => x.Sentiment).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.MonitorId, x.ExternalId }).IsUnique();
            e.HasIndex(x => x.PublishedAt);
            e.HasOne(x => x.Monitor).WithMany(x => x.Articles).HasForeignKey(x => x.MonitorId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable("Articles");
        });
    }
}
