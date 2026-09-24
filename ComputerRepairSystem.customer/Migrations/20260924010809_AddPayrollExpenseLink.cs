using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComputerRepairSystem.company.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollExpenseLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PayrollId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PayrollId",
                table: "Expenses",
                column: "PayrollId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Payrolls_PayrollId",
                table: "Expenses",
                column: "PayrollId",
                principalTable: "Payrolls",
                principalColumn: "PayrollId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Payrolls_PayrollId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_PayrollId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PayrollId",
                table: "Expenses");
        }
    }
}
