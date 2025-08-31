namespace Application;

using Application.Features.Part.Projections;
using Library;
using Microsoft.EntityFrameworkCore;

public class PartsDbContext(DbContextOptions<PartsDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<PartSummary> PartSummary { get; set; } = null!;
    public DbSet<PartDetail> PartDetails { get; set; } = null!;
    public DbSet<PartTransaction> PartTransactions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>().HasKey(e => e.AggregateId);
        modelBuilder.Entity<PartSummary>().HasKey(p => p.Sku);

        // PartDetail configuration
        modelBuilder.Entity<PartDetail>(entity =>
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
        modelBuilder.Entity<PartTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Type).IsRequired().HasMaxLength(20);
            entity.Property(t => t.Justification).IsRequired().HasMaxLength(500);
            entity.Property(t => t.PartSku).IsRequired();
            
            // Index for efficient querying
            entity.HasIndex(t => new { t.PartSku, t.Timestamp });
        });

        base.OnModelCreating(modelBuilder);
    }
}