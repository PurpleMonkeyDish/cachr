using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cachr.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StoredObjects_MetadataId",
                table: "StoredObjects",
                column: "MetadataId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredObjects_MetadataId",
                table: "StoredObjects");
        }
    }
}
