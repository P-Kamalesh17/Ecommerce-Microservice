using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProductAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductsAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CategoryName" },
                values: new object[,]
                {
                    { 1, "Electronics" },
                    { 2, "Clothing" },
                    { 3, "Books" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "ImageUrl", "Name", "Price", "StockQuantity", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "2.4GHz wireless optical mouse", "https://example.com/images/wireless-mouse.jpg", "Wireless Mouse", 19.99m, 150, null },
                    { 2, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Full-size keyboard with blue switches", "https://example.com/images/mechanical-keyboard.jpg", "Mechanical Keyboard", 79.50m, 60, null },
                    { 3, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "100% cotton crew neck t-shirt", "https://example.com/images/cotton-tshirt.jpg", "Cotton T-Shirt", 14.00m, 200, null },
                    { 4, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "A handbook of agile software craftsmanship", "https://example.com/images/clean-code.jpg", "Clean Code", 32.00m, 40, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
