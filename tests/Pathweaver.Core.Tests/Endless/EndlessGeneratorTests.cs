using Pathweaver.Core.Endless;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Scoring;
using Pathweaver.Core.Tiles;

namespace Pathweaver.Core.Tests.Endless;

public class EndlessGeneratorTests
{
    [Fact]
    public void The_same_round_and_seed_generate_the_same_board()
    {
        // Endless Wayfare has no authored data, so reproducibility is the only way a player can
        // be sent the round they were playing when something went wrong.
        // Act
        var first = EndlessGenerator.Generate(round: 5, seed: 1234UL);
        var second = EndlessGenerator.Generate(round: 5, seed: 1234UL);

        // Assert
        Assert.Equal(first.Level.Shape, second.Level.Shape);
        Assert.Equal(first.Level.Endpoints, second.Level.Endpoints);
        Assert.Equal(first.Level.BagTiles, second.Level.BagTiles);
        Assert.Equal(first.Level.TargetScore, second.Level.TargetScore);
        Assert.Equal(first.Level.Seed, second.Level.Seed);
    }

    [Fact]
    public void A_different_seed_generates_a_different_board()
    {
        // Arrange / Act
        var first = EndlessGenerator.Generate(round: 5, seed: 1UL);
        var second = EndlessGenerator.Generate(round: 5, seed: 2UL);

        // Assert — the board outline is the same hexagon for a given round by design, since its
        // size is a function of what the round asks for. What the seed decides is where the
        // networks run, so that is what has to differ.
        Assert.NotEqual(first.Level.Endpoints, second.Level.Endpoints);
        Assert.NotEqual(
            first.Solution.Select(placement => placement.Coordinate),
            second.Solution.Select(placement => placement.Coordinate));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(20)]
    public void A_generated_round_is_a_valid_level(int round)
    {
        // Act
        var generated = EndlessGenerator.Generate(round, seed: 99UL);
        var level = generated.Level;

        // Assert
        Assert.Equal($"endless-{round}", level.Id);
        Assert.NotEmpty(level.Shape);
        Assert.NotEmpty(level.BagTiles);
        Assert.True(level.TargetScore > 0);

        foreach (var endpoint in level.Endpoints)
        {
            Assert.Contains(endpoint.Coordinate, level.Shape);
        }

        // Endpoints occupy cells, so two on one cell is a board that cannot be played.
        Assert.Equal(
            level.Endpoints.Count,
            level.Endpoints.Select(endpoint => endpoint.Coordinate).Distinct().Count());

        // Every spring needs a hub of its own kind, or its route can never be completed.
        foreach (var spring in level.Endpoints.Where(endpoint => endpoint.Role == EndpointRole.Spring))
        {
            Assert.Contains(
                level.Endpoints,
                endpoint => endpoint.Role == EndpointRole.Hub && endpoint.Kind == spring.Kind);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(15)]
    [InlineData(30)]
    public void A_generated_round_reaches_its_target_when_its_own_solution_is_built(int round)
    {
        // The generator plans the routes first and derives the board from them, so the plan is a
        // witness: no search is needed to know the round can be finished. Checking the witness
        // actually scores what the target asks for is what makes that claim worth anything.
        // Arrange
        var generated = EndlessGenerator.Generate(round, seed: 7UL);
        var board = HexGrid<ConduitTile>.FromShape(generated.Level.Shape);

        // Act — build the planned conduits directly, ignoring the order they would be drawn in
        foreach (var placement in generated.Solution)
        {
            board = board.Place(placement.Coordinate, placement.Tile);
        }

        var routes = FlowResolver.FindCompletedRoutes(board, generated.Level.Endpoints);
        var score = routes.Sum(route => ScoreTable.ScoreFor(generated.Level.BaseRouteScore, route.Length));

        // Assert
        Assert.True(
            score >= generated.Level.TargetScore,
            $"round {round}: the planned solution scores {score}, short of {generated.Level.TargetScore}.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(9)]
    public void A_generated_round_carries_the_tiles_its_solution_needs(int round)
    {
        // A bag missing a shape the plan needs would make the round unsolvable however well it
        // is played, and rotation is free, so shapes are compared up to rotation.
        // Arrange
        var generated = EndlessGenerator.Generate(round, seed: 3UL);

        // Assert
        foreach (var placement in generated.Solution)
        {
            Assert.Contains(
                generated.Level.BagTiles,
                tile => tile.Kind == placement.Tile.Kind && IsRotationOf(tile.Edges, placement.Tile.Edges));
        }
    }

    [Fact]
    public void Later_rounds_ask_for_more_than_the_first()
    {
        // Escalation is the whole progression in Endless Wayfare, since there is nothing else to
        // unlock. Asserted between distant rounds rather than between neighbours: the step from
        // one round to the next is deliberately not always upward, so that a run has some rhythm.
        // Act
        var early = EndlessGenerator.Generate(round: 1, seed: 42UL);
        var late = EndlessGenerator.Generate(round: 25, seed: 42UL);

        // Assert
        Assert.True(late.Level.TargetScore > early.Level.TargetScore);
        Assert.True(late.Level.Shape.Count > early.Level.Shape.Count);
        Assert.True(late.Level.Endpoints.Count > early.Level.Endpoints.Count);
    }

    [Fact]
    public void A_generated_round_starts_with_a_tile_that_can_be_placed()
    {
        // A round that opens deadlocked is a round the player has already lost.
        for (var round = 1; round <= 25; round++)
        {
            var state = EndlessGenerator.Generate(round, seed: 11UL).Level.CreateGame();
            Assert.False(state.IsDeadlocked, $"round {round} opens with a tile that fits nowhere.");
        }
    }

    [Fact]
    public void A_round_below_one_is_rejected()
    {
        // Rounds are counted from one, and a zero would silently generate the easiest board
        // forever rather than reporting the caller's mistake.
        Assert.Throws<ArgumentOutOfRangeException>(() => EndlessGenerator.Generate(0, seed: 1UL));
    }

    private static bool IsRotationOf(EdgeMask candidate, EdgeMask target)
    {
        for (var steps = 0; steps < 6; steps++)
        {
            if (candidate.RotateClockwise(steps) == target)
            {
                return true;
            }
        }

        return false;
    }
}
