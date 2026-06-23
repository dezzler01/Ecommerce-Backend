using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PicksAndMore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTypeToSizeOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryType",
                table: "SizeOptions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryType",
                table: "SizeOptions");
        }
    }
}
