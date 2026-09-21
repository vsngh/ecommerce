using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Api.Migrations;

public partial class CreateInventorySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InventoryItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuantityAvailable = table.Column<int>(type: "int", nullable: false),
                QuantityReserved = table.Column<int>(type: "int", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryItems", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InventoryItems_ProductId",
            table: "InventoryItems",
            column: "ProductId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InventoryItems_UpdatedAt",
            table: "InventoryItems",
            column: "UpdatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "InventoryItems");
    }
}
