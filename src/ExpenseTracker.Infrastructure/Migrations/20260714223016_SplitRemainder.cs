using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitRemainder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SplitRemainder",
                table: "ExpenseListTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SplitRemainder",
                table: "ExpenseListTransactions");
        }
    }
}
