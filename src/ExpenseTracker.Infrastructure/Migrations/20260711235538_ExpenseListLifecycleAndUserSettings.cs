using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpenseListLifecycleAndUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceExpenseListId",
                table: "PersonalTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceExpenseListId",
                table: "PersonalCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "ExpenseLists",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByUserId",
                table: "ExpenseLists",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SyncClosedListsToPersonal = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTransactions_SourceExpenseListId",
                table: "PersonalTransactions",
                column: "SourceExpenseListId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCategories_UserId_SourceExpenseListId",
                table: "PersonalCategories",
                columns: new[] { "UserId", "SourceExpenseListId" });

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

            migrationBuilder.DropIndex(
                name: "IX_PersonalTransactions_SourceExpenseListId",
                table: "PersonalTransactions");

            migrationBuilder.DropIndex(
                name: "IX_PersonalCategories_UserId_SourceExpenseListId",
                table: "PersonalCategories");

            migrationBuilder.DropColumn(
                name: "SourceExpenseListId",
                table: "PersonalTransactions");

            migrationBuilder.DropColumn(
                name: "SourceExpenseListId",
                table: "PersonalCategories");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "ExpenseLists");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "ExpenseLists");
        }
    }
}
