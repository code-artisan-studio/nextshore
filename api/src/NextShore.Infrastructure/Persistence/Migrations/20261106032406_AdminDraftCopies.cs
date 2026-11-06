using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextShore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminDraftCopies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pathways_slug",
                table: "pathways");

            migrationBuilder.AddColumn<Guid>(
                name: "pathway_id",
                table: "rule_audit_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "requirements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "points_factors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "pathways",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "occupation_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_rule_audit_log_pathway_id_at",
                table: "rule_audit_log",
                columns: new[] { "pathway_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_pathways_replaces_id",
                table: "pathways",
                column: "replaces_id",
                unique: true,
                filter: "replaces_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_slug",
                table: "pathways",
                column: "slug",
                unique: true,
                filter: "replaces_id IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rule_audit_log_pathway_id_at",
                table: "rule_audit_log");

            migrationBuilder.DropIndex(
                name: "ix_pathways_replaces_id",
                table: "pathways");

            migrationBuilder.DropIndex(
                name: "ix_pathways_slug",
                table: "pathways");

            migrationBuilder.DropColumn(
                name: "pathway_id",
                table: "rule_audit_log");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "points_factors");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "pathways");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "occupation_lists");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_slug",
                table: "pathways",
                column: "slug",
                unique: true);
        }
    }
}
