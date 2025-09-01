namespace Application;

using Application.Features.Part.Projections;
using Application.Features.Product.Projections;
using Library;
using Microsoft.EntityFrameworkCore;

public class PartsDbContext(DbContextOptions<PartsDbContext> options) : DbContext(options)
{
    public DbSet<PartSummaryReadModel> PartSummary { get; set; } = null!;
    public DbSet<PartDetailReadModel> PartDetails { get; set; } = null!;
    public DbSet<PartTransactionReadModel> PartTransactions { get; set; } = null!;
    public DbSet<ProductSummaryReadModel> ProductSummary { get; set; } = null!;
    public DbSet<ProductDetailReadModel> ProductDetails { get; set; } = null!;
    public DbSet<ProductPartTransactionReadModel> ProductPartTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PartSummaryReadModel>().HasKey(p => p.Sku);

        // PartDetail configuration
        modelBuilder.Entity<PartDetailReadModel>(entity =>
        {
            entity.HasKey(p => p.Sku);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.SourceName).HasMaxLength(100);
            entity.Property(p => p.SourceUri).HasMaxLength(500);

            // One-to-many relationship with transactions
            entity.HasMany(p => p.Transactions)
                  .WithOne(t => t.Part)
                  .HasForeignKey(t => t.PartSku)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PartTransaction configuration
        modelBuilder.Entity<PartTransactionReadModel>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Type).IsRequired().HasMaxLength(20);
            entity.Property(t => t.Justification).IsRequired().HasMaxLength(500);
            entity.Property(t => t.PartSku).IsRequired();

            // Index for efficient querying
            entity.HasIndex(t => new { t.PartSku, t.Timestamp });
        });

        // ProductSummary configuration
        modelBuilder.Entity<ProductSummaryReadModel>(entity =>
        {
            entity.HasKey(p => p.Sku);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
        });

        // ProductDetail configuration
        modelBuilder.Entity<ProductDetailReadModel>(entity =>
        {
            entity.HasKey(p => p.Sku);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);

            // One-to-many relationship with part transactions
            entity.HasMany(p => p.PartTransactions)
                  .WithOne(t => t.Product)
                  .HasForeignKey(t => t.ProductSku)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductPartTransaction configuration
        modelBuilder.Entity<ProductPartTransactionReadModel>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Type).IsRequired().HasMaxLength(20);
            entity.Property(t => t.PartSku).IsRequired();
            entity.Property(t => t.ProductSku).IsRequired();

            // Index for efficient querying
            entity.HasIndex(t => new { t.ProductSku, t.Timestamp });
        });

        base.OnModelCreating(modelBuilder);
    }
}