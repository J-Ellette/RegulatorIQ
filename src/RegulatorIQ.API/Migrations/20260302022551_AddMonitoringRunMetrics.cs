using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegulatorIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoringRunMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitoringRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DocumentsFetched = table.Column<int>(type: "integer", nullable: false),
                    DocumentsAdded = table.Column<int>(type: "integer", nullable: false),
                    DocumentsSkipped = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    SourceMetrics = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_monitoring_runs_status",
                table: "MonitoringRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_monitoring_runs_type_started",
                table: "MonitoringRuns",
                columns: new[] { "RunType", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonitoringRuns");
        }
    }
}
