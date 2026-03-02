using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegulatorIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertResolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "RegulatoryAlerts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "RegulatoryAlerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedBy",
                table: "RegulatoryAlerts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "RegulatoryAlerts");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "RegulatoryAlerts");

            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                table: "RegulatoryAlerts");
        }
    }
}
