using Microsoft.EntityFrameworkCore;
using InventoryManagementAPI.Models;

namespace InventoryManagementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // Product configurations
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.SKU).IsUnique();
                entity.Property(p => p.Price).HasPrecision(18, 2);
                entity.Property(p => p.TaxRate).HasPrecision(5, 2);
            });

            // CompanyProfile configurations
            modelBuilder.Entity<CompanyProfile>(entity =>
            {
                entity.Property(p => p.DefaultTaxRate).HasPrecision(5, 2);
            });

            // Customer configurations
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasIndex(c => c.Oib);
                entity.HasIndex(c => c.Email);
            });

            // Invoice configurations
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceNumber).IsUnique();
                entity.Property(i => i.SubTotal).HasPrecision(18, 2);
                entity.Property(i => i.TaxAmount).HasPrecision(18, 2);
                entity.Property(i => i.TotalAmount).HasPrecision(18, 2);
                entity.Property(i => i.TaxRate).HasPrecision(5, 2);

                // Configure relationship with Customer
                entity.HasOne(i => i.Customer)
                      .WithMany(c => c.Invoices)
                      .HasForeignKey(i => i.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // InvoiceItem configurations
            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
                entity.Property(i => i.Quantity).HasPrecision(18, 3);
                entity.Property(i => i.TaxRate).HasPrecision(5, 2);
                entity.Property(i => i.LineTotal).HasPrecision(18, 2);
                entity.Property(i => i.LineTaxAmount).HasPrecision(18, 2);

                // Configure relationships
                entity.HasOne(i => i.Invoice)
                      .WithMany(inv => inv.Items)
                      .HasForeignKey(i => i.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Product)
                      .WithMany(p => p.InvoiceItems)
                      .HasForeignKey(i => i.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}