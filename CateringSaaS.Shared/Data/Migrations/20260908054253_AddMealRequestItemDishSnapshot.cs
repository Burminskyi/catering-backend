using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMealRequestItemDishSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DishId",
                table: "employee_meal_request_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DishName",
                table: "employee_meal_request_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Backfill snapshots for existing meal-request lines from current menu/dish data.
            migrationBuilder.Sql("""
                UPDATE employee_meal_request_items i
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
            migrationBuilder.DropColumn(
                name: "DishId",
                table: "employee_meal_request_items");

            migrationBuilder.DropColumn(
                name: "DishName",
                table: "employee_meal_request_items");
        }
    }
}
