using Pathweaver.Core.Determinism;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Rules;
using Pathweaver.Core.State;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.State;

/// <summary>
/// A radius-3 board with a water spring at (-3,0) and a water hub at (2,0). The
/// four cells between them take east-west conduits, so filling the row completes
/// a route of length 4 — exactly the Pivot Token threshold.
/// </summary>
internal static class GameFixture
{
    internal const int East = 0;
    internal const int SouthEast = 1;
    internal const int West = 3;
    internal const int NorthWest = 4;

    internal const long BaseRouteScore = 100;

    internal static readonly EdgeMask EastWest = EdgeMask.FromDirections(East, West);

    internal static readonly HexCoord SpringCell = new HexCoord(-3, 0);
    internal static readonly HexCoord HubCell = new HexCoord(2, 0);

    /// <summary>The four cells the player fills, ordered from the spring.</summary>
    internal static readonly HexCoord[] RowCells =
    {
        new HexCoord(-2, 0),
        new HexCoord(-1, 0),
        new HexCoord(0, 0),
        new HexCoord(1, 0),
    };

    internal static ConduitTile Straight(ResourceKind kind = ResourceKind.Water)
        => new ConduitTile(kind, EastWest);

    internal static FlowEndpoint[] Endpoints => new[]
    {
        FlowEndpoint.Spring(SpringCell, ResourceKind.Water),
        FlowEndpoint.Hub(HubCell, ResourceKind.Water),
    };

    /// <summary>
    /// A bag holding nothing but east-west straights, so the held tile is
    /// predictable and tests can focus on state transitions.
    /// </summary>
    internal static TileBag StraightBag(ulong seed = 42UL)
        => TileBag.Create(
            new[] { Straight(), Straight() },
            SeedSource.Stream(seed, PathweaverStream.TileBag));

    internal static GameState NewGame(ulong seed = 42UL, int startingTokens = 0, int startingSkips = 0)
        => GameState.Create(
            HexGrid<ConduitTile>.Hexagon(3),
            Endpoints,
            StraightBag(seed),
            BaseRouteScore,
            TokenPool.Of(startingTokens),
            TokenPool.Of(startingSkips));

    /// <summary>
    /// Plays the row from the spring towards the hub, stopping after
    /// <paramref name="count"/> placements.
    /// </summary>
    internal static GameState PlayRow(GameState state, int count)
    {
        for (var index = 0; index < count; index++)
        {
            state = GameEngine.Apply(state, new PlaceTile(RowCells[index], 0));
        }

        return state;
    }
}
