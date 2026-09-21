using Microsoft.EntityFrameworkCore;
using Orders.Api.Entities;

namespace Orders.Api.Data;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(order => order.Id);

            entity.Property(order => order.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(order => order.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(order => order.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(order => order.OrderedAt)
                .IsRequired();

            entity.HasIndex(order => order.UserId);
            entity.HasIndex(order => order.Status);
            entity.HasIndex(order => order.OrderedAt);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");

            entity.HasKey(orderItem => orderItem.Id);

            entity.Property(orderItem => orderItem.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(orderItem => orderItem.UnitPrice)
                .HasPrecision(18, 2);

            entity.HasOne(orderItem => orderItem.Order)
                .WithMany(order => order.OrderItems)
                .HasForeignKey(orderItem => orderItem.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(orderItem => orderItem.OrderId);
            entity.HasIndex(orderItem => orderItem.ProductId);
        });
    }
}
