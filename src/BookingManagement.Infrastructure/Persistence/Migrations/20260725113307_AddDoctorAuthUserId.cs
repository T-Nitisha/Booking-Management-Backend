using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorAuthUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthUserId",
                table: "doctors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctors_AuthUserId",
                table: "doctors",
                column: "AuthUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_doctors_AuthUserId",
                table: "doctors");

            migrationBuilder.DropColumn(
                name: "AuthUserId",
                table: "doctors");
        }
    }
}
