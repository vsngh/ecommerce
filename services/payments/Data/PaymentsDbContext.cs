using Microsoft.EntityFrameworkCore;
using Payments.Api.Entities;

namespace Payments.Api.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");

            entity.HasKey(payment => payment.Id);

            entity.Property(payment => payment.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(payment => payment.Amount)
                .HasPrecision(18, 2);

            entity.Property(payment => payment.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(payment => payment.PaymentMethod)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(payment => payment.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(payment => payment.ProviderReference)
                .HasMaxLength(200);

            entity.Property(payment => payment.CreatedAt)
                .IsRequired();

            entity.HasIndex(payment => payment.OrderId);
            entity.HasIndex(payment => payment.UserId);
            entity.HasIndex(payment => payment.Status);
            entity.HasIndex(payment => payment.ProviderReference);
        });
    }
}
