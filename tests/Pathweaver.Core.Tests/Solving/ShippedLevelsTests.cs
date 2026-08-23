using Pathweaver.Core.Levels;
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
    /// <summary>
    /// Seeds a level must all be solvable under.
    /// </summary>
    /// <remarks>
    /// The Daily Expedition derives its seed from the date, so a level is played
    /// under seeds nobody chose. Checking several catches a level that only works
    /// when the tile order is kind.
    /// </remarks>
    private static readonly ulong[] Seeds = { 1UL, 2UL, 3UL, 7UL, 42UL };

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
    public void A_shipped_level_can_be_completed_under_every_seed(string path)
    {
        // Arrange
        var level = LevelLoader.Parse(File.ReadAllText(path));

        // Act / Assert
        foreach (var seed in Seeds)
        {
            var result = LevelSolver.Solve(level, seed);

            Assert.True(
                result.Solved,
                result.BudgetExhausted
                    ? $"{level.Id} was not proven solvable for seed {seed} within {LevelSolver.DefaultNodeBudget} states."
                    : $"{level.Id} cannot be completed for seed {seed}.");
        }
    }

    [Theory]
    [MemberData(nameof(LevelFiles))]
    public void A_solution_actually_reaches_the_target_when_replayed(string path)
    {
        // Trusting the solver's verdict without replaying it would let a bug in the
        // solver certify a level the game cannot actually clear.
        // Arrange
        var level = LevelLoader.Parse(File.ReadAllText(path));
        var result = LevelSolver.Solve(level, Seeds[0]);
        Assert.True(result.Solved);

        // Act — replay the reported moves from a fresh game
        var state = level.CreateGame(Seeds[0]);
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
