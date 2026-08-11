using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dishes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OutputWeight = table.Column<int>(type: "integer", nullable: false),
                    Instructions = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dishes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dish_ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dish_ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dish_ingredients_dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_days",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_menu_days_menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_menu_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_menu_items_dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_menu_items_menu_days_MenuDayId",
                        column: x => x.MenuDayId,
                        principalTable: "menu_days",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dish_ingredients_DishId",
                table: "dish_ingredients",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_dish_ingredients_DishId_IngredientId",
                table: "dish_ingredients",
                columns: new[] { "DishId", "IngredientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dish_ingredients_WorkspaceId",
                table: "dish_ingredients",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_dishes_WorkspaceId",
                table: "dishes",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_dishes_WorkspaceId_IsActive",
                table: "dishes",
                columns: new[] { "WorkspaceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_dishes_WorkspaceId_Name",
                table: "dishes",
                columns: new[] { "WorkspaceId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_menu_days_MenuId_Date",
                table: "menu_days",
                columns: new[] { "MenuId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_menu_days_WorkspaceId",
                table: "menu_days",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_DishId",
                table: "menu_items",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_MenuDayId",
                table: "menu_items",
                column: "MenuDayId");

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_MenuDayId_DishId",
                table: "menu_items",
                columns: new[] { "MenuDayId", "DishId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_menu_items_WorkspaceId",
                table: "menu_items",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_menus_WorkspaceId",
                table: "menus",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_menus_WorkspaceId_ClientCompanyId",
                table: "menus",
                columns: new[] { "WorkspaceId", "ClientCompanyId" });

            migrationBuilder.CreateIndex(
                name: "IX_menus_WorkspaceId_Status",
                table: "menus",
                columns: new[] { "WorkspaceId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dish_ingredients");

            migrationBuilder.DropTable(
                name: "menu_items");

            migrationBuilder.DropTable(
                name: "dishes");

            migrationBuilder.DropTable(
                name: "menu_days");

            migrationBuilder.DropTable(
                name: "menus");
        }
    }
}
