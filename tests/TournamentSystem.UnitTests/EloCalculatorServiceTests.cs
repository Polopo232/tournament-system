using TournamentSystem.Application.Services;
using TournamentSystem.Infrastructure.Services;
using Xunit;

namespace TournamentSystem.UnitTests;

public class EloCalculatorServiceTests
{
    private readonly EloCalculatorService _eloService = new();

    [Fact]
    public void Calculate_FavoriteWins_IncreasesFavoriteRatingSlightly()
    {
        // Arrange: favoriit (1600) vs autsaider (1200)
        int favoriteRating = 1600;
        int underdogRating = 1200;

        // Act
        var (newFavorite, newUnderdog) = _eloService.CalculateNewRatings(favoriteRating, underdogRating, 1.0);

        // Assert: tõus on väike (positiivne, aga alla poole K-faktorist)
        int gain = newFavorite - favoriteRating;
        Assert.True(gain > 0 && gain <= 5, $"Oodati väikest tõusu, tegelik oli {gain}");
        Assert.Equal(favoriteRating + underdogRating, newFavorite + newUnderdog);
    }

    [Fact]
    public void Calculate_UnderdogWins_IncreasesUnderdogRatingSignificantly()
    {
        // Arrange: autsaider (1200) alistab favoriidi (1600)
        int favoriteRating = 1600;
        int underdogRating = 1200;

        // Act (scoreA = 0.0 -> võitis teine mängija ehk autsaider)
        var (newFavorite, newUnderdog) = _eloService.CalculateNewRatings(favoriteRating, underdogRating, 0.0);

        // Assert: autsaideri reitingu hüpe on suur (> 25 punkti)
        int underdogGain = newUnderdog - underdogRating;
        Assert.True(underdogGain >= 25, $"Oodati suurt hüpet, tegelik oli {underdogGain}");
        Assert.True(newFavorite < favoriteRating);
    }

    [Fact]
    public void Calculate_DrawBetweenEqualPlayers_KeepsRatingsUnchanged()
    {
        // Arrange: võrdsed mängijad (1400 vs 1400) mängivad viiki (0.5)
        int ratingA = 1400;
        int ratingB = 1400;

        // Act
        var (newA, newB) = _eloService.CalculateNewRatings(ratingA, ratingB, 0.5);

        // Assert
        Assert.Equal(1400, newA);
        Assert.Equal(1400, newB);
    }

    [Fact]
    public void Calculate_ExtremeRatingDifference_HandlesBoundsWithoutOverflow()
    {
        // Arrange: äärmuslik reitinguvahe (2800 vs 400)
        int grandmaster = 2800;
        int novice = 400;

        // Act
        var (newGm, newNovice) = _eloService.CalculateNewRatings(grandmaster, novice, 1.0);

        // Assert
        Assert.True(newGm >= grandmaster);
        Assert.True(newNovice <= novice);
        Assert.True(newNovice > 0);
    }
}