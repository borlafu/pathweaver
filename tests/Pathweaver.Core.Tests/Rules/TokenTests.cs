using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Rules;

public class PivotTokenTests
{
    private const int East = 0;
    private const int West = 3;

    private static readonly EdgeMask EastWest = EdgeMask.FromDirections(East, West);

    [Fact]
    public void A_new_pool_holds_the_starting_count()
    {
        // Act
        var pool = TokenPool.Of(2);

        // Assert
        Assert.Equal(2, pool.Count);
    }

    [Fact]
    public void An_empty_pool_holds_nothing()
    {
        Assert.Equal(0, TokenPool.Empty.Count);
    }

    [Fact]
    public void A_negative_starting_count_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenPool.Of(-1));
    }

    [Fact]
    public void Earning_returns_a_new_pool_and_leaves_the_original_alone()
    {
        // Arrange
        var original = TokenPool.Of(1);

        // Act
        var earned = original.Earn(2);

        // Assert
        Assert.Equal(1, original.Count);
        Assert.Equal(3, earned.Count);
    }

    [Fact]
    public void Earning_nothing_is_allowed()
    {
        // Most completed routes earn no token, so this is the common path.
        Assert.Equal(1, TokenPool.Of(1).Earn(0).Count);
    }

    [Fact]
    public void Earning_a_negative_amount_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenPool.Of(1).Earn(-1));
    }

    [Fact]
    public void Spending_returns_a_new_pool_and_leaves_the_original_alone()
    {
        // Arrange
        var original = TokenPool.Of(2);

        // Act
        var spent = original.Spend();

        // Assert
        Assert.Equal(2, original.Count);
        Assert.Equal(1, spent.Count);
    }

    [Fact]
    public void Spending_from_an_empty_pool_is_rejected()
    {
        // A silent no-op would let the UI offer a rotation the player cannot pay
        // for, and the board would change without the cost being taken.
        Assert.Throws<InvalidOperationException>(() => TokenPool.Empty.Spend());
    }

    [Fact]
    public void A_pool_reports_whether_it_can_pay()
    {
        Assert.False(TokenPool.Empty.CanSpend);
        Assert.True(TokenPool.Of(1).CanSpend);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 0)]
    [InlineData(9, 0)]
    public void A_short_route_earns_a_skip_instead_of_a_pivot(int length, int expected)
    {
        // PRD section 3.2A frames the central choice as a short certain route against a
        // long risky one. Rewarding only length would make the safe play pay nothing,
        // which turns a dilemma into an obvious answer.
        Assert.Equal(expected, TokenRules.SkipTokensFor(length));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(12)]
    public void Every_route_pays_out_in_exactly_one_currency(int length)
    {
        // No completed route should feel wasted, and none should pay twice.
        Assert.Equal(1, TokenRules.PivotTokensFor(length) + TokenRules.SkipTokensFor(length));
    }

    [Fact]
    public void A_skip_length_below_one_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenRules.SkipTokensFor(0));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, 1)]
    [InlineData(9, 1)]
    [InlineData(64, 1)]
    public void A_route_earns_a_token_only_once_it_reaches_the_threshold(int length, int expected)
    {
        // PRD section 3.2B says tokens come from high-efficiency plays without
        // fixing a number. Four is the chosen threshold: it rewards exactly the
        // extended routing the 1.35^(L-1) curve pushes toward, so the risk and its
        // insurance reinforce each other.
        Assert.Equal(expected, TokenRules.PivotTokensFor(length));
    }

    [Fact]
    public void The_threshold_is_published_for_the_interface_to_explain()
    {
        // The player has to be able to learn this rule, so it cannot stay buried.
        Assert.Equal(4, TokenRules.PivotThreshold);
    }

    [Fact]
    public void A_route_length_below_one_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TokenRules.PivotTokensFor(0));
    }

    [Fact]
    public void Tokens_earned_across_real_routes_are_summed()
    {
        // Arrange — a radius-3 board, spring at (-3,0), hub at (2,0), four
        // conduits between them, so one route of length 4
        var board = HexGrid<ConduitTile>.Hexagon(3);
        for (var q = -2; q <= 1; q++)
        {
            board = board.Place(new HexCoord(q, 0), new ConduitTile(ResourceKind.Water, EastWest));
        }

        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(-3, 0), ResourceKind.Water),
            FlowEndpoint.Hub(new HexCoord(2, 0), ResourceKind.Water),
        };

        var routes = FlowResolver.FindCompletedRoutes(board, endpoints);

        // Act
        var earned = TokenRules.PivotTokensEarned(routes);

        // Assert
        Assert.Equal(4, Assert.Single(routes).Length);
        Assert.Equal(1, earned);
    }

    [Fact]
    public void A_short_real_route_earns_nothing()
    {
        // Arrange — spring and hub two apart, so a single conduit between them
        var board = HexGrid<ConduitTile>.Hexagon(2)
            .Place(HexCoord.Zero, new ConduitTile(ResourceKind.Water, EastWest));

        var endpoints = new[]
        {
            FlowEndpoint.Spring(new HexCoord(-1, 0), ResourceKind.Water),
            FlowEndpoint.Hub(new HexCoord(1, 0), ResourceKind.Water),
        };

        var routes = FlowResolver.FindCompletedRoutes(board, endpoints);

        // Act / Assert
        Assert.Equal(1, Assert.Single(routes).Length);
        Assert.Equal(0, TokenRules.PivotTokensEarned(routes));
    }

    [Fact]
    public void No_routes_earn_no_tokens()
    {
        Assert.Equal(0, TokenRules.PivotTokensEarned(Array.Empty<Route>()));
    }

    [Fact]
    public void A_null_route_collection_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => TokenRules.PivotTokensEarned(null!));
    }
}
