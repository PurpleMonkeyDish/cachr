using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cachr.Core.Migrations
{
    /// <inheritdoc />
    public partial class ModifiedColumnUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_ObjectMetadataId",
                table: "StoredObjects");

            migrationBuilder.RenameColumn(
                name: "LastUpdate",
                table: "StoredObjects",
                newName: "Modified");

            migrationBuilder.RenameColumn(
                name: "ObjectMetadataId",
                table: "StoredObjects",
                newName: "StoredObjectMetadataId");

            migrationBuilder.RenameIndex(
                name: "IX_StoredObjects_ObjectMetadataId",
                table: "StoredObjects",
                newName: "IX_StoredObjects_StoredObjectMetadataId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Created",
                table: "ObjectMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Modified",
                table: "ObjectMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_StoredObjectMetadataId",
                table: "StoredObjects",
                column: "StoredObjectMetadataId",
                principalTable: "ObjectMetadata",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_StoredObjectMetadataId",
                table: "StoredObjects");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "ObjectMetadata");

            migrationBuilder.DropColumn(
                name: "Modified",
                table: "ObjectMetadata");

            migrationBuilder.RenameColumn(
                name: "Modified",
                table: "StoredObjects",
                newName: "LastUpdate");

            migrationBuilder.RenameColumn(
                name: "StoredObjectMetadataId",
                table: "StoredObjects",
                newName: "ObjectMetadataId");

            migrationBuilder.RenameIndex(
                name: "IX_StoredObjects_StoredObjectMetadataId",
                table: "StoredObjects",
                newName: "IX_StoredObjects_ObjectMetadataId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoredObjects_ObjectMetadata_ObjectMetadataId",
                table: "StoredObjects",
                column: "ObjectMetadataId",
                principalTable: "ObjectMetadata",
                principalColumn: "Id");
        }
    }
}
