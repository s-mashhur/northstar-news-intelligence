using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace NewsIntelligence.Infrastructure.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Users", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
            PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
            Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Users", x => x.Id));
        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);

        migrationBuilder.CreateTable("Monitors", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
            Query = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            Days = table.Column<int>(type: "integer", nullable: false),
            IntervalMinutes = table.Column<int>(type: "integer", nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            LastRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Monitors", x => x.Id);
            table.ForeignKey("FK_Monitors_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_Monitors_UserId", "Monitors", "UserId");

        migrationBuilder.CreateTable("RefreshTokens", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
            ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            table.ForeignKey("FK_RefreshTokens_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_RefreshTokens_TokenHash", "RefreshTokens", "TokenHash", unique: true);
        migrationBuilder.CreateIndex("IX_RefreshTokens_UserId", "RefreshTokens", "UserId");

        migrationBuilder.CreateTable("Articles", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            MonitorId = table.Column<Guid>(type: "uuid", nullable: false),
            ExternalId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
            Title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            Description = table.Column<string>(type: "text", nullable: true),
            Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
            ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
            Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
            Language = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
            Sentiment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            SentimentScore = table.Column<double>(type: "double precision", nullable: true),
            PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Articles", x => x.Id);
            table.ForeignKey("FK_Articles_Monitors_MonitorId", x => x.MonitorId, "Monitors", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_Articles_MonitorId_ExternalId", "Articles", new[] { "MonitorId", "ExternalId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Articles_PublishedAt", "Articles", "PublishedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Articles");
        migrationBuilder.DropTable("RefreshTokens");
        migrationBuilder.DropTable("Monitors");
        migrationBuilder.DropTable("Users");
    }
}
