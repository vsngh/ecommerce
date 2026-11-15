using Microsoft.EntityFrameworkCore;
using Products.Api.Entities;

namespace Products.Api.Data;

public class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(product => product.Id);

            entity.Property(product => product.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(product => product.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(product => product.Description)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(product => product.Price)
                .HasPrecision(18, 2);

            entity.Property(product => product.CreatedAt)
                .IsRequired();

            entity.HasIndex(product => product.Name);
            entity.HasIndex(product => product.CreatedAt);
        });
    }
}
