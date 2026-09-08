using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStockConsumeAndDishSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StockConsumedAt",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DishId",
                table: "order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DishName",
                table: "order_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_StockConsumedAt",
                table: "orders",
                columns: new[] { "WorkspaceId", "StockConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_DishId",
                table: "order_items",
                column: "DishId");

            migrationBuilder.Sql("""
                UPDATE order_items i
                SET "DishId" = mi."DishId",
                    "DishName" = d."Name"
                FROM menu_items mi
                INNER JOIN dishes d ON d."Id" = mi."DishId"
                WHERE mi."Id" = i."MenuItemId"
                  AND (i."DishName" = '' OR i."DishId" IS NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_WorkspaceId_StockConsumedAt",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_items_DishId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "StockConsumedAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DishId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "DishName",
                table: "order_items");
        }
    }
}
