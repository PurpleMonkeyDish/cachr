using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cachr.Core.Migrations
{
    /// <inheritdoc />
    public partial class IndexShardId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StoredObjects_Shard",
                table: "StoredObjects",
                column: "Shard");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredObjects_Shard",
                table: "StoredObjects");
        }
    }
}
