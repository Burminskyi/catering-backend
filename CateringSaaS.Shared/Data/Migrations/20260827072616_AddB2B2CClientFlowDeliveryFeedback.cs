using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddB2B2CClientFlowDeliveryFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DriverId",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "employee_meal_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_meal_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "meal_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "employee_meal_request_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_meal_request_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employee_meal_request_items_employee_meal_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "employee_meal_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_DriverId",
                table: "orders",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_DriverId_TargetDate_Status",
                table: "orders",
                columns: new[] { "WorkspaceId", "DriverId", "TargetDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_meal_request_items_MenuItemId",
                table: "employee_meal_request_items",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_meal_request_items_RequestId",
                table: "employee_meal_request_items",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_meal_request_items_WorkspaceId",
                table: "employee_meal_request_items",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_meal_requests_EmployeeId",
                table: "employee_meal_requests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_meal_requests_WorkspaceId",
                table: "employee_meal_requests",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_meal_requests_WorkspaceId_ClientCompanyId_TargetDa~",
                table: "employee_meal_requests",
                columns: new[] { "WorkspaceId", "ClientCompanyId", "TargetDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_meal_requests_WorkspaceId_EmployeeId_TargetDate",
                table: "employee_meal_requests",
                columns: new[] { "WorkspaceId", "EmployeeId", "TargetDate" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_reviews_MenuItemId",
                table: "meal_reviews",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_reviews_WorkspaceId",
                table: "meal_reviews",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_meal_reviews_WorkspaceId_ClientCompanyId_TargetDate",
                table: "meal_reviews",
                columns: new[] { "WorkspaceId", "ClientCompanyId", "TargetDate" });

            migrationBuilder.CreateIndex(
                name: "IX_meal_reviews_WorkspaceId_EmployeeId_TargetDate_MenuItemId",
                table: "meal_reviews",
                columns: new[] { "WorkspaceId", "EmployeeId", "TargetDate", "MenuItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_meal_request_items");

            migrationBuilder.DropTable(
                name: "meal_reviews");

            migrationBuilder.DropTable(
                name: "employee_meal_requests");

            migrationBuilder.DropIndex(
                name: "IX_orders_DriverId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_orders_WorkspaceId_DriverId_TargetDate_Status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "orders");
        }
    }
}
