using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FirstBrick.Portfolio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioView",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalInvested = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProjectTitle = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioView", x => new { x.UserId, x.ProjectId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioView_UserId",
                table: "PortfolioView",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioView");
        }
    }
}
