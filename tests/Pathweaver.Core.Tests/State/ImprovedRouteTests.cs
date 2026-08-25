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
    /// A ring of six cells with the spring and hub two steps apart: one cell between them the short
    /// way, three the long way. One skip, so that earning one is still visible.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bag holds one shape, and that is why the ring is the smallest one there is. Every cell of a
    /// radius-one ring turns by the same angle, so whatever the bag deals fits wherever the helper
    /// below wants it and no skip is ever needed to build either route.
    /// </para>
    /// <para>
    /// The fixture this replaces was a twelve-cell ring, and it bought its way through a two-shape bag
    /// with sixty skips — a hand no player can hold now that a pool has a ceiling of three. Three was
    /// not enough to build that ring, because a cell can only be filled once its neighbour is, so a
    /// mismatched deal has nowhere else to go.
    /// </para>
    /// </remarks>
    private const string RingLevel = """
        id: ring
        base-score: 100
        target-score: 150
        tokens: 1
        skips: 1
        cell: 1,0
        cell: 0,1
        cell: -1,1
        cell: -1,0
        cell: 0,-1
        cell: 1,-1
        spring: 1,0 water
        hub: -1,1 water
        tile: 0,2 water x6
        """;

    /// <summary>The ring in order, from the spring the long way round to the hub.</summary>
    private static readonly HexCoord[] TheLongWay =
    {
        new HexCoord(1, 0),
        new HexCoord(1, -1),
        new HexCoord(0, -1),
        new HexCoord(-1, 0),
        new HexCoord(-1, 1),
    };

    /// <summary>The single cell between the spring and the hub the other way round.</summary>
    private static readonly HexCoord ShortCut = new HexCoord(0, 1);

    [Fact]
    public void A_longer_route_pays_the_difference_once_the_short_cut_is_gone()
    {
        // Arrange — the short cut first, which is the trap
        var level = LevelLoader.Parse(RingLevel);
        var state = TakeTheShortCut(level.CreateGame());

        Assert.Equal(ScoreTable.ScoreFor(100, 1), state.Score);

        // Act — build the long way round, then retrieve the short cut so the resources take it
        state = BuildLongWay(state);
        state = GameEngine.Apply(state, new PivotRetrieve(ShortCut));

        // Assert — three conduits, paid in full, with the 100 already taken deducted
        var expected = ScoreTable.ScoreFor(100, 3);
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
        var state = TakeTheShortCut(level.CreateGame());
        var afterFirst = state.Score;

        // Act — take it off and put it back
        state = GameEngine.Apply(state, new PivotRetrieve(ShortCut));
        state = TakeTheShortCut(state);

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
        Assert.Equal(ScoreTable.ScoreFor(100, 3), afterLongWay);

        // Act
        state = TakeTheShortCut(state);

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
        var state = TakeTheShortCut(level.CreateGame());

        var skipsAfterFirst = state.SkipTokens.Count;
        var tokensAfterFirst = state.PivotTokens.Count;

        // Act — improve it to three conduits, which would earn a skip if this were a first completion
        state = BuildLongWay(state);
        state = GameEngine.Apply(state, new PivotRetrieve(ShortCut));

        // Assert — the score improved, and the only change to the currencies is the token the retrieve
        // spent. The level deals one skip and the first completion earns a second, which leaves room
        // below the ceiling: a third would mean the improvement had been paid as a fresh route.
        Assert.True(state.Score > ScoreTable.ScoreFor(100, 1));
        Assert.Equal(tokensAfterFirst - 1, state.PivotTokens.Count);
        Assert.Equal(skipsAfterFirst, state.SkipTokens.Count);
        Assert.False(state.SkipTokens.IsFull, "the ceiling, not the rule, would be hiding a grant");
    }

    [Fact]
    public void A_saved_game_remembers_what_each_pair_was_paid()
    {
        // Without the length in the save, closing the app between the short cut and the long way
        // round would lose the record of what had been paid — and the difference would be paid twice.
        // Arrange
        var level = LevelLoader.Parse(RingLevel);
        var state = TakeTheShortCut(level.CreateGame());

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(state));
        reloaded = BuildLongWay(reloaded);
        reloaded = GameEngine.Apply(reloaded, new PivotRetrieve(ShortCut));

        // Assert
        Assert.Equal(ScoreTable.ScoreFor(100, 3), reloaded.Score);
    }

    /// <summary>Joins the spring to the hub through the single cell between them.</summary>
    private static GameState TakeTheShortCut(GameState state)
        => Build(state, ShapesFor(new[] { TheLongWay[0], ShortCut, TheLongWay[^1] }));

    /// <summary>Builds the conduits of the long way round.</summary>
    private static GameState BuildLongWay(GameState state) => Build(state, ShapesFor(TheLongWay));

    /// <summary>
    /// The shape each cell of a path needs: open towards the cell before it and the cell after it.
    /// </summary>
    /// <remarks>
    /// The ends of the path are the spring and the hub, which are already on the board, so only the
    /// cells between them appear here.
    /// </remarks>
    private static Dictionary<HexCoord, EdgeMask> ShapesFor(IReadOnlyList<HexCoord> path)
    {
        var shapes = new Dictionary<HexCoord, EdgeMask>();

        for (var index = 1; index < path.Count - 1; index++)
        {
            shapes[path[index]] = EdgeMask.FromDirections(
                DirectionTo(path[index], path[index - 1]), DirectionTo(path[index], path[index + 1]));
        }

        return shapes;
    }

    /// <summary>
    /// Fills the wanted cells, placing whatever the bag deals wherever that shape is still wanted.
    /// </summary>
    /// <remarks>
    /// Playing the deal rather than dictating an order is what keeps this within the three skips a
    /// level grants. Insisting on ring order meant every mismatch cost a skip; taking the tile in hand
    /// to any cell that wants it means a mismatch is only a mismatch when no remaining cell wants that
    /// shape at all, which on this board is rare. A run of them would exhaust the skips, and that
    /// throws rather than quietly asserting against a half-built ring.
    /// </remarks>
    private static GameState Build(GameState state, Dictionary<HexCoord, EdgeMask> wanted)
    {
        var remaining = new Dictionary<HexCoord, EdgeMask>(wanted);

        while (remaining.Count > 0)
        {
            var placed = false;

            foreach (var candidate in state.LegalPlacements)
            {
                if (!remaining.TryGetValue(candidate.Coordinate, out var shape)
                    || candidate.Tile.Edges != shape)
                {
                    continue;
                }

                state = GameEngine.Apply(state, new PlaceTile(candidate.Coordinate, candidate.Rotation));
                remaining.Remove(candidate.Coordinate);
                placed = true;
                break;
            }

            if (placed)
            {
                continue;
            }

            if (!state.SkipTokens.CanSpend)
            {
                throw new InvalidOperationException(
                    $"Ran out of skips with {remaining.Count} cells still to fill, holding {state.HeldTile}.");
            }

            state = GameEngine.Apply(state, new SkipTile());
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
