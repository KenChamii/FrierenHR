using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrierenHR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyRuleConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyRuleConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    RuleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRuleConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyRuleConfigs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRuleConfigs_CompanyId_RuleType_IsActive",
                table: "CompanyRuleConfigs",
                columns: new[] { "CompanyId", "RuleType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyRuleConfigs");
        }
    }
}
