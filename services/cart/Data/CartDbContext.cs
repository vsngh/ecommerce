using Cart.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cart.Api.Data;

public class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cart.Api.Entities.Cart> Carts => Set<Cart.Api.Entities.Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart.Api.Entities.Cart>(entity =>
        {
            entity.ToTable("Carts");

            entity.HasKey(cart => cart.Id);

            entity.Property(cart => cart.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(cart => cart.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(cart => cart.CreatedAt)
                .IsRequired();

            entity.Property(cart => cart.UpdatedAt)
                .IsRequired();

            entity.HasIndex(cart => cart.UserId);
            entity.HasIndex(cart => cart.Status);
            entity.HasIndex(cart => cart.UpdatedAt);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");

            entity.HasKey(cartItem => cartItem.Id);

            entity.Property(cartItem => cartItem.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(cartItem => cartItem.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(cartItem => cartItem.AddedAt)
                .IsRequired();

            entity.HasOne(cartItem => cartItem.Cart)
                .WithMany(cart => cart.CartItems)
                .HasForeignKey(cartItem => cartItem.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cartItem => cartItem.CartId);
            entity.HasIndex(cartItem => cartItem.ProductId);
            entity.HasIndex(cartItem => new { cartItem.CartId, cartItem.ProductId })
                .IsUnique();
        });
    }
}
