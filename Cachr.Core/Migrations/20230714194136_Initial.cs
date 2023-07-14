using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cachr.Core.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ObjectMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AbsoluteExpiration = table.Column<long>(type: "INTEGER", nullable: true),
                    SlidingExpiration = table.Column<double>(type: "REAL", nullable: true),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    Modified = table.Column<long>(type: "INTEGER", nullable: false),
                    LastAccess = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoredObjects",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Shard = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    Modified = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredObjects", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StoredObjects_ObjectMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ObjectMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoredObjects_MetadataId",
                table: "StoredObjects",
                column: "MetadataId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredObjects");

            migrationBuilder.DropTable(
                name: "ObjectMetadata");
        }
    }
}
