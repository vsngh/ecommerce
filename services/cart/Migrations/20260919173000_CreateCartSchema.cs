using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cart.Api.Migrations;

public partial class CreateCartSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Carts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Carts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CartItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CartItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_CartItems_Carts_CartId",
                    column: x => x.CartId,
                    principalTable: "Carts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CartItems_CartId",
            table: "CartItems",
            column: "CartId");

        migrationBuilder.CreateIndex(
            name: "IX_CartItems_CartId_ProductId",
            table: "CartItems",
            columns: new[] { "CartId", "ProductId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CartItems_ProductId",
            table: "CartItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Carts_Status",
            table: "Carts",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Carts_UpdatedAt",
            table: "Carts",
            column: "UpdatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Carts_UserId",
            table: "Carts",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CartItems");

        migrationBuilder.DropTable(
            name: "Carts");
    }
}
