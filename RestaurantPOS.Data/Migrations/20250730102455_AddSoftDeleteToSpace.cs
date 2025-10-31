using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spaces_SpaceName",
                table: "Spaces");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Spaces",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Spaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Spaces_SpaceName_IsDeleted",
                table: "Spaces",
                columns: new[] { "SpaceName", "IsDeleted" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spaces_SpaceName_IsDeleted",
                table: "Spaces");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Spaces");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Spaces");

            migrationBuilder.CreateIndex(
                name: "IX_Spaces_SpaceName",
                table: "Spaces",
                column: "SpaceName",
                unique: true);
        }
    }
}
