using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ADDRez.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTagCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TagCategoryId",
                table: "tags",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tag_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tag_categories_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tags_TagCategoryId",
                table: "tags",
                column: "TagCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_tag_categories_CompanyId",
                table: "tag_categories",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_tags_tag_categories_TagCategoryId",
                table: "tags",
                column: "TagCategoryId",
                principalTable: "tag_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tags_tag_categories_TagCategoryId",
                table: "tags");

            migrationBuilder.DropTable(
                name: "tag_categories");

            migrationBuilder.DropIndex(
                name: "IX_tags_TagCategoryId",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "TagCategoryId",
                table: "tags");
        }
    }
}
