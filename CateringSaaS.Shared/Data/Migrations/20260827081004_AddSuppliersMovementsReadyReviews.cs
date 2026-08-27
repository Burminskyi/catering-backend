using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliersMovementsReadyReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_WorkspaceId",
                table: "suppliers",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_WorkspaceId_IsActive",
                table: "suppliers",
                columns: new[] { "WorkspaceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_WorkspaceId_Name",
                table: "suppliers",
                columns: new[] { "WorkspaceId", "Name" });

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "stock_batches",
                type: "uuid",
                nullable: true);

            // Backfill legacy batches with a workspace-scoped placeholder supplier.
            migrationBuilder.Sql("""
                INSERT INTO suppliers ("Id", "WorkspaceId", "Name", "Phone", "Email", "Notes", "IsActive")
                SELECT gen_random_uuid(), sb."WorkspaceId", 'Legacy / Unknown', NULL, NULL, 'Auto-created for pre-supplier stock batches', TRUE
                FROM (SELECT DISTINCT "WorkspaceId" FROM stock_batches) sb
                WHERE NOT EXISTS (
                    SELECT 1 FROM suppliers s
                    WHERE s."WorkspaceId" = sb."WorkspaceId" AND s."Name" = 'Legacy / Unknown');

                UPDATE stock_batches b
                SET "SupplierId" = s."Id"
                FROM suppliers s
                WHERE b."SupplierId" IS NULL
                  AND s."WorkspaceId" = b."WorkspaceId"
                  AND s."Name" = 'Legacy / Unknown';
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "stock_batches",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_SupplierId",
                table: "stock_batches",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_batches_suppliers_SupplierId",
                table: "stock_batches",
                column: "SupplierId",
                principalTable: "suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<bool>(
                name: "IsReclamation",
                table: "meal_reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE meal_reviews
                SET "IsReclamation" = TRUE
                WHERE "Rating" <= 3;
                """);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "meal_reviews",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_reviews_WorkspaceId_IsReclamation_CreatedAt",
                table: "meal_reviews",
                columns: new[] { "WorkspaceId", "IsReclamation", "CreatedAt" });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SignedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_movements_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_IngredientId",
                table: "inventory_movements",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_WorkspaceId",
                table: "inventory_movements",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_WorkspaceId_CreatedAt",
                table: "inventory_movements",
                columns: new[] { "WorkspaceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_WorkspaceId_IngredientId_CreatedAt",
                table: "inventory_movements",
                columns: new[] { "WorkspaceId", "IngredientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_WorkspaceId_Type_CreatedAt",
                table: "inventory_movements",
                columns: new[] { "WorkspaceId", "Type", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_batches_suppliers_SupplierId",
                table: "stock_batches");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_stock_batches_SupplierId",
                table: "stock_batches");

            migrationBuilder.DropIndex(
                name: "IX_meal_reviews_WorkspaceId_IsReclamation_CreatedAt",
                table: "meal_reviews");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "stock_batches");

            migrationBuilder.DropColumn(
                name: "IsReclamation",
                table: "meal_reviews");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "meal_reviews");
        }
    }
}
