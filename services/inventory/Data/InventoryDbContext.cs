using Inventory.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("InventoryItems");

            entity.HasKey(inventoryItem => inventoryItem.Id);

            entity.Property(inventoryItem => inventoryItem.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(inventoryItem => inventoryItem.UpdatedAt)
                .IsRequired();

            entity.HasIndex(inventoryItem => inventoryItem.ProductId)
                .IsUnique();

            entity.HasIndex(inventoryItem => inventoryItem.UpdatedAt);
        });
    }
}
