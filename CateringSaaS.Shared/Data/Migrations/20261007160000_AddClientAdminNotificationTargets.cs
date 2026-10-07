using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientAdminNotificationTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetClientCompanyId",
                table: "workspace_notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RelatedDate",
                table: "workspace_notifications",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_notifications_WorkspaceId_Type_Audience_TargetClientCompanyId_RelatedDate",
                table: "workspace_notifications",
                columns: new[] { "WorkspaceId", "Type", "Audience", "TargetClientCompanyId", "RelatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspace_notifications_WorkspaceId_Type_Audience_TargetClientCompanyId_RelatedDate",
                table: "workspace_notifications");

            migrationBuilder.DropColumn(
                name: "TargetClientCompanyId",
                table: "workspace_notifications");

            migrationBuilder.DropColumn(
                name: "RelatedDate",
                table: "workspace_notifications");
        }
    }
}
