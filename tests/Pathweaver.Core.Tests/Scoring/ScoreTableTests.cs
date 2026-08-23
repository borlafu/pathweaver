using Pathweaver.Core.Scoring;

namespace Pathweaver.Core.Tests.Scoring;

public class ScoreTableTests
{
    [Fact]
    public void A_route_of_one_carries_no_multiplier()
    {
        // PRD section 3.2A: S = S_base * 1.35^(L-1), so L of 1 is the base score.
        Assert.Equal(ScoreTable.Scale, ScoreTable.MultiplierFor(1));
    }

    [Theory]
    [InlineData(1, 1_000_000L)]
    [InlineData(2, 1_350_000L)]
    [InlineData(3, 1_822_500L)]
    [InlineData(4, 2_460_375L)]
    [InlineData(5, 3_321_506L)]
    [InlineData(10, 14_893_745L)]
    [InlineData(20, 299_461_918L)]
    [InlineData(40, 121_064_544_208L)]
    [InlineData(64, 162_565_137_305_043L)]
    public void Multipliers_match_the_formula_to_the_unit(int length, long expected)
    {
        // Values computed independently in exact rational arithmetic:
        // round(1.35^(L-1) * 1_000_000). Exactness is the point — a naive
        // repeated multiply by 135/100 drifts about 5.5 million units by L of 64.
        Assert.Equal(expected, ScoreTable.MultiplierFor(length));
    }

    [Fact]
    public void Multipliers_increase_strictly_with_length()
    {
        // The whole risk-reward tension in PRD section 3.2A depends on this.
        for (var length = 2; length <= ScoreTable.MaxRouteLength; length++)
        {
            Assert.True(
                ScoreTable.MultiplierFor(length) > ScoreTable.MultiplierFor(length - 1),
                $"Multiplier did not grow from length {length - 1} to {length}.");
        }
    }

    [Theory]
    [InlineData(1, 100L)]
    [InlineData(2, 135L)]
    [InlineData(3, 182L)]
    [InlineData(4, 246L)]
    [InlineData(5, 332L)]
    public void A_base_of_one_hundred_scores_as_the_formula_predicts(int length, long expected)
    {
        Assert.Equal(expected, ScoreTable.ScoreFor(100, length));
    }

    [Fact]
    public void A_base_of_zero_scores_nothing_at_any_length()
    {
        Assert.Equal(0, ScoreTable.ScoreFor(0, 1));
        Assert.Equal(0, ScoreTable.ScoreFor(0, ScoreTable.MaxRouteLength));
    }

    [Fact]
    public void Scores_round_to_nearest_with_halves_going_up()
    {
        // 10 * 1.35 is exactly 13.5. Rounding must be defined rather than
        // incidental, because a player comparing two routes will notice.
        Assert.Equal(14, ScoreTable.ScoreFor(10, 2));
    }

    [Fact]
    public void Scores_round_down_below_a_half()
    {
        // 10 * 1.8225 is 18.225
        Assert.Equal(18, ScoreTable.ScoreFor(10, 3));
    }

    [Fact]
    public void A_long_route_beats_repeated_short_ones_of_the_same_total_length()
    {
        // The design intent of the geometric multiplier: one route of 6 must pay
        // more than three routes of 2, or extending would never be worth the
        // congestion risk described in PRD section 3.2A.
        var oneLongRoute = ScoreTable.ScoreFor(100, 6);
        var threeShortRoutes = 3 * ScoreTable.ScoreFor(100, 2);

        Assert.True(
            oneLongRoute > threeShortRoutes,
            $"One route of 6 scored {oneLongRoute}, three of 2 scored {threeShortRoutes}.");
    }

    [Fact]
    public void Scores_increase_strictly_with_length_for_a_realistic_base()
    {
        // Arrange
        var previous = 0L;

        // Act / Assert
        for (var length = 1; length <= ScoreTable.MaxRouteLength; length++)
        {
            var score = ScoreTable.ScoreFor(100, length);
            Assert.True(score > previous, $"Score did not grow at length {length}.");
            previous = score;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_length_below_one_is_rejected(int length)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScoreTable.MultiplierFor(length));
        Assert.Throws<ArgumentOutOfRangeException>(() => ScoreTable.ScoreFor(100, length));
    }

    [Fact]
    public void A_length_beyond_the_table_is_rejected()
    {
        // Rejecting rather than clamping: a clamp would silently underpay a route
        // and look like a scoring bug to whoever built it.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ScoreTable.MultiplierFor(ScoreTable.MaxRouteLength + 1));
    }

    [Fact]
    public void A_negative_base_score_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ScoreTable.ScoreFor(-1, 3));
    }

    [Fact]
    public void A_base_score_beyond_the_supported_maximum_is_rejected()
    {
        // The cap exists so the largest multiplier cannot overflow a long. Failing
        // loudly beats wrapping into a negative score.
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ScoreTable.ScoreFor(ScoreTable.MaxBaseScore + 1, 1));
    }

    [Fact]
    public void The_largest_supported_inputs_do_not_overflow()
    {
        // Act
        var score = ScoreTable.ScoreFor(ScoreTable.MaxBaseScore, ScoreTable.MaxRouteLength);

        // Assert
        Assert.True(score > 0, $"Expected a positive score, got {score}.");
    }

    [Fact]
    public void Repeated_calls_return_identical_values()
    {
        // Guards against any future caching or lazy initialisation introducing
        // order dependence into something the Daily Expedition must reproduce.
        for (var length = 1; length <= 20; length++)
        {
            Assert.Equal(ScoreTable.MultiplierFor(length), ScoreTable.MultiplierFor(length));
            Assert.Equal(ScoreTable.ScoreFor(75, length), ScoreTable.ScoreFor(75, length));
        }
    }

    [Fact]
    public void The_table_covers_more_than_any_plausible_route()
    {
        // A route cannot exceed the cell count of a level, and MVP levels are far
        // smaller than this.
        Assert.True(ScoreTable.MaxRouteLength >= 64);
    }
}
