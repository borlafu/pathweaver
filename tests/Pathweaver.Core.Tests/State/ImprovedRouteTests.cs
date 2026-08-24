using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Levels;
using Pathweaver.Core.Rules;
using Pathweaver.Core.Save;
using Pathweaver.Core.Scoring;
using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.State;

/// <summary>
/// What happens when a pair that has already paid out is connected a better way.
/// </summary>
/// <remarks>
/// <para>
/// Reported from a device on <c>biome1-17</c>, the ring level: the player joined the spring to the hub
/// through the single cell between them, taking 100 points, then built the nine-conduit way round and
/// nothing happened. A pair paid once, ever, at whatever length it first completed — so the 800 target
/// had become unreachable and restarting was the only way out. The Pivot Token the level grants could
/// not rescue it either, because retrieving the short cut did not clear the record.
/// </para>
/// <para>
/// A pair now pays for the best route it has managed, and only the difference. Nothing is ever paid
/// twice, and a route that gets longer is worth what the length curve says it is worth.
/// </para>
/// </remarks>
public class ImprovedRouteTests
{
    /// <summary>
    /// A ring of twelve cells with the spring and hub two steps apart: one cell between them the short
    /// way, nine the long way. Sixty skips, so the helper below can always reach the tile it needs —
    /// this fixture is about what a completed route pays, not about managing a hand.
    /// </summary>
    private const string RingLevel = """
        id: ring
        base-score: 100
        target-score: 400
        tokens: 1
        skips: 60
        cell: 2,0
        cell: 1,1
        cell: 0,2
        cell: -1,2
        cell: -2,2
        cell: -2,1
        cell: -2,0
        cell: -1,-1
        cell: 0,-2
        cell: 1,-2
        cell: 2,-2
        cell: 2,-1
        spring: 2,0 water
        hub: 2,-2 water
        tile: 0,3 water x6
        tile: 0,2 water x5
        """;

    [Fact]
    public void A_longer_route_pays_the_difference_once_the_short_cut_is_gone()
    {
        // Arrange — the short cut first, which is the trap
        var level = LevelLoader.Parse(RingLevel);
        var state = level.CreateGame();

        state = Connect(state, new HexCoord(2, -1), new HexCoord(2, 0), new HexCoord(2, -2));
        Assert.Equal(ScoreTable.ScoreFor(100, 1), state.Score);

        // Act — build the long way round, then retrieve the short cut so the resources take it
        state = BuildLongWay(state);
        state = GameEngine.Apply(state, new PivotRetrieve(new HexCoord(2, -1)));

        // Assert — nine conduits, paid in full, with the 100 already taken deducted
        var expected = ScoreTable.ScoreFor(100, 9);
        Assert.Equal(expected, state.Score);
        Assert.True(level.IsClearedBy(state.Score));
    }

    [Fact]
    public void The_same_route_rebuilt_pays_nothing_more()
    {
        // The rule the change must not break: retrieving a conduit and putting it back is not a way
        // to be paid twice for the same work.
        // Arrange
        var level = LevelLoader.Parse(RingLevel);
        var state = level.CreateGame();

        state = Connect(state, new HexCoord(2, -1), new HexCoord(2, 0), new HexCoord(2, -2));
        var afterFirst = state.Score;

        // Act — take it off and put it back
        state = GameEngine.Apply(state, new PivotRetrieve(new HexCoord(2, -1)));
        state = Connect(state, new HexCoord(2, -1), new HexCoord(2, 0), new HexCoord(2, -2));

        // Assert
        Assert.Equal(afterFirst, state.Score);
    }

    [Fact]
    public void A_shorter_route_after_a_longer_one_pays_nothing_and_takes_nothing_back()
    {
        // Arrange — the long way first, then the short cut added on top of it
        var level = LevelLoader.Parse(RingLevel);
        var state = BuildLongWay(level.CreateGame());
        var afterLongWay = state.Score;
        Assert.Equal(ScoreTable.ScoreFor(100, 9), afterLongWay);

        // Act
        state = Connect(state, new HexCoord(2, -1), new HexCoord(2, 0), new HexCoord(2, -2));

        // Assert — the resources now take the short cut, but a payout already made is not undone
        Assert.Equal(afterLongWay, state.Score);
    }

