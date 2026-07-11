using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DomainSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_AspNetUsers_FromUserId",
                table: "Settlements");

            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_AspNetUsers_ToUserId",
                table: "Settlements");

            migrationBuilder.DropTable(
                name: "TransactionParticipants");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Settlements_FromUserId",
                table: "Settlements");

            migrationBuilder.DropIndex(
                name: "IX_Settlements_ToUserId",
                table: "Settlements");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseListMembers_ExpenseListId_UserId",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "FromUserId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ToUserId",
                table: "Settlements");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Settlements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Settlements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FromMemberId",
                table: "Settlements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Settlements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ToMemberId",
                table: "Settlements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ExpenseListMembers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ExpenseListMembers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ExpenseListMembers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ExpenseListMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ExpenseListMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ExpenseListMembers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "ExpenseListMembers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ExpenseListMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "ExpenseListMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ExpenseListMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenseListCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseListCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseListCategories_ExpenseLists_ExpenseListId",
                        column: x => x.ExpenseListId,
                        principalTable: "ExpenseLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalCategories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseListTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PaidByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseListTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseListTransactions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseListTransactions_ExpenseListCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ExpenseListCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseListTransactions_ExpenseListMembers_PaidByMemberId",
                        column: x => x.PaidByMemberId,
                        principalTable: "ExpenseListMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseListTransactions_ExpenseLists_ExpenseListId",
                        column: x => x.ExpenseListId,
                        principalTable: "ExpenseLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonalTransactions_PersonalCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "PersonalCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseListTransactionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomShareAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseListTransactionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseListTransactionParticipants_ExpenseListMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "ExpenseListMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseListTransactionParticipants_ExpenseListTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "ExpenseListTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_FromMemberId",
                table: "Settlements",
                column: "FromMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_ToMemberId",
                table: "Settlements",
                column: "ToMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListMembers_ExpenseListId_UserId",
                table: "ExpenseListMembers",
                columns: new[] { "ExpenseListId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListCategories_ExpenseListId",
                table: "ExpenseListCategories",
                column: "ExpenseListId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListCategories_ExpenseListId_IsDefault",
                table: "ExpenseListCategories",
                columns: new[] { "ExpenseListId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactionParticipants_MemberId",
                table: "ExpenseListTransactionParticipants",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactionParticipants_TransactionId",
                table: "ExpenseListTransactionParticipants",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactionParticipants_TransactionId_MemberId",
                table: "ExpenseListTransactionParticipants",
                columns: new[] { "TransactionId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactions_CategoryId",
                table: "ExpenseListTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactions_CreatedByUserId",
                table: "ExpenseListTransactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactions_Date",
                table: "ExpenseListTransactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactions_ExpenseListId",
                table: "ExpenseListTransactions",
                column: "ExpenseListId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactions_ExpenseListId_Date",
                table: "ExpenseListTransactions",
                columns: new[] { "ExpenseListId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListTransactions_PaidByMemberId",
                table: "ExpenseListTransactions",
                column: "PaidByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCategories_UserId",
                table: "PersonalCategories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalCategories_UserId_IsDefault",
                table: "PersonalCategories",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTransactions_CategoryId",
                table: "PersonalTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTransactions_Date",
                table: "PersonalTransactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTransactions_UserId",
                table: "PersonalTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTransactions_UserId_Date",
                table: "PersonalTransactions",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_ExpenseListMembers_FromMemberId",
                table: "Settlements",
                column: "FromMemberId",
                principalTable: "ExpenseListMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_ExpenseListMembers_ToMemberId",
                table: "Settlements",
                column: "ToMemberId",
                principalTable: "ExpenseListMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_ExpenseListMembers_FromMemberId",
                table: "Settlements");

            migrationBuilder.DropForeignKey(
                name: "FK_Settlements_ExpenseListMembers_ToMemberId",
                table: "Settlements");

            migrationBuilder.DropTable(
                name: "ExpenseListTransactionParticipants");

            migrationBuilder.DropTable(
                name: "PersonalTransactions");

            migrationBuilder.DropTable(
                name: "ExpenseListTransactions");

            migrationBuilder.DropTable(
                name: "PersonalCategories");

            migrationBuilder.DropTable(
                name: "ExpenseListCategories");

            migrationBuilder.DropIndex(
                name: "IX_Settlements_FromMemberId",
                table: "Settlements");

            migrationBuilder.DropIndex(
                name: "IX_Settlements_ToMemberId",
                table: "Settlements");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseListMembers_ExpenseListId_UserId",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "FromMemberId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ToMemberId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "ExpenseListMembers");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ExpenseListMembers");

            migrationBuilder.AddColumn<string>(
                name: "FromUserId",
                table: "Settlements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ToUserId",
                table: "Settlements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ExpenseListMembers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseListId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.CheckConstraint("CK_Category_Ownership", "(\"UserId\" IS NOT NULL AND \"ExpenseListId\" IS NULL) OR (\"UserId\" IS NULL AND \"ExpenseListId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Categories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Categories_ExpenseLists_ExpenseListId",
                        column: x => x.ExpenseListId,
                        principalTable: "ExpenseLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseListId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_AspNetUsers_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_ExpenseLists_ExpenseListId",
                        column: x => x.ExpenseListId,
                        principalTable: "ExpenseLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomShareAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionParticipants_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_FromUserId",
                table: "Settlements",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_ToUserId",
                table: "Settlements",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseListMembers_ExpenseListId_UserId",
                table: "ExpenseListMembers",
                columns: new[] { "ExpenseListId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ExpenseListId",
                table: "Categories",
                column: "ExpenseListId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ExpenseListId_IsDefault",
                table: "Categories",
                columns: new[] { "ExpenseListId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId",
                table: "Categories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_UserId_IsDefault",
                table: "Categories",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionParticipants_TransactionId",
                table: "TransactionParticipants",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionParticipants_TransactionId_UserId",
                table: "TransactionParticipants",
                columns: new[] { "TransactionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionParticipants_UserId",
                table: "TransactionParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedByUserId",
                table: "Transactions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date",
                table: "Transactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExpenseListId",
                table: "Transactions",
                column: "ExpenseListId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExpenseListId_Date",
                table: "Transactions",
                columns: new[] { "ExpenseListId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaidByUserId",
                table: "Transactions",
                column: "PaidByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_AspNetUsers_FromUserId",
                table: "Settlements",
                column: "FromUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Settlements_AspNetUsers_ToUserId",
                table: "Settlements",
                column: "ToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
