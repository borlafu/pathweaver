using Pathweaver.Core.Levels;
using Pathweaver.Core.Scoring;
using Pathweaver.Core.State;

namespace Pathweaver.Core.Tests.Solving;

/// <summary>
/// The gate that keeps an unsolvable level out of the game.
/// </summary>
/// <remarks>
/// Every file under <c>levels/</c> must load and must be completable. A level that
/// cannot be cleared fails CI here rather than reaching a player who then cannot
/// tell whether the fault is theirs.
/// </remarks>
public class ShippedLevelsTests
{
    public static TheoryData<string> LevelFiles()
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.GetFiles(LevelsDirectory(), "*.pwlevel"))
        {
            data.Add(path);
        }

        return data;
    }

    [Fact]
    public void At_least_one_level_ships()
    {
        // Guards the guard: if the directory moved, every other test here would
        // pass by vacuously having nothing to check.
        Assert.NotEmpty(Directory.GetFiles(LevelsDirectory(), "*.pwlevel"));
    }

    [Theory]
    [MemberData(nameof(LevelFiles))]
    public void A_shipped_level_loads(string path)
    {
        // Act
        var level = LevelLoader.Parse(File.ReadAllText(path));

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(level.Id));
        Assert.Equal(Path.GetFileNameWithoutExtension(path), level.Id);
    }

    [Theory]
    [MemberData(nameof(LevelFiles))]
    public void A_shipped_level_can_be_completed_at_its_own_seed(string path)
    {
        // The puzzle players actually get.
        var level = LevelLoader.Parse(File.ReadAllText(path));
        var result = LevelSolver.Solve(level, level.Seed);

        // Three failures needing three different responses: widen the search, look at the level, or
        // accept that a narrowed search cannot prove a negative.
        var reason = result.BudgetExhausted
            ? $"was not proven solvable within {LevelSolver.DefaultNodeBudget} states"
            : result.Exhaustive
                ? "cannot be completed"
                : "was not solved; the search narrowed its options, so this is not proof it is impossible";

        Assert.True(result.Solved, $"{level.Id} (seed {level.Seed}) {reason}.");
    }

    [Theory]
    [MemberData(nameof(LevelFiles))]
    public void A_level_asks_for_more_than_a_single_short_route(string path)
    {
        // A target two conduits already cover is not a puzzle: the player drops a tile beside each
        // endpoint and the level ends before it has asked anything.
        //
        // The bar is deliberately this low. Set at three conduits it failed biome1-02, whose whole
        // lesson is rotation inside a zigzag corridor three cells long — a short route can still be
        // a real puzzle when the board only admits one orientation.
        //
        // This replaces an earlier check that targets never fell from one level to the next, which
        // encoded a wrong assumption: difficulty is not monotonic in target score, because a cramped
        // board with a low target can be harder than an open one with a high target.
        // Arrange
        var level = LevelLoader.Parse(File.ReadAllText(path));

        // Act
        var trivialRoute = ScoreTable.ScoreFor(level.BaseRouteScore, length: 2);

        // Assert
        Assert.True(
            level.TargetScore > trivialRoute,
            $"{level.Id} targets {level.TargetScore}, which a single two-conduit route "
            + $"({trivialRoute}) already clears.");
    }

    [Theory]
    [MemberData(nameof(LevelFiles))]
    public void A_solution_actually_reaches_the_target_when_replayed(string path)
    {
        // Trusting the solver's verdict without replaying it would let a bug in the
        // solver certify a level the game cannot actually clear.
        // Arrange
        var level = LevelLoader.Parse(File.ReadAllText(path));
        var result = LevelSolver.Solve(level, level.Seed);
        Assert.True(result.Solved);

        // Act — replay the reported moves from a fresh game
        var state = level.CreateGame();
        foreach (var command in result.Commands)
        {
            state = GameEngine.Apply(state, command);
        }

        // Assert
        Assert.True(
            state.Score >= level.TargetScore,
            $"Replaying the solution reached {state.Score}, short of {level.TargetScore}.");
    }

    /// <summary>
    /// Finds the repository's level directory by locating the solution file.
    /// </summary>
    /// <remarks>
    /// Anchored on <c>Pathweaver.slnx</c> rather than by looking for a directory
    /// called "levels". macOS is case-insensitive, so that search matched this test
    /// project's own <c>Levels</c> folder and quietly found no level files — a
    /// failure that would not have reproduced on the Linux CI runner.
    /// </remarks>
    private static string LevelsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Pathweaver.slnx")))
            {
                return Path.Combine(directory.FullName, "levels");
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"No Pathweaver.slnx found above {AppContext.BaseDirectory}.");
    }
}
