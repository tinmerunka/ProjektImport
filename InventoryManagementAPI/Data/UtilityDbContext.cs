using InventoryManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementAPI.Data
{
    public class UtilityDbContext : DbContext
    {
        public UtilityDbContext(DbContextOptions<UtilityDbContext> options) : base(options) { }

        // Import tracking
        public DbSet<ImportBatch> ImportBatches { get; set; }

        // Utility tables
        public DbSet<UtilityInvoice> UtilityInvoices { get; set; }
        public DbSet<UtilityInvoiceItem> UtilityInvoiceItems { get; set; }
        public DbSet<UtilityConsumptionData> UtilityConsumptionData { get; set; }

        // Shared tables
        public DbSet<User> Users { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================================
            // ImportBatch configuration
            // ========================================
            modelBuilder.Entity<ImportBatch>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.BatchId).IsUnique();
                entity.HasIndex(e => e.ImportedAt);
                entity.Property(e => e.Status).HasConversion<string>();
            });

            // ========================================
            // UtilityInvoice configuration
            // ========================================
            modelBuilder.Entity<UtilityInvoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ImportBatchId);
                entity.HasIndex(e => e.InvoiceNumber);
                entity.HasIndex(e => e.FiscalizationStatus);
                entity.HasIndex(e => e.CreatedAt);

                // ✅ Foreign key to ImportBatch with NO ACTION to prevent cascade cycles
                entity.HasOne(e => e.ImportBatch)
                    .WithMany(b => b.Invoices)
                    .HasForeignKey(e => e.ImportBatchId)
                    .HasPrincipalKey(b => b.BatchId)
                    .OnDelete(DeleteBehavior.Restrict); // Changed to Restrict to prevent cycles

                // ✅ Configure child relationships with CASCADE
                entity.HasMany(e => e.Items)
                    .WithOne(i => i.UtilityInvoice)
                    .HasForeignKey(i => i.UtilityInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade); // Items cascade delete with invoice

                entity.HasMany(e => e.ConsumptionData)
                    .WithOne(c => c.UtilityInvoice)
                    .HasForeignKey(c => c.UtilityInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade); // Consumption data cascade delete with invoice
            });

            // ========================================
            // Disable cascade delete for shared tables to prevent conflicts
            // ========================================

            // CompanyProfile relationships - minimal configuration
            // NOTE: Products, Customers, and old Invoices models have been removed
            // Only User relationship remains
            modelBuilder.Entity<CompanyProfile>(entity =>
            {
                // CompanyProfile is kept for fiscalization settings only
                // No navigation properties to old inventory models
            });

            // User relationships - NO CASCADE
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasMany(u => u.Companies)
                    .WithOne(c => c.User)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================================
            // Minimal Invoice configuration for FINA compatibility ONLY
            // ========================================
            // NOTE: This is a minimal Invoice model used only for converting
            // UtilityInvoice to FINA format. It does NOT have Customer relationship.
            modelBuilder.Entity<Invoice>(entity =>
            {
                // No relationships configured - this is a standalone model
                // Used only for FINA fiscalization conversion
            });
        }
    }
}