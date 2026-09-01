using Pathweaver.Core.Levels;

namespace Pathweaver.Core.Tests.Levels;

/// <summary>
/// The second way a level may prove it can be cleared.
/// </summary>
/// <remarks>
/// The first way — searching — is stronger, because nothing had to know the answer. This exists because
/// the search cannot finish on a board large enough to need panning, so these tests are mostly about
/// the replay failing loudly when a solution and its level have drifted apart. A quiet failure there
/// would certify a level nobody can clear.
/// </remarks>
public class AuthoredSolutionTests
{
    /// <summary>
    /// A three-cell corridor: a spring, one conduit, a hub. Solvable in one move.
    /// </summary>
    private const string Corridor = """
        id: test-corridor
        name: Corridor
        base-score: 100
        target-score: 100
        skips: 1
        cell: 0,0
        cell: 1,0
        cell: 2,0
        spring: 0,0 water
        hub: 2,0 water
        tile: 0,3 water x2
        """;

    [Fact]
    public void A_level_without_a_solution_has_an_empty_one()
    {
        // Arrange and act
        var level = LevelLoader.Parse(Corridor);

        // Assert
        Assert.Empty(level.Solution);
    }

    [Fact]
    public void A_solution_is_read_in_the_order_it_is_written()
    {
        // Arrange and act
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0\nsolve: skip");

        // Assert
        Assert.Equal(2, level.Solution.Count);
        Assert.False(level.Solution[0].IsSkip);
        Assert.Equal(1, level.Solution[0].At.Q);
        Assert.True(level.Solution[1].IsSkip);
    }

    [Fact]
    public void A_rotation_is_optional()
    {
        // The point of it being optional: on a corridor there is exactly one rotation that fits, so
        // writing it down is transcription rather than information.
        // Arrange and act
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0");

        // Assert
        Assert.Null(level.Solution[0].Rotation);
    }

    [Fact]
    public void A_rotation_can_be_insisted_on()
    {
        // Which matters at a junction, where more than one rotation joins something legally and only
        // one continues the route.
        // Arrange and act
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0 rot 3");

        // Assert
        Assert.Equal(3, level.Solution[0].Rotation);
    }

    [Fact]
    public void Replaying_a_solution_reaches_the_target()
    {
        // Arrange
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0");

        // Act
        var state = AuthoredSolution.Replay(level);

        // Assert
        Assert.True(state.Score >= level.TargetScore);
    }

    [Fact]
    public void The_replay_finds_the_rotation_when_none_is_given()
    {
        // A straight laid across a corridor needs turning; the replay tries each rotation rather than
        // failing on the one the tile happened to be dealt at.
        // Arrange
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0");

        // Act and assert — it does not throw
        AuthoredSolution.Replay(level);
    }

    [Fact]
    public void An_illegal_placement_names_the_step_that_failed()
    {
        // The whole reason the replay is worth trusting. A solution that has drifted out of step with
        // its level must say where, or it is no easier to fix than an unsolvable board.
        // Arrange — 0,0 holds the spring, so nothing can be placed on it
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0\nsolve: 0,0");

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => AuthoredSolution.Replay(level));

        // Assert
        Assert.Contains("test-corridor step 2", error.Message);
    }

    [Fact]
    public void A_rotation_that_does_not_fit_is_refused_rather_than_corrected()
    {
        // An insisted rotation is an instruction, not a hint. Silently turning it would hide the fact
        // that the author's reading of the board was wrong.
        // Arrange — a straight at 0,3 turned once has edges 1 and 4, which face no neighbour of 1,0
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0 rot 1");

        // Act and assert
        var error = Assert.Throws<InvalidOperationException>(() => AuthoredSolution.Replay(level));
        Assert.Contains("step 1", error.Message);
    }

    [Fact]
    public void A_placement_with_no_legal_rotation_says_so()
    {
        // Distinguished from a rejected command, because the responses differ: this one means the cell
        // is wrong, not the turn.
        // Arrange — 1,0 must be filled before 2,0's hub is reachable, and the hub cell takes no tile
        var level = LevelLoader.Parse(Corridor + "\nsolve: 2,0");

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => AuthoredSolution.Replay(level));

        // Assert
        Assert.Contains("no rotation", error.Message);
    }

    [Fact]
    public void Replaying_a_level_with_no_solution_is_a_mistake_rather_than_a_pass()
    {
        // Otherwise the gate could report a level certified because it silently replayed nothing.
        // Arrange
        var level = LevelLoader.Parse(Corridor);

        // Act and assert
        Assert.Throws<InvalidOperationException>(() => AuthoredSolution.Replay(level));
    }

    [Theory]
    [InlineData("solve: 1,0 rot")]
    [InlineData("solve: 1,0 turn 2")]
    [InlineData("solve: 1,0 rot 6")]
    [InlineData("solve: nowhere")]
    public void A_malformed_step_is_refused_at_load(string line)
    {
        // Arrange, act and assert
        Assert.Throws<LevelFormatException>(() => LevelLoader.Parse(Corridor + "\n" + line));
    }

    [Fact]
    public void A_solution_survives_being_given_atlas_capacities()
    {
        // LevelDefinition clones itself when a relic raises the token ceiling. A clone that dropped the
        // solution would make a certified level uncertifiable the moment a player unlocked a node.
        // Arrange
        var level = LevelLoader.Parse(Corridor + "\nsolve: 1,0");

        // Act
        var raised = level.WithStartingResources(
            startingTokens: 1, startingSkips: 1, tokenCapacity: 5, skipCapacity: 5);

        // Assert
        Assert.Single(raised.Solution);
    }
}
