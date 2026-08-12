using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_MenuItemId",
                table: "order_items",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_OrderId",
                table: "order_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_WorkspaceId",
                table: "order_items",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_PlacedByUserId",
                table: "orders",
                column: "PlacedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId",
                table: "orders",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_ClientCompanyId_TargetDate",
                table: "orders",
                columns: new[] { "WorkspaceId", "ClientCompanyId", "TargetDate" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_PlacedByUserId",
                table: "orders",
                columns: new[] { "WorkspaceId", "PlacedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_Status",
                table: "orders",
                columns: new[] { "WorkspaceId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_orders_users_PlacedByUserId",
                table: "orders",
                column: "PlacedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_menu_items_MenuItemId",
                table: "order_items",
                column: "MenuItemId",
                principalTable: "menu_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_menu_items_MenuItemId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_users_PlacedByUserId",
                table: "orders");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
