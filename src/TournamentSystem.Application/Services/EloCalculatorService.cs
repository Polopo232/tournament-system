namespace TournamentSystem.Application.Services;

public class EloCalculatorService
{
    private const int DefaultKFactor = 32;

    /// <summary>
    /// Arvutab uued Elo-reitingud kahele mängijale vastavalt mängu tulemusele.
    /// </summary>
    /// <param name="player1Rating">Esimese mängija praegune reiting.</param>
    /// <param name="player2Rating">Teise mängija praegune reiting.</param>
    /// <param name="player1ActualScore">1.0 (1. võit), 0.5 (viik), 0.0 (2. võit).</param>
    /// <param name="kFactor">K-faktor (vaikimisi 32).</param>
    public (int NewPlayer1Rating, int NewPlayer2Rating) CalculateNewRatings(
        int player1Rating,
        int player2Rating,
        double player1ActualScore,
        int kFactor = DefaultKFactor)
    {
        double expected1 = 1.0 / (1.0 + Math.Pow(10.0, (player2Rating - player1Rating) / 400.0));
        double expected2 = 1.0 - expected1;
        double player2ActualScore = 1.0 - player1ActualScore;

        int newRating1 = (int)Math.Round(player1Rating + kFactor * (player1ActualScore - expected1));
        int newRating2 = (int)Math.Round(player2Rating + kFactor * (player2ActualScore - expected2));

        return (newRating1, newRating2);
    }
}