using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Message.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UserSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 4, 7, 28, 42, 6, DateTimeKind.Utc).AddTicks(7966), new DateTime(2024, 12, 4, 7, 28, 42, 6, DateTimeKind.Utc).AddTicks(7966) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "raed123",
                columns: new[] { "CreatedAt", "LastActiveTime", "Password", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 4, 7, 28, 42, 6, DateTimeKind.Utc).AddTicks(7966), new DateTime(2024, 12, 4, 7, 28, 42, 6, DateTimeKind.Utc).AddTicks(7966), "$2a$11$R8z.UOHlTXkYo2wugIiPpO8q5xo/H6/drB8FKDC4VmQeuKcsGy8Me", new DateTime(2024, 12, 4, 7, 28, 42, 6, DateTimeKind.Utc).AddTicks(7966) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UserSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "raed123",
                columns: new[] { "CreatedAt", "LastActiveTime", "Password", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), "$2a$11$LXHFQQMxrbCunbXWCDbei.Kxdkl/YfSwwnNDj/NclB7EbTxhTuiNe", new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884) });
        }
    }
}
