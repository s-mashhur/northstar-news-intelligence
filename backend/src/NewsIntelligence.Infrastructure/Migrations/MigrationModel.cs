using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace NewsIntelligence.Infrastructure.Migrations;

/// <summary>
/// Canonical relational model used by the initial migration metadata and snapshot.
/// Keep this aligned with AppDbContext. The explicit provider annotations are
/// important for EF Core 9 pending-model validation with Npgsql.
/// </summary>
internal static class MigrationModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.19")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("NewsIntelligence.Domain.Article", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Description")
                .HasColumnType("text");

            b.Property<string>("ExternalId")
                .IsRequired()
                .HasMaxLength(300)
                .HasColumnType("character varying(300)");

            b.Property<string>("ImageUrl")
                .HasMaxLength(2048)
                .HasColumnType("character varying(2048)");

            b.Property<string>("Language")
                .HasMaxLength(40)
                .HasColumnType("character varying(40)");

            b.Property<Guid>("MonitorId")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("PublishedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Sentiment")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)");

            b.Property<double?>("SentimentScore")
                .HasColumnType("double precision");

            b.Property<string>("Source")
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("character varying(256)");

            b.Property<string>("Title")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)");

            b.Property<string>("Url")
                .IsRequired()
                .HasMaxLength(2048)
                .HasColumnType("character varying(2048)");

            b.HasKey("Id");
            b.HasIndex("PublishedAt");
            b.HasIndex("MonitorId", "ExternalId").IsUnique();
            b.ToTable("Articles");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.NewsMonitor", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<int>("Days")
                .HasColumnType("integer");

            b.Property<int>("IntervalMinutes")
                .HasColumnType("integer");

            b.Property<bool>("IsActive")
                .HasColumnType("boolean");

            b.Property<string>("Language")
                .HasMaxLength(40)
                .HasColumnType("character varying(40)");

            b.Property<DateTimeOffset?>("LastRunAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(120)
                .HasColumnType("character varying(120)");

            b.Property<string>("Query")
                .IsRequired()
                .HasMaxLength(1000)
                .HasColumnType("character varying(1000)");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.HasKey("Id");
            b.HasIndex("UserId");
            b.ToTable("Monitors");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.RefreshToken", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTimeOffset>("ExpiresAt")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTimeOffset?>("RevokedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("TokenHash")
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("character varying(128)");

            b.Property<Guid>("UserId")
                .HasColumnType("uuid");

            b.HasKey("Id");
            b.HasIndex("TokenHash").IsUnique();
            b.HasIndex("UserId");
            b.ToTable("RefreshTokens");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.User", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAt")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnType("character varying(320)");

            b.Property<string>("PasswordHash")
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnType("character varying(512)");

            b.Property<string>("Role")
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("character varying(32)");

            b.HasKey("Id");
            b.HasIndex("Email").IsUnique();
            b.ToTable("Users");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.Article", b =>
        {
            b.HasOne("NewsIntelligence.Domain.NewsMonitor", "Monitor")
                .WithMany("Articles")
                .HasForeignKey("MonitorId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Monitor");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.NewsMonitor", b =>
        {
            b.HasOne("NewsIntelligence.Domain.User", "User")
                .WithMany("Monitors")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("User");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.RefreshToken", b =>
        {
            b.HasOne("NewsIntelligence.Domain.User", "User")
                .WithMany("RefreshTokens")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("User");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.NewsMonitor", b =>
        {
            b.Navigation("Articles");
        });

        modelBuilder.Entity("NewsIntelligence.Domain.User", b =>
        {
            b.Navigation("Monitors");
            b.Navigation("RefreshTokens");
        });
#pragma warning restore 612, 618
    }
}
