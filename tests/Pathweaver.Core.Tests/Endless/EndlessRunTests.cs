using Pathweaver.Core.Endless;

namespace Pathweaver.Core.Tests.Endless;

public class EndlessRunTests
{
    [Fact]
    public void A_new_run_starts_at_round_one()
    {
        // Act
        var run = EndlessRun.Start(seed: 12UL);

        // Assert
        Assert.Equal(1, run.Round);
        Assert.Equal(1, run.BestRound);
        Assert.Equal(12UL, run.Seed);
    }

    [Fact]
    public void Clearing_a_round_advances_to_the_next()
    {
        // Arrange
        var run = EndlessRun.Start(seed: 12UL);

        // Act
        var next = run.Cleared();

        // Assert
        Assert.Equal(2, next.Round);
        Assert.Equal(2, next.BestRound);

        // The value it came from is untouched, as everything in the simulation is.
        Assert.Equal(1, run.Round);
    }

    [Fact]
    public void Giving_up_returns_to_the_first_round_but_keeps_the_best()
    {
        // A run is not a score to protect: the point of Endless is starting again. What is worth
        // keeping is how far the player has ever got, because that is the only number the mode has.
        // Arrange
        var run = EndlessRun.Start(seed: 12UL).Cleared().Cleared().Cleared();
        Assert.Equal(4, run.Round);

        // Act
        var restarted = run.Abandoned(newSeed: 99UL);

        // Assert
        Assert.Equal(1, restarted.Round);
        Assert.Equal(4, restarted.BestRound);
        Assert.Equal(99UL, restarted.Seed);
    }

    [Fact]
    public void A_run_survives_being_written_and_read()
    {
        // Arrange
        var run = EndlessRun.Start(seed: 8_000_000_000_000_000_123UL).Cleared().Cleared();

        // Act
        var restored = EndlessRunFormat.Read(EndlessRunFormat.Write(run));

        // Assert
        Assert.Equal(run.Seed, restored.Seed);
        Assert.Equal(run.Round, restored.Round);
        Assert.Equal(run.BestRound, restored.BestRound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nonsense")]
    [InlineData("pathweaver-endless 99\n1 1 1")]
    [InlineData("pathweaver-endless 1\nnot numbers")]
    public void Unreadable_text_becomes_a_fresh_run_rather_than_an_error(string text)
    {
        // The same rule the campaign file follows: refusing to start the game because a progress
        // file is damaged is worse than losing the progress, and the player can do nothing either
        // way. A fresh run needs a seed, so the caller supplies one.
        // Act
        var run = EndlessRunFormat.Read(text, fallbackSeed: 5UL);

        // Assert
        Assert.Equal(1, run.Round);
        Assert.Equal(5UL, run.Seed);
    }

    [Fact]
    public void An_impossible_round_keeps_the_seed_it_was_stored_with()
    {
        // Only the round is damaged here, and a seed is still a seed. Throwing it away would give
        // the player a different set of boards for no reason, so the run restarts on its own seed.
        // Act
        var run = EndlessRunFormat.Read("pathweaver-endless 1\n12 0 0", fallbackSeed: 5UL);

        // Assert
        Assert.Equal(1, run.Round);
        Assert.Equal(12UL, run.Seed);
    }

    [Fact]
    public void The_round_a_run_is_on_generates_that_round()
    {
        // Arrange
        var run = EndlessRun.Start(seed: 3UL).Cleared();

        // Act
        var round = run.CurrentRound();

        // Assert
        Assert.Equal("endless-2", round.Level.Id);
        Assert.Equal(EndlessGenerator.Generate(2, 3UL).Level.Seed, round.Level.Seed);
    }
}
