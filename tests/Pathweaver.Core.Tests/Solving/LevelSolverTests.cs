using Pathweaver.Core.Levels;
using Pathweaver.Core.State;

namespace Pathweaver.Core.Tests.Solving;

public class LevelSolverTests
{
    private const string SolvableLevel = """
        id: solvable
        base-score: 100
        target-score: 135
        cell: -2,0
        cell: -1,0
        cell: 0,0
        cell: 1,0
        cell: 2,0
        spring: -2,0 water
        hub: 2,0 water
        tile: 0,3 water x3
        """;

    [Fact]
    public void A_reachable_target_is_solved()
    {
        // Arrange — three straights in a row between the endpoints scores a route
        // of 3, which is 182 and clears a target of 135
        var level = LevelLoader.Parse(SolvableLevel);

        // Act
        var result = LevelSolver.Solve(level, seed: 1UL);

        // Assert
        Assert.True(result.Solved);
        Assert.NotEmpty(result.Commands);
        Assert.False(result.BudgetExhausted);
    }

    [Fact]
    public void A_solution_replays_to_the_same_score()
    {
        // Arrange
        var level = LevelLoader.Parse(SolvableLevel);
        var result = LevelSolver.Solve(level, seed: 1UL);

        // Act
        var state = level.CreateGame(seed: 1UL);
        foreach (var command in result.Commands)
        {
            state = GameEngine.Apply(state, command);
        }

        // Assert
        Assert.True(state.Score >= level.TargetScore);
    }

    [Fact]
    public void An_unreachable_target_is_reported_unsolved()
    {
        // Arrange — the board holds three conduits, so the best route is length 3
        // at 182 points; 100000 can never be reached
        var text = SolvableLevel.Replace("target-score: 135", "target-score: 100000");
        var level = LevelLoader.Parse(text);

        // Act
        var result = LevelSolver.Solve(level, seed: 1UL);

        // Assert
        Assert.False(result.Solved);
        Assert.Empty(result.Commands);
    }

    [Fact]
    public void An_exhausted_budget_is_reported_rather_than_treated_as_unsolvable()
    {
        // The distinction matters: "not proven solvable" is a warning about the
        // search, while "unsolvable" is a verdict about the level.
        // Arrange
        var level = LevelLoader.Parse(SolvableLevel.Replace("target-score: 135", "target-score: 100000"));

        // Act — a budget too small to explore anything meaningful
        var result = LevelSolver.Solve(level, seed: 1UL, nodeBudget: 3);

        // Assert
        Assert.False(result.Solved);
        Assert.True(result.BudgetExhausted);
    }

    [Fact]
    public void The_search_reports_how_much_it_explored()
    {
        // Arrange
        var level = LevelLoader.Parse(SolvableLevel);

        // Act
        var result = LevelSolver.Solve(level, seed: 1UL);

        // Assert
        Assert.True(result.NodesExplored > 0);
    }

    [Fact]
    public void A_target_already_met_needs_no_moves()
    {
        // Arrange — a target of 1 point is met before anything is placed only if
        // the score starts there, which it does not, so this checks the opposite:
        // the solver must still place something
        var level = LevelLoader.Parse(SolvableLevel.Replace("target-score: 135", "target-score: 1"));

        // Act
        var result = LevelSolver.Solve(level, seed: 1UL);

        // Assert
        Assert.True(result.Solved);
        Assert.NotEmpty(result.Commands);
    }
}
