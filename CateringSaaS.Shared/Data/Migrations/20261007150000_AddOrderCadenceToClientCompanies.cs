using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCadenceToClientCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrderCadence",
                table: "client_companies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Daily");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderCadence",
                table: "client_companies");
        }
    }
}
