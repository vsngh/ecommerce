using System;
using Cart.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Api.Migrations;

[DbContext(typeof(CartDbContext))]
[Migration("20260919173000_CreateCartSchema")]
partial class CreateCartSchema
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("Cart.Api.Entities.Cart", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier")
                .HasDefaultValueSql("newsequentialid()");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<DateTime>("UpdatedAt")
                .HasColumnType("datetime2");

            b.Property<Guid>("UserId")
                .HasColumnType("uniqueidentifier");

            b.HasKey("Id");

            b.HasIndex("Status");

            b.HasIndex("UpdatedAt");

            b.HasIndex("UserId");

            b.ToTable("Carts", (string?)null);
        });

        modelBuilder.Entity("Cart.Api.Entities.CartItem", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier")
                .HasDefaultValueSql("newsequentialid()");

            b.Property<DateTime>("AddedAt")
                .HasColumnType("datetime2");

            b.Property<Guid>("CartId")
                .HasColumnType("uniqueidentifier");

            b.Property<Guid>("ProductId")
                .HasColumnType("uniqueidentifier");

            b.Property<int>("Quantity")
                .HasColumnType("int");

            b.Property<decimal>("UnitPrice")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.HasKey("Id");

            b.HasIndex("CartId");

            b.HasIndex("ProductId");

            b.HasIndex("CartId", "ProductId")
                .IsUnique();

            b.ToTable("CartItems", (string?)null);
        });

        modelBuilder.Entity("Cart.Api.Entities.CartItem", b =>
        {
            b.HasOne("Cart.Api.Entities.Cart", "Cart")
                .WithMany("CartItems")
                .HasForeignKey("CartId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Cart");
        });

        modelBuilder.Entity("Cart.Api.Entities.Cart", b =>
        {
            b.Navigation("CartItems");
        });
#pragma warning restore 612, 618
    }
}
