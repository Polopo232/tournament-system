using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentSystem.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingHistoryTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TournamentId",
                table: "RatingHistory",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE rh SET rh.TournamentId = m.TournamentId
                FROM RatingHistory rh
                INNER JOIN Matches m ON m.Id = rh.MatchId
                WHERE rh.TournamentId IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TournamentId",
                table: "RatingHistory");
        }
    }
}
