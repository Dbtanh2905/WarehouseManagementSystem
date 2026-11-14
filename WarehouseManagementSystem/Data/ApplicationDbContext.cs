using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementSystem.Models;

namespace WarehouseManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
        public DbSet<StockReceiptDetail> StockReceiptDetails => Set<StockReceiptDetail>();
        public DbSet<StockIssue> StockIssues => Set<StockIssue>();
        public DbSet<StockIssueDetail> StockIssueDetails => Set<StockIssueDetail>();
        public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(e => e.ProductCode).IsUnique();
                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Supplier)
                    .WithMany(s => s.Products)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WarehouseLocation configuration
            modelBuilder.Entity<WarehouseLocation>(entity =>
            {
                entity.HasIndex(e => e.LocationCode).IsUnique();
            });

            // Inventory configuration: Unique (ProductId, LocationId)
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasIndex(e => new { e.ProductId, e.LocationId }).IsUnique();

                entity.ToTable(t => t.HasCheckConstraint("CK_Inventory_Quantity_NonNegative", "[Quantity] >= 0"));

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Inventories)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Inventories)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StockReceipt configuration
            modelBuilder.Entity<StockReceipt>(entity =>
            {
                entity.HasIndex(e => e.ReceiptCode).IsUnique();

                entity.HasOne(e => e.Supplier)
                    .WithMany(s => s.StockReceipts)
                    .HasForeignKey(e => e.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.CreatedStockReceipts)
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StockReceiptDetail configuration
            modelBuilder.Entity<StockReceiptDetail>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_StockReceiptDetail_Quantity_Positive", "[Quantity] > 0"));

                entity.HasOne(e => e.StockReceipt)
                    .WithMany(r => r.ReceiptDetails)
                    .HasForeignKey(e => e.StockReceiptId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.StockReceiptDetails)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.StockReceiptDetails)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StockIssue configuration
            modelBuilder.Entity<StockIssue>(entity =>
            {
                entity.HasIndex(e => e.IssueCode).IsUnique();

                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.CreatedStockIssues)
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StockIssueDetail configuration
            modelBuilder.Entity<StockIssueDetail>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_StockIssueDetail_Quantity_Positive", "[Quantity] > 0"));

                entity.HasOne(e => e.StockIssue)
                    .WithMany(i => i.IssueDetails)
                    .HasForeignKey(e => e.StockIssueId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.StockIssueDetails)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.StockIssueDetails)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StockTransaction configuration
            modelBuilder.Entity<StockTransaction>(entity =>
            {
                entity.ToTable(t => t.HasCheckConstraint("CK_StockTransaction_Quantity_Positive", "[Quantity] > 0"));

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.StockTransactions)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.StockTransactions)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PerformedBy)
                    .WithMany(u => u.StockTransactions)
                    .HasForeignKey(e => e.PerformedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
