using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SavedNuveiPaymentToken",
                table: "Players",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentDetails",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentPackageId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessorTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SimplyConnectResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentDetails", x => x.PaymentId);
                });

            migrationBuilder.CreateTable(
                name: "PayoutDetails",
                columns: table => new
                {
                    PayoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithdrawalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserPaymentOptionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Processor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessorTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoutDetails", x => x.PayoutId);
                });

            migrationBuilder.CreateTable(
                name: "RefundDetails",
                columns: table => new
                {
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithdrawalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Processor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessorTransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundDetails", x => x.RefundId);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalDetails",
                columns: table => new
                {
                    WithdrawalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MutationToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalDetails", x => x.WithdrawalId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentDetails");

            migrationBuilder.DropTable(
                name: "PayoutDetails");

            migrationBuilder.DropTable(
                name: "RefundDetails");

            migrationBuilder.DropTable(
                name: "WithdrawalDetails");

            migrationBuilder.DropColumn(
                name: "SavedNuveiPaymentToken",
                table: "Players");
        }
    }
}
