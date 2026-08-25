using System.IO;
using NUnit.Framework;
using Pathweaver.Core.Atlas;
using Pathweaver.Core.Campaign;
using Pathweaver.Core.Endless;
using Pathweaver.Core.Levels;
using Pathweaver.Game.App;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Whether resetting the game actually forgets everything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Progress lives in four places, and the failure worth guarding against is a wipe that clears
    /// three of them. A player who asks for a fresh start and is dropped back onto their old board,
    /// or who finds their atlas still bought, has been told the game ignored them.
    /// </para>
    /// <para>
    /// Every store is pointed at one temporary directory here, which is also how the game arranges
    /// them: they all write to <c>Application.persistentDataPath</c>. A reset that only cleared its
    /// own file type would pass a test that gave each store a directory of its own.
    /// </para>
    /// </remarks>
    public class ProgressResetTests
    {
        // A verbatim string rather than a raw literal: Unity compiles C# 9.
        private const string LevelText =
            "id: reset-level\n" +
            "base-score: 100\n" +
            "target-score: 135\n" +
            "cell: -2,0\n" +
            "cell: -1,0\n" +
            "cell: 0,0\n" +
            "cell: 1,0\n" +
            "cell: 2,0\n" +
            "spring: -2,0 water\n" +
            "hub: 2,0 water\n" +
            "tile: 0,3 water x3\n";

        private string _directory;
        private SaveService _saves;
        private CampaignProgressStore _campaign;
        private AtlasProgressStore _atlas;
        private EndlessRunStore _endless;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pathweaver-reset-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);

            _saves = new SaveService(_directory);
            _campaign = new CampaignProgressStore(_directory);
            _atlas = new AtlasProgressStore(_directory);
            _endless = new EndlessRunStore(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        /// <summary>Puts something in every place progress is kept.</summary>
        private void PlayForAWhile()
        {
            var level = LevelLoader.Parse(LevelText);

            _saves.Save("reset-level", level.CreateGame(seed: 42UL));
            _saves.Save("endless-7", level.CreateGame(seed: 7UL));

            _campaign.Save(CampaignProgress.Empty.WithCleared("biome1-01").WithPivotTokens(2));
            _atlas.Save(AtlasProgress.Empty.WithEssence(20).WithUnlocked("spring-well", cost: 3));
            _endless.Save(EndlessRun.Start(seed: 99UL).Cleared(pivotTokensLeft: 1, skipsLeft: 2));
        }

        [Test]
        public void A_wipe_leaves_nothing_behind_in_any_of_the_four_places()
        {
            // Arrange
            PlayForAWhile();

            // Act
            ProgressReset.Wipe(_saves, _campaign, _atlas, _endless);

            // Assert
            Assert.That(_saves.HasSave("reset-level"), Is.False, "a campaign board survived");
            Assert.That(_saves.HasSave("endless-7"), Is.False, "an endless board survived");

            var campaign = _campaign.Load();
            Assert.That(campaign.ClearedLevels, Is.Empty);
            Assert.That(campaign.PivotTokens, Is.EqualTo(0));

            var atlas = _atlas.Load();
            Assert.That(atlas.UnlockedNodes, Is.Empty);
            Assert.That(atlas.Essence, Is.EqualTo(0));

            Assert.That(_endless.Load().Round, Is.EqualTo(1));
        }

        [Test]
        public void A_wipe_leaves_the_folder_as_empty_as_a_first_launch()
        {
            // The stronger claim, and the one that catches a file type nobody thought about: after a
            // reset there is nothing at all to read. Anything left here would be something a future
            // build might start honouring again.
            // Arrange
            PlayForAWhile();

            // Act
            ProgressReset.Wipe(_saves, _campaign, _atlas, _endless);

            // Assert
            Assert.That(Directory.GetFiles(_directory), Is.Empty);
        }

        [Test]
        public void A_wipe_with_nothing_to_wipe_is_harmless()
        {
            // The player who opens settings on a fresh install and presses it out of curiosity.
            Assert.DoesNotThrow(() => ProgressReset.Wipe(_saves, _campaign, _atlas, _endless));

            Assert.That(_campaign.Load().ClearedLevels, Is.Empty);
        }

        [Test]
        public void A_quarantined_save_is_cleared_too()
        {
            // Unreadable saves are moved aside rather than deleted, which is right for diagnosis and
            // wrong for a player asking to be forgotten.
            // Arrange
            var quarantined = _saves.PathFor("broken") + ".corrupt";
            File.WriteAllBytes(quarantined, new byte[] { 1, 2, 3 });

            // Act
            ProgressReset.Wipe(_saves, _campaign, _atlas, _endless);

            // Assert
            Assert.That(File.Exists(quarantined), Is.False);
        }

        [Test]
        public void Deleting_boards_reports_how_many_there_were()
        {
            // Arrange
            PlayForAWhile();

            // Act / Assert
            Assert.That(_saves.DeleteAll(), Is.EqualTo(2));
            Assert.That(_saves.DeleteAll(), Is.EqualTo(0));
        }
    }
}
