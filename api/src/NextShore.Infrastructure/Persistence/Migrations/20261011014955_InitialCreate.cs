using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextShore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profile = table.Column<string>(type: "jsonb", nullable: false),
                    results = table.Column<string>(type: "jsonb", nullable: false),
                    rule_version = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: true),
                    summary_generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    occupation_system = table.Column<string>(type: "text", nullable: false),
                    language_scale = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "language_score_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    test = table.Column<string>(type: "text", nullable: false),
                    skill = table.Column<string>(type: "text", nullable: false),
                    min_score = table.Column<decimal>(type: "numeric", nullable: true),
                    target_scale = table.Column<string>(type: "text", nullable: false),
                    target_level = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    verified_at = table.Column<DateOnly>(type: "date", nullable: true),
                    unverified_fields = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_language_score_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rule_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    actor = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pathways",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    audience = table.Column<string[]>(type: "text[]", nullable: false),
                    is_points_based = table.Column<bool>(type: "boolean", nullable: false),
                    pass_mark = table.Column<int>(type: "integer", nullable: true),
                    cutoff = table.Column<int>(type: "integer", nullable: true),
                    cutoff_as_of = table.Column<DateOnly>(type: "date", nullable: true),
                    next_pathway_id = table.Column<Guid>(type: "uuid", nullable: true),
                    typical_duration_months = table.Column<int>(type: "integer", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: false),
                    rule_version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    verified_at = table.Column<DateOnly>(type: "date", nullable: true),
                    unverified_fields = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pathways", x => x.id);
                    table.ForeignKey(
                        name: "fk_pathways_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pathways_pathways_next_pathway_id",
                        column: x => x.next_pathway_id,
                        principalTable: "pathways",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "occupation_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pathway_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    is_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    verified_at = table.Column<DateOnly>(type: "date", nullable: true),
                    unverified_fields = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_occupation_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_occupation_lists_pathways_pathway_id",
                        column: x => x.pathway_id,
                        principalTable: "pathways",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "points_factors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pathway_id = table.Column<Guid>(type: "uuid", nullable: false),
                    factor = table.Column<string>(type: "text", nullable: false),
                    variant = table.Column<string>(type: "text", nullable: false),
                    min = table.Column<decimal>(type: "numeric", nullable: true),
                    max = table.Column<decimal>(type: "numeric", nullable: true),
                    match_value = table.Column<string>(type: "text", nullable: true),
                    points = table.Column<int>(type: "integer", nullable: true),
                    max_points_for_factor = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    verified_at = table.Column<DateOnly>(type: "date", nullable: true),
                    unverified_fields = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_points_factors", x => x.id);
                    table.ForeignKey(
                        name: "fk_points_factors_pathways_pathway_id",
                        column: x => x.pathway_id,
                        principalTable: "pathways",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pathway_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field = table.Column<string>(type: "text", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "text", nullable: false),
                    value = table.Column<string>(type: "jsonb", nullable: true),
                    unit = table.Column<string>(type: "text", nullable: true),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    fixability = table.Column<string>(type: "text", nullable: false),
                    fix_hint = table.Column<string>(type: "text", nullable: false),
                    fix_months = table.Column<int>(type: "integer", nullable: true),
                    label = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    verified_at = table.Column<DateOnly>(type: "date", nullable: true),
                    unverified_fields = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requirements", x => x.id);
                    table.ForeignKey(
                        name: "fk_requirements_pathways_pathway_id",
                        column: x => x.pathway_id,
                        principalTable: "pathways",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assessments_expires_at",
                table: "assessments",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_countries_code",
                table: "countries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_countries_status",
                table: "countries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_language_score_mappings_status",
                table: "language_score_mappings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_language_score_mappings_test_skill_target_scale",
                table: "language_score_mappings",
                columns: new[] { "test", "skill", "target_scale" });

            migrationBuilder.CreateIndex(
                name: "ix_occupation_lists_pathway_id_code",
                table: "occupation_lists",
                columns: new[] { "pathway_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_occupation_lists_status",
                table: "occupation_lists",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_country_id",
                table: "pathways",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_next_pathway_id",
                table: "pathways",
                column: "next_pathway_id");

            migrationBuilder.CreateIndex(
                name: "ix_pathways_slug",
                table: "pathways",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pathways_status",
                table: "pathways",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_points_factors_pathway_id",
                table: "points_factors",
                column: "pathway_id");

            migrationBuilder.CreateIndex(
                name: "ix_points_factors_status",
                table: "points_factors",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_requirements_pathway_id",
                table: "requirements",
                column: "pathway_id");

            migrationBuilder.CreateIndex(
                name: "ix_requirements_status",
                table: "requirements",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_rule_audit_log_at",
                table: "rule_audit_log",
                column: "at");

            migrationBuilder.CreateIndex(
                name: "ix_rule_audit_log_entity_entity_id",
                table: "rule_audit_log",
                columns: new[] { "entity", "entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "language_score_mappings");

            migrationBuilder.DropTable(
                name: "occupation_lists");

            migrationBuilder.DropTable(
                name: "points_factors");

            migrationBuilder.DropTable(
                name: "requirements");

            migrationBuilder.DropTable(
                name: "rule_audit_log");

            migrationBuilder.DropTable(
                name: "pathways");

            migrationBuilder.DropTable(
                name: "countries");
        }
    }
}
