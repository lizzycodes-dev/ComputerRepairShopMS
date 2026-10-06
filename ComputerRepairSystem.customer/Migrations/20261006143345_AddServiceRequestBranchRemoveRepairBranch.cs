using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComputerRepairSystem.company.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRequestBranchRemoveRepairBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Repairs_Branches_BranchId",
                table: "Repairs");

            migrationBuilder.DropIndex(
                name: "IX_Repairs_BranchId",
                table: "Repairs");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Repairs");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ServiceRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_BranchId",
                table: "ServiceRequests",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_Branches_BranchId",
                table: "ServiceRequests",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_Branches_BranchId",
                table: "ServiceRequests");

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_BranchId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ServiceRequests");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Repairs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repairs_BranchId",
                table: "Repairs",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Repairs_Branches_BranchId",
                table: "Repairs",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
