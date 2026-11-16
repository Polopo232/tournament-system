using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationSeedNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeedNumber",
                table: "Registrations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeedNumber",
                table: "Registrations");
        }
    }
}
