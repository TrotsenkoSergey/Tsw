using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tsw.EventBus.Outbox.Migrations
{
  /// <inheritdoc />
  public partial class Initial : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "IntegrationEventLogs",
          columns: table => new
          {
            EventId = table.Column<Guid>(type: "uuid", nullable: false),
            EventTypeName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            State = table.Column<int>(type: "integer", nullable: false),
            TimesSent = table.Column<int>(type: "integer", nullable: false),
            CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            TransactionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_IntegrationEventLogs", x => x.EventId);
          });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "IntegrationEventLogs");
    }
  }
}
