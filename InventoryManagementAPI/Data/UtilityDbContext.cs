using Microsoft.EntityFrameworkCore;
using InventoryManagementAPI.Models;

namespace InventoryManagementAPI.Data
{
    public class UtilityDbContext : DbContext
    {
        public UtilityDbContext(DbContextOptions<UtilityDbContext> options) : base(options)
        {
        }

        // Keep only User from old system for authentication
        public DbSet<User> Users { get; set; }

        // Add CompanyProfile to utility database
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }

        // New utility tables ONLY
        public DbSet<UtilityInvoice> UtilityInvoices { get; set; }
        public DbSet<UtilityInvoiceItem> UtilityInvoiceItems { get; set; }
        public DbSet<UtilityConsumptionData> UtilityConsumptionData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations (ignore navigation properties)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Ignore(u => u.Companies); // Ignore to prevent including old relationships
            });

            // CompanyProfile configurations
            modelBuilder.Entity<CompanyProfile>(entity =>
            {
                entity.Property(p => p.DefaultTaxRate).HasPrecision(5, 2);

                // Ignore old navigation properties to prevent creating unnecessary tables
                entity.Ignore(c => c.Products);
                entity.Ignore(c => c.Customers);
                entity.Ignore(c => c.Invoices);
                entity.Ignore(c => c.User); // Don't create user relationship in utility DB
            });

            // Utility Invoice configurations
            modelBuilder.Entity<UtilityInvoice>(entity =>
            {
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.HasIndex(e => e.CustomerCode);
                entity.HasIndex(e => e.Period);
                entity.HasIndex(e => e.Building);

                entity.HasMany(e => e.Items)
                    .WithOne(e => e.UtilityInvoice)
                    .HasForeignKey(e => e.UtilityInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.ConsumptionData)
                    .WithOne(e => e.UtilityInvoice)
                    .HasForeignKey(e => e.UtilityInvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Utility Invoice Item configurations
            modelBuilder.Entity<UtilityInvoiceItem>(entity =>
            {
                entity.Property(i => i.UnitPrice).HasPrecision(18, 5);
                entity.Property(i => i.Quantity).HasPrecision(18, 3);
                entity.Property(i => i.Amount).HasPrecision(18, 2);
            });

            // Utility Consumption Data configurations
            modelBuilder.Entity<UtilityConsumptionData>(entity =>
            {
                entity.Property(c => c.ParameterValue).HasPrecision(18, 3);
            });
        }
    }
}