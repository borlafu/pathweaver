using Pathweaver.Core.Save;
using Pathweaver.Core.State;
using Pathweaver.Core.Tests.State;

namespace Pathweaver.Core.Tests.Save;

public class SaveGameTests
{
    private static GameState MidGame()
        => GameFixture.PlayRow(GameFixture.NewGame(startingTokens: 1), 2);

    private static GameState CompletedGame()
        => GameFixture.PlayRow(GameFixture.NewGame(), 4);

    [Fact]
    public void A_saved_game_reloads_with_the_same_score_and_tokens()
    {
        // Arrange
        var original = CompletedGame();

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Assert
        Assert.Equal(original.Score, reloaded.Score);
        Assert.Equal(original.PivotTokens, reloaded.PivotTokens);
        Assert.Equal(original.BaseRouteScore, reloaded.BaseRouteScore);
    }

    [Fact]
    public void A_saved_game_reloads_with_the_same_board()
    {
        // Arrange
        var original = MidGame();

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Assert
        Assert.Equal(original.Board.Coordinates, reloaded.Board.Coordinates);
        Assert.Equal(
            original.Board.OccupiedCells.Select(cell => (cell.Coordinate, cell.Value)),
            reloaded.Board.OccupiedCells.Select(cell => (cell.Coordinate, cell.Value)));
    }

    [Fact]
    public void A_saved_game_reloads_with_the_same_endpoints()
    {
        // Arrange
        var original = MidGame();

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Assert
        Assert.Equal(original.Endpoints, reloaded.Endpoints);
    }

    [Fact]
    public void A_saved_game_reloads_with_the_same_tile_in_hand()
    {
        // PRD section 2.1 asks for instant suspend and resume, so the player must
        // find the same tile waiting.
        // Arrange
        var original = MidGame();

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Assert
        Assert.Equal(original.HeldTile, reloaded.HeldTile);
    }

    [Fact]
    public void A_saved_game_reloads_with_the_same_completed_routes()
    {
        // Without this, resuming would let a player be paid a second time for a
        // route already harvested.
        // Arrange
        var original = CompletedGame();

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Assert
        Assert.Equal(original.CompletedRoutes, reloaded.CompletedRoutes);
    }

    [Fact]
    public void Resuming_continues_the_same_draw_sequence()
    {
        // The tile bag carries generator state, not just its contents. If that
        // were lost, the Daily Expedition would diverge from everyone else's the
        // moment a player suspended the app.
        // Arrange
        var original = MidGame();
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Act — play the same two moves on each
        var continuedOriginal = original;
        var continuedReloaded = reloaded;
        for (var index = 2; index < 4; index++)
        {
            continuedOriginal = GameEngine.Apply(continuedOriginal, new PlaceTile(GameFixture.RowCells[index], 0));
            continuedReloaded = GameEngine.Apply(continuedReloaded, new PlaceTile(GameFixture.RowCells[index], 0));
        }

        // Assert
        Assert.Equal(continuedOriginal.HeldTile, continuedReloaded.HeldTile);
        Assert.Equal(continuedOriginal.Score, continuedReloaded.Score);
        Assert.Equal(continuedOriginal.PivotTokens, continuedReloaded.PivotTokens);
    }

    [Fact]
    public void Resuming_keeps_a_deadlock_verdict_intact()
    {
        // Arrange
        var original = MidGame();

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Assert
        Assert.Equal(original.IsDeadlocked, reloaded.IsDeadlocked);
        Assert.Equal(
            original.LegalPlacements.Select(placement => (placement.Coordinate, placement.Rotation)),
            reloaded.LegalPlacements.Select(placement => (placement.Coordinate, placement.Rotation)));
    }

