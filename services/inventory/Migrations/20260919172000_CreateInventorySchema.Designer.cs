using System;
using Inventory.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Api.Migrations;

[DbContext(typeof(InventoryDbContext))]
[Migration("20260919172000_CreateInventorySchema")]
partial class CreateInventorySchema
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("Inventory.Api.Entities.InventoryItem", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier")
                .HasDefaultValueSql("newsequentialid()");

            b.Property<Guid>("ProductId")
                .HasColumnType("uniqueidentifier");

            b.Property<int>("QuantityAvailable")
                .HasColumnType("int");

            b.Property<int>("QuantityReserved")
                .HasColumnType("int");

            b.Property<DateTime>("UpdatedAt")
                .HasColumnType("datetime2");

            b.HasKey("Id");

            b.HasIndex("ProductId")
                .IsUnique();

            b.HasIndex("UpdatedAt");

            b.ToTable("InventoryItems", (string?)null);
        });
#pragma warning restore 612, 618
    }
}
