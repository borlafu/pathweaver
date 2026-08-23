using Pathweaver.Core.Endless;

namespace Pathweaver.Core.Tests.Solving;

/// <summary>
/// Whether a generated round can be finished by playing it, not merely by building its plan.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EndlessGeneratorTests"/> proves the plan scores what the target asks for, which is a
/// statement about the board. This is the stronger statement: that the plan is reachable through the
/// hand, where the tile that arrives is the bag's choice and the only ways out of a bad draw are
/// rotation and a skip.
/// </para>
/// <para>
/// Only rounds up to nine are checked, and that is a measured limit rather than a guess: rounds 14
/// and 20 both exhaust a 600,000-state budget. An open hexagon multiplies the search — the same
/// effect that made an early version of biome1-11 unprovable — so an unproven round says something
/// about the search, not about the board. What guarantees the later rounds is the construction: the
/// plan is built first and EndlessGeneratorTests checks it scores what the target asks for.
/// </para>
/// </remarks>
public class EndlessSolvabilityTests
{
    [Theory]
    [InlineData(1, 5UL)]
    [InlineData(2, 5UL)]
    [InlineData(1, 77UL)]
    [InlineData(5, 5UL)]
    [InlineData(9, 5UL)]
    public void An_early_generated_round_can_be_played_to_its_target(int round, ulong seed)
    {
        // Arrange
        var generated = EndlessGenerator.Generate(round, seed);

        // Act
        var result = LevelSolver.Solve(generated.Level, generated.Level.Seed);

        // Assert
        var reason = result.BudgetExhausted
            ? $"was not proven within {LevelSolver.DefaultNodeBudget} states"
            : result.Exhaustive ? "cannot be completed" : "was not solved by a narrowed search";

        Assert.True(result.Solved, $"round {round} at seed {seed} {reason}.");
    }
}
