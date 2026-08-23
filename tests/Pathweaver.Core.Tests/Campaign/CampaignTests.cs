using Pathweaver.Core.Campaign;

namespace Pathweaver.Core.Tests.Campaign;

public class CampaignProgressTests
{
    [Fact]
    public void A_new_player_has_cleared_nothing()
    {
        Assert.Equal(0, CampaignProgress.Empty.ClearedCount);
        Assert.False(CampaignProgress.Empty.IsCleared("biome1-01"));
    }

    [Fact]
    public void Clearing_a_level_returns_new_progress_and_leaves_the_old_alone()
    {
        // Arrange
        var before = CampaignProgress.Empty;

        // Act
        var after = before.WithCleared("biome1-01");

        // Assert
        Assert.False(before.IsCleared("biome1-01"));
        Assert.True(after.IsCleared("biome1-01"));
    }

    [Fact]
    public void Clearing_the_same_level_twice_is_not_an_error()
    {
        // A player may replay a level for a better score, and treating that as a fault would
        // make replaying something the game has to special-case.
        var progress = CampaignProgress.Empty.WithCleared("biome1-01").WithCleared("biome1-01");

        Assert.Equal(1, progress.ClearedCount);
    }

    [Fact]
    public void A_blank_identifier_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => CampaignProgress.Empty.WithCleared("   "));
    }

    [Fact]
    public void Blanks_and_duplicates_are_ignored_when_building_from_a_list()
    {
        var progress = CampaignProgress.Of(new[] { "a", "a", "", "  ", "b" });

        Assert.Equal(2, progress.ClearedCount);
    }

    [Fact]
    public void Cleared_levels_come_back_in_a_stable_order()
    {
        // The order is what gets written to disk, so an unstable one would rewrite the file
        // on every save even when nothing changed.
        var first = CampaignProgress.Of(new[] { "c", "a", "b" });
        var second = CampaignProgress.Of(new[] { "b", "c", "a" });

        Assert.Equal(first.ClearedLevels, second.ClearedLevels);
    }
}

public class CampaignOrderTests
{
    private static readonly string[] Levels = { "biome1-01", "biome1-02", "biome1-03" };

    [Fact]
    public void The_first_level_is_always_open()
    {
        var campaign = Pathweaver.Core.Campaign.Campaign.Of(Levels);

        Assert.True(campaign.IsUnlocked("biome1-01", CampaignProgress.Empty));
    }

    [Fact]
    public void A_later_level_stays_locked_until_the_one_before_it_is_cleared()
    {
        var campaign = Pathweaver.Core.Campaign.Campaign.Of(Levels);

        Assert.False(campaign.IsUnlocked("biome1-02", CampaignProgress.Empty));
        Assert.True(campaign.IsUnlocked("biome1-02", CampaignProgress.Empty.WithCleared("biome1-01")));
    }

    [Fact]
    public void Clearing_a_level_out_of_order_does_not_open_the_rest()
    {
        // Progress records identifiers rather than a count, so credit for a later level cannot
        // be mistaken for credit for the ones before it.
        var campaign = Pathweaver.Core.Campaign.Campaign.Of(Levels);
        var progress = CampaignProgress.Empty.WithCleared("biome1-03");

        Assert.False(campaign.IsUnlocked("biome1-02", progress));
    }

    [Fact]
    public void A_cleared_level_stays_open()
    {
        var campaign = Pathweaver.Core.Campaign.Campaign.Of(Levels);
        var progress = CampaignProgress.Empty.WithCleared("biome1-01");

        Assert.True(campaign.IsUnlocked("biome1-01", progress));
    }

    [Fact]
    public void An_unknown_level_is_never_unlocked()
    {
        var campaign = Pathweaver.Core.Campaign.Campaign.Of(Levels);

        Assert.False(campaign.IsUnlocked("does-not-exist", CampaignProgress.Empty));
    }

    [Fact]
    public void The_next_level_is_the_first_one_not_yet_cleared()
    {
        var campaign = Pathweaver.Core.Campaign.Campaign.Of(Levels);

        Assert.Equal("biome1-01", campaign.NextLevel(CampaignProgress.Empty));
        Assert.Equal(
            "biome1-02",
            campaign.NextLevel(CampaignProgress.Empty.WithCleared("biome1-01")));
    }

    [Fact]
    public void The_last_level_is_offered_once_everything_is_cleared()
    {
        var campaign = Pathweaver.Core.Campaign.Campaign.Of(Levels);
        var progress = CampaignProgress.Of(Levels);

        Assert.Equal("biome1-03", campaign.NextLevel(progress));
    }

    [Fact]
    public void A_campaign_needs_levels()
    {
        Assert.Throws<ArgumentException>(() => Pathweaver.Core.Campaign.Campaign.Of(Array.Empty<string>()));
    }

    [Fact]
    public void A_repeated_level_is_rejected()
    {
        // The same level in two places would make unlocking ambiguous.
        Assert.Throws<ArgumentException>(
            () => Pathweaver.Core.Campaign.Campaign.Of(new[] { "a", "b", "a" }));
    }
}

public class CampaignProgressFormatTests
{
    [Fact]
    public void Progress_survives_a_round_trip()
    {
        var progress = CampaignProgress.Of(new[] { "biome1-01", "biome1-02" });

        var reloaded = CampaignProgressFormat.Read(CampaignProgressFormat.Write(progress));

        Assert.Equal(progress.ClearedLevels, reloaded.ClearedLevels);
    }

    [Fact]
    public void Writing_the_same_progress_twice_gives_identical_text()
    {
        // So an autosave can skip writing when nothing changed.
        var progress = CampaignProgress.Of(new[] { "b", "a" });

        Assert.Equal(CampaignProgressFormat.Write(progress), CampaignProgressFormat.Write(progress));
    }

    [Fact]
    public void Nothing_read_from_nothing()
    {
        Assert.Equal(0, CampaignProgressFormat.Read("").ClearedCount);
        Assert.Equal(0, CampaignProgressFormat.Read(null).ClearedCount);
    }

    [Theory]
    [InlineData("not a progress file")]
    [InlineData("pathweaver-progress")]
    [InlineData("pathweaver-progress abc")]
    [InlineData("pathweaver-progress 99\nbiome1-01")]
    public void Unreadable_text_yields_empty_progress_rather_than_an_error(string text)
    {
        // Losing a campaign to a damaged file is bad; refusing to start the game because of one
        // is worse, and a player can do nothing about either.
        Assert.Equal(0, CampaignProgressFormat.Read(text).ClearedCount);
    }

    [Fact]
    public void An_unrecognised_level_identifier_is_kept()
    {
        // A player moving between builds should not lose credit for a level that was renamed
        // or temporarily removed.
        var progress = CampaignProgressFormat.Read("pathweaver-progress 1\nsome-old-level\n");

        Assert.True(progress.IsCleared("some-old-level"));
    }

    [Fact]
    public void The_format_is_readable_by_a_person()
    {
        // Deliberately text: this file has to survive every future version, and one a human can
        // repair by hand is worth more than a compact one.
        var text = CampaignProgressFormat.Write(CampaignProgress.Empty.WithCleared("biome1-01"));

        Assert.Contains("biome1-01", text);
        Assert.StartsWith("pathweaver-progress 1", text);
    }
}
