using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextShore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pathways_slug",
                table: "pathways");

            migrationBuilder.DropIndex(
                name: "ix_occupation_lists_pathway_id_code",
                table: "occupation_lists");

            migrationBuilder.AddColumn<Guid>(
                name: "pathway_id",
                table: "rule_audit_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "requirements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "requirements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "points_factors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "points_factors",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "pathways",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "pathways",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "occupation_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "occupation_lists",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "language_score_mappings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "replaces_id",
                table: "language_score_mappings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_rule_audit_log_pathway_id_at",
                table: "rule_audit_log",
                columns: new[] { "pathway_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_requirements_replaces_id",
                table: "requirements",
                column: "replaces_id");

            migrationBuilder.CreateIndex(
                name: "ix_points_factors_replaces_id",
                table: "points_factors",
                column: "replaces_id");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_replaces_id",
                table: "pathways",
                column: "replaces_id");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_slug",
                table: "pathways",
                column: "slug",
                unique: true,
                filter: "replaces_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_occupation_lists_pathway_id_code",
                table: "occupation_lists",
                columns: new[] { "pathway_id", "code" },
                unique: true,
                filter: "replaces_id IS NULL AND status <> 'archived'");

            migrationBuilder.CreateIndex(
                name: "ix_occupation_lists_replaces_id",
                table: "occupation_lists",
                column: "replaces_id");

            migrationBuilder.CreateIndex(
                name: "ix_language_score_mappings_replaces_id",
                table: "language_score_mappings",
                column: "replaces_id");

            migrationBuilder.AddForeignKey(
                name: "fk_language_score_mappings_language_score_mappings_replaces_id",
                table: "language_score_mappings",
                column: "replaces_id",
                principalTable: "language_score_mappings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_occupation_lists_occupation_lists_replaces_id",
                table: "occupation_lists",
                column: "replaces_id",
                principalTable: "occupation_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pathways_pathways_replaces_id",
                table: "pathways",
                column: "replaces_id",
                principalTable: "pathways",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_points_factors_points_factors_replaces_id",
                table: "points_factors",
                column: "replaces_id",
                principalTable: "points_factors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_requirements_requirements_replaces_id",
                table: "requirements",
                column: "replaces_id",
                principalTable: "requirements",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_language_score_mappings_language_score_mappings_replaces_id",
                table: "language_score_mappings");

            migrationBuilder.DropForeignKey(
                name: "fk_occupation_lists_occupation_lists_replaces_id",
                table: "occupation_lists");

            migrationBuilder.DropForeignKey(
                name: "fk_pathways_pathways_replaces_id",
                table: "pathways");

            migrationBuilder.DropForeignKey(
                name: "fk_points_factors_points_factors_replaces_id",
                table: "points_factors");

            migrationBuilder.DropForeignKey(
                name: "fk_requirements_requirements_replaces_id",
                table: "requirements");

            migrationBuilder.DropIndex(
                name: "ix_rule_audit_log_pathway_id_at",
                table: "rule_audit_log");

            migrationBuilder.DropIndex(
                name: "ix_requirements_replaces_id",
                table: "requirements");

            migrationBuilder.DropIndex(
                name: "ix_points_factors_replaces_id",
                table: "points_factors");

            migrationBuilder.DropIndex(
                name: "ix_pathways_replaces_id",
                table: "pathways");

            migrationBuilder.DropIndex(
                name: "ix_pathways_slug",
                table: "pathways");

            migrationBuilder.DropIndex(
                name: "ix_occupation_lists_pathway_id_code",
                table: "occupation_lists");

            migrationBuilder.DropIndex(
                name: "ix_occupation_lists_replaces_id",
                table: "occupation_lists");

            migrationBuilder.DropIndex(
                name: "ix_language_score_mappings_replaces_id",
                table: "language_score_mappings");

            migrationBuilder.DropColumn(
                name: "pathway_id",
                table: "rule_audit_log");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "points_factors");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "points_factors");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "pathways");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "pathways");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "occupation_lists");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "occupation_lists");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "language_score_mappings");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "language_score_mappings");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_slug",
                table: "pathways",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_occupation_lists_pathway_id_code",
                table: "occupation_lists",
                columns: new[] { "pathway_id", "code" },
                unique: true);
        }
    }
}
