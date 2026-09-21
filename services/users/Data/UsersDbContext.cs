using Microsoft.EntityFrameworkCore;
using Users.Api.Entities;

namespace Users.Api.Data;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Id)
                .HasDefaultValueSql("newsequentialid()");

            entity.Property(user => user.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(user => user.Role)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.HasIndex(user => user.Role);
            entity.HasIndex(user => user.CreatedAt);
        });
    }
}
