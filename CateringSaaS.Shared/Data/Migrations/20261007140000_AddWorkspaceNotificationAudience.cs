using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CateringSaaS.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceNotificationAudience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Audience",
                table: "workspace_notifications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "operations");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetUserId",
                table: "workspace_notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workspace_notifications_WorkspaceId_Audience_TargetUserId",
                table: "workspace_notifications",
                columns: new[] { "WorkspaceId", "Audience", "TargetUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workspace_notifications_WorkspaceId_Audience_TargetUserId",
                table: "workspace_notifications");

            migrationBuilder.DropColumn(
                name: "Audience",
                table: "workspace_notifications");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "workspace_notifications");
        }
    }
}
