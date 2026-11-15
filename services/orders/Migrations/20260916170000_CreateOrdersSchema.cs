using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Api.Migrations;

public partial class CreateOrdersSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "OrderItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderItems_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrderItems_OrderId",
            table: "OrderItems",
            column: "OrderId");

        migrationBuilder.CreateIndex(
            name: "IX_OrderItems_ProductId",
            table: "OrderItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_OrderedAt",
            table: "Orders",
            column: "OrderedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status",
            table: "Orders",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_UserId",
            table: "Orders",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OrderItems");

        migrationBuilder.DropTable(
            name: "Orders");
    }
}
