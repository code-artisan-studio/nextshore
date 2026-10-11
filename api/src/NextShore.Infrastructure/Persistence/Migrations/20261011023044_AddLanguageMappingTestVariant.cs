using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextShore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguageMappingTestVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_language_score_mappings_test_skill_target_scale",
                table: "language_score_mappings");

            migrationBuilder.AddColumn<string>(
                name: "test_variant",
                table: "language_score_mappings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_language_score_mappings_test_test_variant_skill_target_scale",
                table: "language_score_mappings",
                columns: new[] { "test", "test_variant", "skill", "target_scale" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_language_score_mappings_test_test_variant_skill_target_scale",
                table: "language_score_mappings");

            migrationBuilder.DropColumn(
                name: "test_variant",
                table: "language_score_mappings");

            migrationBuilder.CreateIndex(
                name: "ix_language_score_mappings_test_skill_target_scale",
                table: "language_score_mappings",
                columns: new[] { "test", "skill", "target_scale" });
        }
    }
}
