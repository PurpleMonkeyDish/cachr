using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cachr.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_StoredObjectMetadataId",
                table: "StoredObjects");

            migrationBuilder.DropIndex(
                name: "IX_StoredObjects_StoredObjectMetadataId",
                table: "StoredObjects");

            migrationBuilder.DropColumn(
                name: "StoredObjectMetadataId",
                table: "StoredObjects");

            migrationBuilder.AddForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_MetadataId",
                table: "StoredObjects",
                column: "MetadataId",
                principalTable: "ObjectMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_MetadataId",
                table: "StoredObjects");

            migrationBuilder.AddColumn<Guid>(
                name: "StoredObjectMetadataId",
                table: "StoredObjects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredObjects_StoredObjectMetadataId",
                table: "StoredObjects",
                column: "StoredObjectMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_StoredObjectMetadataId",
                table: "StoredObjects",
                column: "StoredObjectMetadataId",
                principalTable: "ObjectMetadata",
                principalColumn: "Id");
        }
    }
}
