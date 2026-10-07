using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

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
                name: "ingredients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BaseUnit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CostPerUnit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EmbeddingModel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmbeddingDimensions = table.Column<int>(type: "integer", nullable: false),
                    ChunkCount = table.Column<int>(type: "integer", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_documents", x => x.Id);
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
                    PhotoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    IsReclamation = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_reviews", x => x.Id);
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
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    StockConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ClientCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workspace_notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LinkPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Audience = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetClientCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlanType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.Id);
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
                name: "employee_meal_request_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: true),
                    DishName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventories_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "knowledge_chunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    TokenEstimate = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_chunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_chunks_knowledge_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "knowledge_documents",
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
                name: "order_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: true),
                    DishName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "stock_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_batches_ingredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "ingredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_batches_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workspace_notification_reads",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_notification_reads", x => new { x.NotificationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_workspace_notification_reads_workspace_notifications_Notifi~",
                        column: x => x.NotificationId,
                        principalTable: "workspace_notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OrderCadence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Daily"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_client_companies_workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "workspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_client_companies_WorkspaceId_Name",
                table: "client_companies",
                columns: new[] { "WorkspaceId", "Name" });

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
                name: "IX_ingredients_WorkspaceId_Name",
                table: "ingredients",
                columns: new[] { "WorkspaceId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_inventories_IngredientId",
                table: "inventories",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_inventories_WorkspaceId",
                table: "inventories",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_inventories_WorkspaceId_IngredientId",
                table: "inventories",
                columns: new[] { "WorkspaceId", "IngredientId" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_chunks_DocumentId",
                table: "knowledge_chunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_chunks_Embedding",
                table: "knowledge_chunks",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_chunks_WorkspaceId_DocumentId_ChunkIndex",
                table: "knowledge_chunks",
                columns: new[] { "WorkspaceId", "DocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_documents_WorkspaceId_CreatedAtUtc",
                table: "knowledge_documents",
                columns: new[] { "WorkspaceId", "CreatedAtUtc" });

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

            migrationBuilder.CreateIndex(
                name: "IX_meal_reviews_WorkspaceId_IsReclamation_CreatedAt",
                table: "meal_reviews",
                columns: new[] { "WorkspaceId", "IsReclamation", "CreatedAt" });

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

            migrationBuilder.CreateIndex(
                name: "IX_order_items_DishId",
                table: "order_items",
                column: "DishId");

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
                name: "IX_orders_DriverId",
                table: "orders",
                column: "DriverId");

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
                name: "IX_orders_WorkspaceId_DriverId_TargetDate_Status",
                table: "orders",
                columns: new[] { "WorkspaceId", "DriverId", "TargetDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_PlacedByUserId",
                table: "orders",
                columns: new[] { "WorkspaceId", "PlacedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_Status",
                table: "orders",
                columns: new[] { "WorkspaceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_WorkspaceId_StockConsumedAt",
                table: "orders",
                columns: new[] { "WorkspaceId", "StockConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_IngredientId",
                table: "stock_batches",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_SupplierId",
                table: "stock_batches",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_WorkspaceId",
                table: "stock_batches",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_WorkspaceId_IngredientId_ReceivedAt",
                table: "stock_batches",
                columns: new[] { "WorkspaceId", "IngredientId", "ReceivedAt" });

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

            migrationBuilder.CreateIndex(
                name: "IX_users_ClientCompanyId",
                table: "users",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_users_WorkspaceId_Username",
                table: "users",
                columns: new[] { "WorkspaceId", "Username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_notification_reads_UserId",
                table: "workspace_notification_reads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_workspace_notifications_WorkspaceId_Audience_TargetUserId",
                table: "workspace_notifications",
                columns: new[] { "WorkspaceId", "Audience", "TargetUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_notifications_WorkspaceId_CreatedAtUtc",
                table: "workspace_notifications",
                columns: new[] { "WorkspaceId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_notifications_WorkspaceId_Type_Audience_TargetCli~",
                table: "workspace_notifications",
                columns: new[] { "WorkspaceId", "Type", "Audience", "TargetClientCompanyId", "RelatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_workspace_notifications_WorkspaceId_Type_RelatedEntityId",
                table: "workspace_notifications",
                columns: new[] { "WorkspaceId", "Type", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_workspaces_Subdomain",
                table: "workspaces",
                column: "Subdomain",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "client_companies");

            migrationBuilder.DropTable(
                name: "dish_ingredients");

            migrationBuilder.DropTable(
                name: "employee_meal_request_items");

            migrationBuilder.DropTable(
                name: "inventories");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "knowledge_chunks");

            migrationBuilder.DropTable(
                name: "meal_reviews");

            migrationBuilder.DropTable(
                name: "menu_items");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "stock_batches");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "workspace_notification_reads");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropTable(
                name: "employee_meal_requests");

            migrationBuilder.DropTable(
                name: "knowledge_documents");

            migrationBuilder.DropTable(
                name: "dishes");

            migrationBuilder.DropTable(
                name: "menu_days");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "ingredients");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "workspace_notifications");

            migrationBuilder.DropTable(
                name: "menus");
        }
    }
}
