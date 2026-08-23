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
    public void Tokens_left_at_the_end_of_a_round_come_with_the_player()
    {
        // A Pivot Token is earned by building a route of four or more, so wiping the pool between
        // rounds takes back a reward the player has already been given. It also made the counter
        // look broken: pips appeared and then vanished for no visible reason.
        // Arrange
        var run = EndlessRun.Start(seed: 12UL);

        // Act
        var next = run.Cleared(pivotTokensLeft: 2, skipsLeft: 1);

        // Assert
        Assert.Equal(2, next.CarriedPivotTokens);
        Assert.Equal(1, next.CarriedSkips);
    }

    [Fact]
    public void A_carried_token_is_added_to_the_rounds_own_allowance()
    {
        // Arrange — round 2 grants no Pivot Token of its own and three skips
        var run = EndlessRun.Start(seed: 12UL).Cleared(pivotTokensLeft: 2, skipsLeft: 5);

        // Act
        var state = run.CurrentRound().Level.CreateGame();

        // Assert — the allowance is a floor, not a replacement, so hoarding is rewarded and
        // spending is never punished with a worse start
        Assert.Equal(2, state.PivotTokens.Count);
        Assert.Equal(5, state.SkipTokens.Count);
    }

    [Fact]
    public void A_round_never_starts_below_its_own_allowance()
    {
        // Arrange — a player who spent everything still gets the round's three skips
        var run = EndlessRun.Start(seed: 12UL).Cleared(pivotTokensLeft: 0, skipsLeft: 0);

        // Act
        var state = run.CurrentRound().Level.CreateGame();

        // Assert
        Assert.Equal(3, state.SkipTokens.Count);
    }

    [Fact]
    public void Starting_again_drops_the_tokens_with_the_run()
    {
        // Arrange
        var run = EndlessRun.Start(seed: 1UL).Cleared(pivotTokensLeft: 4, skipsLeft: 4);

        // Act
        var restarted = run.Abandoned(newSeed: 2UL);

        // Assert — carrying a hoard into a fresh run would make round one of the second attempt
        // easier than round one of the first, which is not what starting again means
        Assert.Equal(0, restarted.CarriedPivotTokens);
        Assert.Equal(0, restarted.CarriedSkips);
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
        var run = EndlessRun.Start(seed: 8_000_000_000_000_000_123UL)
            .Cleared(pivotTokensLeft: 3, skipsLeft: 2)
            .Cleared(pivotTokensLeft: 1, skipsLeft: 4);

        // Act
        var restored = EndlessRunFormat.Read(EndlessRunFormat.Write(run));

        // Assert
        Assert.Equal(run.Seed, restored.Seed);
        Assert.Equal(run.Round, restored.Round);
        Assert.Equal(run.BestRound, restored.BestRound);
        Assert.Equal(run.CarriedPivotTokens, restored.CarriedPivotTokens);
        Assert.Equal(run.CarriedSkips, restored.CarriedSkips);
    }

    [Fact]
    public void A_run_written_by_the_older_format_still_reads()
    {
        // Version 1 files carry three numbers and know nothing about carried tokens. A player
        // updating the app keeps their round; the tokens they were holding are the price.
        // Act
        var run = EndlessRunFormat.Read("pathweaver-endless 1\n4242 6 6");

        // Assert
        Assert.Equal(4242UL, run.Seed);
        Assert.Equal(6, run.Round);
        Assert.Equal(0, run.CarriedPivotTokens);
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
