using Microsoft.EntityFrameworkCore;
using RegulatorIQ.Models;

namespace RegulatorIQ.Data
{
    public class RegulatorIQContext : DbContext
    {
        public RegulatorIQContext(DbContextOptions<RegulatorIQContext> options) : base(options)
        {
        }

        public DbSet<RegulatoryAgency> RegulatoryAgencies { get; set; }
        public DbSet<RegulatoryDocument> RegulatoryDocuments { get; set; }
        public DbSet<DocumentAnalysis> DocumentAnalyses { get; set; }
        public DbSet<ComplianceRequirement> ComplianceRequirements { get; set; }
        public DbSet<ComplianceFramework> ComplianceFrameworks { get; set; }
        public DbSet<FrameworkRegulationMapping> FrameworkRegulationMappings { get; set; }
        public DbSet<ChangeImpactAssessment> ChangeImpactAssessments { get; set; }
        public DbSet<RegulatoryAlert> RegulatoryAlerts { get; set; }
        public DbSet<RegulatoryAuditLog> RegulatoryAuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure PostgreSQL-specific features
            modelBuilder.HasPostgresExtension("uuid-ossp");

            // Configure indexes
            modelBuilder.Entity<RegulatoryDocument>()
                .HasIndex(d => new { d.AgencyId, d.PublicationDate })
                .HasDatabaseName("idx_documents_agency_date");

            modelBuilder.Entity<RegulatoryDocument>()
                .HasIndex(d => d.EffectiveDate)
                .HasDatabaseName("idx_documents_effective_date")
                .HasFilter("effective_date IS NOT NULL");

            modelBuilder.Entity<RegulatoryDocument>()
                .HasIndex(d => d.PriorityScore)
                .HasDatabaseName("idx_documents_priority");

            modelBuilder.Entity<ComplianceRequirement>()
                .HasIndex(cr => cr.Deadline)
                .HasDatabaseName("idx_requirements_deadline")
                .HasFilter("deadline IS NOT NULL");

            modelBuilder.Entity<RegulatoryAlert>()
                .HasIndex(a => new { a.Status, a.CreatedAt })
                .HasDatabaseName("idx_alerts_status_created");

            modelBuilder.Entity<ComplianceFramework>()
                .HasIndex(f => f.CompanyId)
                .HasDatabaseName("idx_frameworks_company");

            // Configure JSON columns
            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.Classification)
                .HasColumnType("jsonb");

            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.EntitiesExtracted)
                .HasColumnType("jsonb");

            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.ComplianceRequirements)
                .HasColumnType("jsonb");

            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.ImpactAssessment)
                .HasColumnType("jsonb");

            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.TimelineAnalysis)
                .HasColumnType("jsonb");

            modelBuilder.Entity<DocumentAnalysis>()
                .Property(da => da.ActionableItems)
                .HasColumnType("jsonb");

            modelBuilder.Entity<ComplianceFramework>()
                .Property(cf => cf.FrameworkData)
                .HasColumnType("jsonb");

            modelBuilder.Entity<RegulatoryAlert>()
                .Property(a => a.AlertData)
                .HasColumnType("jsonb");

            modelBuilder.Entity<ChangeImpactAssessment>()
                .Property(c => c.AffectedProcesses)
                .HasColumnType("jsonb");

            modelBuilder.Entity<ChangeImpactAssessment>()
                .Property(c => c.RequiredUpdates)
                .HasColumnType("jsonb");

            modelBuilder.Entity<ChangeImpactAssessment>()
                .Property(c => c.TimelineConflicts)
                .HasColumnType("jsonb");

            // Configure relationships
            modelBuilder.Entity<RegulatoryDocument>()
                .HasOne(d => d.Agency)
                .WithMany()
                .HasForeignKey(d => d.AgencyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DocumentAnalysis>()
                .HasOne(da => da.Document)
                .WithMany(d => d.Analyses)
                .HasForeignKey(da => da.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComplianceRequirement>()
                .HasOne(cr => cr.Document)
                .WithMany(d => d.ComplianceRequirements)
                .HasForeignKey(cr => cr.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FrameworkRegulationMapping>()
                .HasOne(m => m.Framework)
                .WithMany(f => f.RegulationMappings)
                .HasForeignKey(m => m.FrameworkId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FrameworkRegulationMapping>()
                .HasOne(m => m.Document)
                .WithMany()
                .HasForeignKey(m => m.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChangeImpactAssessment>()
                .HasOne(c => c.Document)
                .WithMany(d => d.ImpactAssessments)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChangeImpactAssessment>()
                .HasOne(c => c.Framework)
                .WithMany(f => f.ImpactAssessments)
                .HasForeignKey(c => c.FrameworkId)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}
