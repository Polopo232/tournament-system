using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLichessImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Tournaments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalRank",
                table: "Registrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Registrations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_ExternalId",
                table: "Tournaments",
                column: "ExternalId",
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tournaments_ExternalId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "FinalRank",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Registrations");
        }
    }
}
