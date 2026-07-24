using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrierenHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceLogBreakMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BreakMinutes",
                table: "AttendanceLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakMinutes",
                table: "AttendanceLogs");
        }
    }
}