    [Fact]
    public void Tokens_are_granted_once_per_pair_rather_than_once_per_improvement()
    {
        // Otherwise a player could extend a route one cell at a time and be paid a token for every
        // step, which is a farm rather than a reward.
        // Arrange
        var level = LevelLoader.Parse(RingLevel);
        var state = level.CreateGame();

        state = Connect(state, new HexCoord(2, -1), new HexCoord(2, 0), new HexCoord(2, -2));
        var skipsAfterFirst = state.SkipTokens.Count;
        var tokensAfterFirst = state.PivotTokens.Count;

        // Act — improve it to nine conduits, which would earn a Pivot Token if this were a first
        // completion
        state = BuildLongWay(state);
        state = GameEngine.Apply(state, new PivotRetrieve(new HexCoord(2, -1)));

        // Assert — the score improved, and the only change to the currencies is the token the
        // retrieve spent. A nine-conduit route treated as a first completion would have granted a
        // Pivot Token, which would show up here as no change at all.
        Assert.True(state.Score > ScoreTable.ScoreFor(100, 1));
        Assert.Equal(tokensAfterFirst - 1, state.PivotTokens.Count);

        // Skips only ever went down: the helper spends them to reach the tiles it needs, and nothing
        // in an improvement grants one.
        Assert.True(state.SkipTokens.Count <= skipsAfterFirst);
    }

    [Fact]
    public void A_saved_game_remembers_what_each_pair_was_paid()
    {
        // Without the length in the save, closing the app between the short cut and the long way
        // round would lose the record of what had been paid — and the difference would be paid twice.
        // Arrange
        var level = LevelLoader.Parse(RingLevel);
        var state = Connect(
            level.CreateGame(), new HexCoord(2, -1), new HexCoord(2, 0), new HexCoord(2, -2));

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(state));
        reloaded = BuildLongWay(reloaded);
        reloaded = GameEngine.Apply(reloaded, new PivotRetrieve(new HexCoord(2, -1)));

        // Assert
        Assert.Equal(ScoreTable.ScoreFor(100, 9), reloaded.Score);
    }

    /// <summary>
    /// Places a conduit joining two given neighbours, whatever the bag deals, by skipping until the
    /// tile in hand fits.
    /// </summary>
    private static GameState Connect(GameState state, HexCoord cell, HexCoord from, HexCoord to)
    {
        var wanted = EdgeMask.FromDirections(DirectionTo(cell, from), DirectionTo(cell, to));

        for (var attempt = 0; attempt < 40; attempt++)
        {
            for (var rotation = 0; rotation < 6; rotation++)
            {
                if (state.HeldTile.RotateClockwise(rotation).Edges == wanted)
                {
                    return GameEngine.Apply(state, new PlaceTile(cell, rotation));
                }
            }

            state = GameEngine.Apply(state, new SkipTile());
        }

        throw new InvalidOperationException($"No tile fitting {wanted} was dealt for {cell}.");
    }

    /// <summary>Builds the nine conduits of the long way round, in ring order.</summary>
    private static GameState BuildLongWay(GameState state)
    {
        var ring = new[]
        {
            new HexCoord(2, 0),
            new HexCoord(1, 1),
            new HexCoord(0, 2),
            new HexCoord(-1, 2),
            new HexCoord(-2, 2),
            new HexCoord(-2, 1),
            new HexCoord(-2, 0),
            new HexCoord(-1, -1),
            new HexCoord(0, -2),
            new HexCoord(1, -2),
            new HexCoord(2, -2),
        };

        for (var index = 1; index < ring.Length - 1; index++)
        {
            state = Connect(state, ring[index], ring[index - 1], ring[index + 1]);
        }

        return state;
    }

    private static int DirectionTo(HexCoord from, HexCoord neighbour)
    {
        var offset = neighbour - from;

        for (var direction = 0; direction < HexCoord.Directions.Count; direction++)
        {
            if (HexCoord.Directions[direction].Equals(offset))
            {
                return direction;
            }
        }

        throw new ArgumentException($"{neighbour} does not touch {from}.", nameof(neighbour));
    }
}
