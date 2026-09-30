using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComputerRepairSystem.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseTypeToSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPlanId1",
                table: "Subscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnterpriseType",
                table: "SubscriptionPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPlanId1",
                table: "SubscriptionPlanModules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_SubscriptionPlanId1",
                table: "Subscriptions",
                column: "SubscriptionPlanId1");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanModules_SubscriptionPlanId1",
                table: "SubscriptionPlanModules",
                column: "SubscriptionPlanId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlanModules_SubscriptionPlans_SubscriptionPlanId1",
                table: "SubscriptionPlanModules",
                column: "SubscriptionPlanId1",
                principalTable: "SubscriptionPlans",
                principalColumn: "SubscriptionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId1",
                table: "Subscriptions",
                column: "SubscriptionPlanId1",
                principalTable: "SubscriptionPlans",
                principalColumn: "SubscriptionPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlanModules_SubscriptionPlans_SubscriptionPlanId1",
                table: "SubscriptionPlanModules");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_SubscriptionPlanId1",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_SubscriptionPlanId1",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlanModules_SubscriptionPlanId1",
                table: "SubscriptionPlanModules");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId1",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "EnterpriseType",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId1",
                table: "SubscriptionPlanModules");
        }
    }
}
