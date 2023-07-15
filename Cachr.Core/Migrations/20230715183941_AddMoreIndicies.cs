using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cachr.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreIndicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StoredObjects_Shard_Key",
                table: "StoredObjects",
                columns: new[] { "Shard", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredObjects_Shard_MetadataId",
                table: "StoredObjects",
                columns: new[] { "Shard", "MetadataId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredObjects_Shard_Key",
                table: "StoredObjects");

            migrationBuilder.DropIndex(
                name: "IX_StoredObjects_Shard_MetadataId",
                table: "StoredObjects");
        }
    }
}
