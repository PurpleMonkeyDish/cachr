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
                    SlidingExpiration = table.Column<long>(type: "INTEGER", nullable: true),
                    CurrentExpiration = table.Column<long>(type: "INTEGER", nullable: true)
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
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Created = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUpdate = table.Column<long>(type: "INTEGER", nullable: false),
                    ObjectMetadataId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredObjects", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StoredObjects_ObjectMetadata_ObjectMetadataId",
                        column: x => x.ObjectMetadataId,
                        principalTable: "ObjectMetadata",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoredObjects_ObjectMetadataId",
                table: "StoredObjects",
                column: "ObjectMetadataId");
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
