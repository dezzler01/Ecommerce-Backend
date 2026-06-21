using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PicksAndMore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCollectionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectionType",
                table: "Products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectionType",
                table: "Products");
        }
    }
}
