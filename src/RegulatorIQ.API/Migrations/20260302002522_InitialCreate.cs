using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RegulatorIQ.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "ComplianceFrameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FrameworkVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IndustrySegments = table.Column<string[]>(type: "text[]", nullable: true),
                    GeographicScope = table.Column<string[]>(type: "text[]", nullable: true),
                    FrameworkData = table.Column<string>(type: "jsonb", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryAgencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AgencyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Jurisdiction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactInfo = table.Column<string>(type: "jsonb", nullable: true),
                    MonitoringEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatoryAgencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldData = table.Column<string>(type: "jsonb", nullable: true),
                    NewData = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ChangeReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatoryAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgencyId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CommentDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ComplianceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RawContent = table.Column<string>(type: "text", nullable: true),
                    ProcessedContent = table.Column<string>(type: "text", nullable: true),
                    DocketNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FederalRegisterNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CfrCitation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriorityScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatoryDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulatoryDocuments_RegulatoryAgencies_AgencyId",
                        column: x => x.AgencyId,
                        principalTable: "RegulatoryAgencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChangeImpactAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImpactScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AffectedProcesses = table.Column<string>(type: "jsonb", nullable: true),
                    RequiredUpdates = table.Column<string>(type: "jsonb", nullable: true),
                    TimelineConflicts = table.Column<string>(type: "jsonb", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    ImplementationComplexity = table.Column<int>(type: "integer", nullable: true),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AssessmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssessedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeImpactAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeImpactAssessments_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChangeImpactAssessments_RegulatoryDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "RegulatoryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequirementText = table.Column<string>(type: "text", nullable: false),
                    RequirementType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Applicability = table.Column<string[]>(type: "text[]", nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ImplementationGuidance = table.Column<string>(type: "text", nullable: true),
                    EstimatedCostImpact = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    ComplexityLevel = table.Column<int>(type: "integer", nullable: true),
                    Citation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceRequirements_RegulatoryDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "RegulatoryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisVersion = table.Column<int>(type: "integer", nullable: false),
                    Classification = table.Column<string>(type: "jsonb", nullable: true),
                    EntitiesExtracted = table.Column<string>(type: "jsonb", nullable: true),
                    ComplianceRequirements = table.Column<string>(type: "jsonb", nullable: true),
                    ImpactAssessment = table.Column<string>(type: "jsonb", nullable: true),
                    TimelineAnalysis = table.Column<string>(type: "jsonb", nullable: true),
                    AffectedParties = table.Column<string[]>(type: "text[]", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    ActionableItems = table.Column<string>(type: "jsonb", nullable: true),
                    RelatedRegulations = table.Column<string[]>(type: "text[]", nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnalyzerVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentAnalyses_RegulatoryDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "RegulatoryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegulatoryAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: true),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    AlertData = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatoryAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegulatoryAlerts_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegulatoryAlerts_RegulatoryDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "RegulatoryDocuments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FrameworkRegulationMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequirementId = table.Column<Guid>(type: "uuid", nullable: true),
                    MappingType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ComplianceStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImplementationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AssignedTo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkRegulationMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrameworkRegulationMappings_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FrameworkRegulationMappings_ComplianceRequirements_Requirem~",
                        column: x => x.RequirementId,
                        principalTable: "ComplianceRequirements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FrameworkRegulationMappings_RegulatoryDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "RegulatoryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeImpactAssessments_DocumentId",
                table: "ChangeImpactAssessments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ChangeImpactAssessments_FrameworkId",
                table: "ChangeImpactAssessments",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "idx_frameworks_company",
                table: "ComplianceFrameworks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "idx_requirements_deadline",
                table: "ComplianceRequirements",
                column: "Deadline",
                filter: "deadline IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceRequirements_DocumentId",
                table: "ComplianceRequirements",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAnalyses_DocumentId",
                table: "DocumentAnalyses",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkRegulationMappings_DocumentId",
                table: "FrameworkRegulationMappings",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkRegulationMappings_FrameworkId",
                table: "FrameworkRegulationMappings",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkRegulationMappings_RequirementId",
                table: "FrameworkRegulationMappings",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "idx_alerts_status_created",
                table: "RegulatoryAlerts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryAlerts_DocumentId",
                table: "RegulatoryAlerts",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryAlerts_FrameworkId",
                table: "RegulatoryAlerts",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "idx_documents_agency_date",
                table: "RegulatoryDocuments",
                columns: new[] { "AgencyId", "PublicationDate" });

            migrationBuilder.CreateIndex(
                name: "idx_documents_effective_date",
                table: "RegulatoryDocuments",
                column: "EffectiveDate",
                filter: "effective_date IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_documents_priority",
                table: "RegulatoryDocuments",
                column: "PriorityScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeImpactAssessments");

            migrationBuilder.DropTable(
                name: "DocumentAnalyses");

            migrationBuilder.DropTable(
                name: "FrameworkRegulationMappings");

            migrationBuilder.DropTable(
                name: "RegulatoryAlerts");

            migrationBuilder.DropTable(
                name: "RegulatoryAuditLogs");

            migrationBuilder.DropTable(
                name: "ComplianceRequirements");

            migrationBuilder.DropTable(
                name: "ComplianceFrameworks");

            migrationBuilder.DropTable(
                name: "RegulatoryDocuments");

            migrationBuilder.DropTable(
                name: "RegulatoryAgencies");
        }
    }
}
