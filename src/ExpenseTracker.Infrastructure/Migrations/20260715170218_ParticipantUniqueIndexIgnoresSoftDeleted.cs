using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ParticipantUniqueIndexIgnoresSoftDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseListTransactionParticipants_TransactionId_MemberId",
                table: "ExpenseListTransactionParticipants");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactionParticipants_TransactionId_MemberId",
                table: "ExpenseListTransactionParticipants",
                columns: new[] { "TransactionId", "MemberId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExpenseListTransactionParticipants_TransactionId_MemberId",
                table: "ExpenseListTransactionParticipants");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactionParticipants_TransactionId_MemberId",
                table: "ExpenseListTransactionParticipants",
                columns: new[] { "TransactionId", "MemberId" },
                unique: true);
        }
    }
}
