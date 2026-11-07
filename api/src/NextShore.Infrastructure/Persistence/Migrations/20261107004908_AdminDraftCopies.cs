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
            // From AdminDrafts (earlier M5 design): refuse to drop its columns while they still mean something.
            // Its drafts (replaces_id / delete_on_publish) and archived rows have no place in the draft-copy design;
            // publish or discard them first. Empty on a fresh database. The migration runs in one transaction.
            migrationBuilder.Sql("""
                DO $$
                DECLARE leftover text;
                BEGIN
                    SELECT string_agg(t, ', ') INTO leftover FROM (
                        SELECT 'pathways' AS t WHERE EXISTS (SELECT 1 FROM pathways WHERE replaces_id IS NOT NULL OR delete_on_publish OR status = 'archived')
                        UNION ALL SELECT 'requirements' WHERE EXISTS (SELECT 1 FROM requirements WHERE replaces_id IS NOT NULL OR delete_on_publish OR status = 'archived')
                        UNION ALL SELECT 'points_factors' WHERE EXISTS (SELECT 1 FROM points_factors WHERE replaces_id IS NOT NULL OR delete_on_publish OR status = 'archived')
                        UNION ALL SELECT 'occupation_lists' WHERE EXISTS (SELECT 1 FROM occupation_lists WHERE replaces_id IS NOT NULL OR delete_on_publish OR status = 'archived')
                        UNION ALL SELECT 'language_score_mappings' WHERE EXISTS (SELECT 1 FROM language_score_mappings WHERE replaces_id IS NOT NULL OR delete_on_publish OR status = 'archived')
                    ) found;
                    IF leftover IS NOT NULL THEN
                        RAISE EXCEPTION 'AdminDraftCopies: rows from the earlier admin draft design remain in: % (replaces_id set, delete_on_publish true or status archived). Nothing was changed. See docs/DEPLOY.md.', leftover;
                    END IF;
                END $$;
                """);

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
                name: "ix_requirements_replaces_id",
                table: "requirements");

            migrationBuilder.DropIndex(
                name: "ix_points_factors_replaces_id",
                table: "points_factors");

            migrationBuilder.DropIndex(
                name: "ix_pathways_replaces_id",
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
                name: "delete_on_publish",
                table: "requirements");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "points_factors");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "pathways");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "occupation_lists");

            migrationBuilder.DropColumn(
                name: "delete_on_publish",
                table: "language_score_mappings");

            migrationBuilder.DropColumn(
                name: "replaces_id",
                table: "language_score_mappings");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_replaces_id",
                table: "pathways",
                column: "replaces_id",
                unique: true,
                filter: "replaces_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_occupation_lists_pathway_id_code",
                table: "occupation_lists",
                columns: new[] { "pathway_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pathways_replaces_id",
                table: "pathways");

            migrationBuilder.DropIndex(
                name: "ix_occupation_lists_pathway_id_code",
                table: "occupation_lists");

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "requirements",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "points_factors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "pathways",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "delete_on_publish",
                table: "occupation_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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
    }
}
