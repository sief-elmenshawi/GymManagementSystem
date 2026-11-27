using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthRecordAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthRecordAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HealthRecordId = table.Column<int>(type: "int", nullable: true),
                    MemberId = table.Column<int>(type: "int", nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Height = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthRecordAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthRecordAudits_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HealthRecordAudits_HealthRecords_HealthRecordId",
                        column: x => x.HealthRecordId,
                        principalTable: "HealthRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HealthRecordAudits_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecordAudits_ActorUserId",
                table: "HealthRecordAudits",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecordAudits_CreatedAt",
                table: "HealthRecordAudits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecordAudits_HealthRecordId",
                table: "HealthRecordAudits",
                column: "HealthRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthRecordAudits_MemberId",
                table: "HealthRecordAudits",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthRecordAudits");
        }
    }
}
