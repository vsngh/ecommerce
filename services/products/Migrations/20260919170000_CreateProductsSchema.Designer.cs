using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Products.Api.Data;

#nullable disable

namespace Products.Api.Migrations;

[DbContext(typeof(ProductsDbContext))]
[Migration("20260919170000_CreateProductsSchema")]
partial class CreateProductsSchema
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("Products.Api.Entities.Product", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier")
                .HasDefaultValueSql("newsequentialid()");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Description")
                .IsRequired()
                .HasMaxLength(2000)
                .HasColumnType("nvarchar(2000)");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            b.Property<decimal>("Price")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.Property<int>("StockQuantity")
                .HasColumnType("int");

            b.HasKey("Id");

            b.HasIndex("CreatedAt");

            b.HasIndex("Name");

            b.ToTable("Products", (string?)null);
        });
#pragma warning restore 612, 618
    }
}