    [Fact]
    public void Writing_the_same_state_twice_produces_identical_bytes()
    {
        // A stable encoding means a save file can be compared, hashed, or diffed,
        // and that an unchanged game does not rewrite storage on every autosave.
        // Arrange
        var state = MidGame();

        // Act
        var first = SaveGame.Write(state);
        var second = SaveGame.Write(state);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void A_save_survives_two_round_trips_unchanged()
    {
        // Arrange
        var bytes = SaveGame.Write(MidGame());

        // Act
        var again = SaveGame.Write(SaveGame.Read(bytes));

        // Assert
        Assert.Equal(bytes, again);
    }

    [Fact]
    public void The_format_version_is_published()
    {
        // Bumped to 3 when a save started recording what each pair had been paid for, which is what
        // lets a pair connected a better way later be paid the difference.
        Assert.Equal(3, SaveGame.FormatVersion);
    }

    [Fact]
    public void Older_saves_are_still_readable()
    {
        // The version field exists so a player mid-run through an update keeps their
        // board. If this drops below the current version, that promise is gone.
        Assert.True(SaveGame.MinimumReadableVersion < SaveGame.FormatVersion);
    }

    [Fact]
    public void A_saved_game_reloads_with_the_same_skips()
    {
        // Arrange
        var original = GameFixture.NewGame(startingSkips: 3);

        // Act
        var reloaded = SaveGame.Read(SaveGame.Write(original));

        // Assert
        Assert.Equal(original.SkipTokens, reloaded.SkipTokens);
    }

    [Fact]
    public void A_version_1_save_loads_with_skips_filled_in()
    {
        // Version 1 predates skips entirely. Rejecting such a save would cost a player
        // their run for updating the app, which is exactly what the version field is
        // meant to prevent.
        // Arrange — a current save, rewritten as version 1 by dropping the skip count
        var current = SaveGame.Write(GameFixture.NewGame(startingSkips: 2));

        var asVersionOne = new List<byte>();
        asVersionOne.AddRange(current.Take(4));                  // marker
        asVersionOne.AddRange(BitConverter.GetBytes(1));         // version
        asVersionOne.AddRange(current.Skip(8).Take(20));         // base score, score, pivots
        asVersionOne.AddRange(current.Skip(32));                 // everything after the skips

        // Act
        var loaded = SaveGame.Read(asVersionOne.ToArray());

        // Assert
        Assert.Equal(3, loaded.SkipTokens.Count);
        Assert.Equal(0, loaded.Score);
    }

    [Fact]
    public void A_save_from_a_version_below_the_minimum_is_rejected()
    {
        // Arrange
        var bytes = SaveGame.Write(MidGame());
        bytes[4] = 0;

        // Act / Assert
        Assert.Throws<SaveFormatException>(() => SaveGame.Read(bytes));
    }

    [Fact]
    public void A_save_records_its_format_version()
    {
        // Act
        var bytes = SaveGame.Write(MidGame());

        // Assert
        Assert.Equal(SaveGame.FormatVersion, SaveGame.ReadFormatVersion(bytes));
    }

    [Fact]
    public void Data_that_is_not_a_save_is_rejected()
    {
        // Arrange — plausible-looking bytes with the wrong marker
        var notASave = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        // Act / Assert
        Assert.Throws<SaveFormatException>(() => SaveGame.Read(notASave));
    }

    [Fact]
    public void A_save_from_a_newer_version_is_rejected_with_a_clear_reason()
    {
        // Arrange — a valid save with the version field bumped beyond what this
        // build understands
        var bytes = SaveGame.Write(MidGame());
        bytes[4] = 99;

        // Act
        var error = Assert.Throws<SaveFormatException>(() => SaveGame.Read(bytes));

        // Assert — the message has to distinguish "too new" from "corrupt", since
        // one means the player downgraded and the other means data loss
        Assert.Contains("99", error.Message);
    }

    [Fact]
    public void A_truncated_save_is_rejected_rather_than_half_loaded()
    {
        // Arrange — a save cut short, as a kill during a write would leave it
        var bytes = SaveGame.Write(MidGame());
        var truncated = bytes.Take(bytes.Length / 2).ToArray();

        // Act / Assert
        Assert.Throws<SaveFormatException>(() => SaveGame.Read(truncated));
    }

    [Fact]
    public void A_save_with_a_corrupt_resource_kind_is_rejected()
    {
        // Arrange — flip the last stretch of payload to nonsense
        var bytes = SaveGame.Write(MidGame());
        for (var index = bytes.Length - 8; index < bytes.Length; index++)
        {
            bytes[index] = 0xFF;
        }

        // Act / Assert — corrupt data must not produce a playable but wrong game
        Assert.Throws<SaveFormatException>(() => SaveGame.Read(bytes));
    }

    [Fact]
    public void Empty_data_is_rejected()
    {
        Assert.Throws<SaveFormatException>(() => SaveGame.Read(Array.Empty<byte>()));
    }

    [Fact]
    public void Null_data_is_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => SaveGame.Read(null!));
    }

    [Fact]
    public void A_null_state_cannot_be_written()
    {
        Assert.Throws<ArgumentNullException>(() => SaveGame.Write(null!));
    }

    [Fact]
    public void A_save_stays_small_enough_to_write_atomically()
    {
        // The PRD's cold-boot and resume budgets assume a small file, and the
        // deliberate choice not to use SQLite rests on the payload staying tiny.
        // Act
        var bytes = SaveGame.Write(CompletedGame());

        // Assert
        Assert.True(bytes.Length < 4096, $"A mid-game save reached {bytes.Length} bytes.");
    }
}
