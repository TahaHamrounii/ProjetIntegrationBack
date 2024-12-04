using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Message.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Avatar = table.Column<string>(type: "TEXT", nullable: false),
                    LastActiveTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "LastActiveTime", "Name", "Password", "UpdatedAt", "Username" },
                values: new object[] { "raed123", "default-avatar.jpg", new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), "raed@test.com", new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), "Raed Test", "$2a$11$LXHFQQMxrbCunbXWCDbei.Kxdkl/YfSwwnNDj/NclB7EbTxhTuiNe", new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), "raed" });

            migrationBuilder.InsertData(
                table: "UserSettings",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Language", "Theme", "UpdatedAt", "UserId" },
                values: new object[] { 1, new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), true, "en", "dark", new DateTime(2024, 12, 4, 7, 19, 13, 275, DateTimeKind.Utc).AddTicks(5884), "raed123" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
