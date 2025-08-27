using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewOrderDetailProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
      name: "IsNewItem",
      table: "OrderDetails",
      type: "bit",
      nullable: false,
      defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "OrderDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime?>(
                name: "ConfirmedAt",
                table: "OrderDetails",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
