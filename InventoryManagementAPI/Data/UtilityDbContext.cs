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

            // CompanyProfile relationships - NO CASCADE
            modelBuilder.Entity<CompanyProfile>(entity =>
            {
                entity.HasMany(c => c.Products)
                    .WithOne(p => p.Company)
                    .HasForeignKey(p => p.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Customers)
                    .WithOne(cu => cu.Company)
                    .HasForeignKey(cu => cu.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.Invoices)
                    .WithOne(i => i.Company)
                    .HasForeignKey(i => i.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
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
            // Invoice relationships - NO CASCADE to prevent cycles
            // ========================================
            modelBuilder.Entity<Invoice>(entity =>
            {
                // Invoice -> Customer (NO CASCADE)
                entity.HasOne(i => i.Customer)
                    .WithMany(c => c.Invoices)
                    .HasForeignKey(i => i.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Invoice -> Company (already configured above)

                // Invoice -> InvoiceItems (CASCADE allowed here since it's single path)
                entity.HasMany(i => i.Items)
                    .WithOne(ii => ii.Invoice)
                    .HasForeignKey(ii => ii.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}