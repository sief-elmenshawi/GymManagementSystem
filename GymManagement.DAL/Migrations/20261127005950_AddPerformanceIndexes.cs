using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Memberships_MemberId",
                table: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_PlanId",
                table: "Memberships");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EndDate",
                table: "Sessions",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StartDate_EndDate",
                table: "Sessions",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_EndDate",
                table: "Memberships",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MemberId_EndDate",
                table: "Memberships",
                columns: new[] { "MemberId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_PlanId_EndDate",
                table: "Memberships",
                columns: new[] { "PlanId", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_EndDate",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_StartDate_EndDate",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_EndDate",
                table: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_MemberId_EndDate",
                table: "Memberships");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_PlanId_EndDate",
                table: "Memberships");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_MemberId",
                table: "Memberships",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_PlanId",
                table: "Memberships",
                column: "PlanId");
        }
    }
}
