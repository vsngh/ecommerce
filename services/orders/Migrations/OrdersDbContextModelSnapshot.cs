using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Orders.Api.Data;

#nullable disable

namespace Orders.Api.Migrations;

[DbContext(typeof(OrdersDbContext))]
partial class OrdersDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("Orders.Api.Entities.Order", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier")
                .HasDefaultValueSql("newsequentialid()");

            b.Property<DateTime>("OrderedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<decimal>("TotalAmount")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.Property<Guid>("UserId")
                .HasColumnType("uniqueidentifier");

            b.HasKey("Id");

            b.HasIndex("OrderedAt");

            b.HasIndex("Status");

            b.HasIndex("UserId");

            b.ToTable("Orders", (string?)null);
        });

        modelBuilder.Entity("Orders.Api.Entities.OrderItem", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier")
                .HasDefaultValueSql("newsequentialid()");

            b.Property<Guid>("OrderId")
                .HasColumnType("uniqueidentifier");

            b.Property<Guid>("ProductId")
                .HasColumnType("uniqueidentifier");

            b.Property<int>("Quantity")
                .HasColumnType("int");

            b.Property<decimal>("UnitPrice")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.HasKey("Id");

            b.HasIndex("OrderId");

            b.HasIndex("ProductId");

            b.ToTable("OrderItems", (string?)null);
        });

        modelBuilder.Entity("Orders.Api.Entities.OrderItem", b =>
        {
            b.HasOne("Orders.Api.Entities.Order", "Order")
                .WithMany("OrderItems")
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("Order");
        });

        modelBuilder.Entity("Orders.Api.Entities.Order", b =>
        {
            b.Navigation("OrderItems");
        });
#pragma warning restore 612, 618
    }
}
