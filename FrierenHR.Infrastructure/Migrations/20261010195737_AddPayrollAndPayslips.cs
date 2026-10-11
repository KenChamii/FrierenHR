using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrierenHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollAndPayslips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyRate",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardMonthlyHours",
                table: "Employees",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PayrollDeductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollDeductions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payslips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MonthlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RegularHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayableOvertimeHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payslips_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payslips_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayslipLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayslipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Basis = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipLines_Payslips_PayslipId",
                        column: x => x.PayslipId,
                        principalTable: "Payslips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollDeductions_CompanyId_Name",
                table: "PayrollDeductions",
                columns: new[] { "CompanyId", "Name" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipLines_PayslipId",
                table: "PayslipLines",
                column: "PayslipId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_CompanyId",
                table: "Payslips",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId_PeriodStart_PeriodEnd",
                table: "Payslips",
                columns: new[] { "EmployeeId", "PeriodStart", "PeriodEnd" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollDeductions");

            migrationBuilder.DropTable(
                name: "PayslipLines");

            migrationBuilder.DropTable(
                name: "Payslips");

            migrationBuilder.DropColumn(
                name: "MonthlyRate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StandardMonthlyHours",
                table: "Employees");
        }
    }
}
