using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Payments.Api.Data;

#nullable disable

namespace Payments.Api.Migrations;

[DbContext(typeof(PaymentsDbContext))]
[Migration("20260919174000_CreatePaymentsSchema")]
partial class CreatePaymentsSchema
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("Payments.Api.Entities.Payment", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uniqueidentifier")
                .HasDefaultValueSql("newsequentialid()");

            b.Property<decimal>("Amount")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            b.Property<DateTime?>("CompletedAt")
                .HasColumnType("datetime2");

            b.Property<DateTime>("CreatedAt")
                .HasColumnType("datetime2");

            b.Property<string>("Currency")
                .IsRequired()
                .HasMaxLength(3)
                .HasColumnType("nvarchar(3)");

            b.Property<Guid>("OrderId")
                .HasColumnType("uniqueidentifier");

            b.Property<string>("PaymentMethod")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            b.Property<string>("ProviderReference")
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            b.Property<string>("Status")
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("nvarchar(50)");

            b.Property<Guid>("UserId")
                .HasColumnType("uniqueidentifier");

            b.HasKey("Id");

            b.HasIndex("OrderId");

            b.HasIndex("ProviderReference");

            b.HasIndex("Status");

            b.HasIndex("UserId");

            b.ToTable("Payments", (string?)null);
        });
#pragma warning restore 612, 618
    }
}
