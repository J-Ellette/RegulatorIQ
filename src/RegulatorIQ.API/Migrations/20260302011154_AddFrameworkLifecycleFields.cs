using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegulatorIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFrameworkLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextReviewDate",
                table: "ComplianceFrameworks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "ComplianceFrameworks",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ComplianceFrameworks",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "ComplianceFrameworks");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "ComplianceFrameworks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ComplianceFrameworks");
        }
    }
}
