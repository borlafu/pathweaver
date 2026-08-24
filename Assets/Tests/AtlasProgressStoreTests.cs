using System.IO;
using NUnit.Framework;
using Pathweaver.Core.Atlas;
using Pathweaver.Game.App;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Whether the atlas survives closing the game.
    /// </summary>
    /// <remarks>
    /// Star Essence is earned a couple of points at a time across a whole campaign, so this file
    /// represents more play than any other. Losing it costs more than losing a run.
    /// </remarks>
    public class AtlasProgressStoreTests
    {
        private string _directory;
        private AtlasProgressStore _store;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pathweaver-atlas-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
            _store = new AtlasProgressStore(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [Test]
        public void A_player_with_no_file_has_an_empty_atlas()
        {
            var progress = _store.Load();

            Assert.That(progress.Essence, Is.EqualTo(0));
            Assert.That(progress.UnlockedNodes, Is.Empty);
        }

        [Test]
        public void Essence_and_unlocked_nodes_both_come_back()
        {
            // Arrange
            var progress = AtlasProgress.Empty
                .WithEssence(14)
                .WithUnlocked("spring-well", cost: 3)
                .WithUnlocked("ley-line", cost: 4);

            // Act
            _store.Save(progress);
            var restored = _store.Load();

            // Assert
            Assert.That(restored.Essence, Is.EqualTo(7));
            Assert.That(restored.IsUnlocked("spring-well"), Is.True);
            Assert.That(restored.IsUnlocked("ley-line"), Is.True);
        }

        [Test]
        public void A_damaged_file_costs_the_atlas_rather_than_the_game()
        {
            File.WriteAllText(_store.Path, "this is not an atlas");

            Assert.That(_store.Load().Essence, Is.EqualTo(0));
        }

        [Test]
        public void Saving_twice_leaves_one_file_and_no_temporary()
        {
            // The write goes via a temporary file so a kill cannot leave it half written, and that
            // temporary must not survive the write.
            _store.Save(AtlasProgress.Empty.WithEssence(1));
            _store.Save(AtlasProgress.Empty.WithEssence(2));

            var files = Directory.GetFiles(_directory);

            Assert.That(files.Length, Is.EqualTo(1));
            Assert.That(Path.GetFileName(files[0]), Is.EqualTo("atlas-progress.txt"));
        }

        [Test]
        public void The_shipped_packs_load_and_agree_with_the_bonuses_they_promise()
        {
            // Reads the packs the build actually ships, through the same catalogue the game uses, so a
            // pack copied in badly by the build script fails here rather than on a device.
            var map = AtlasCatalogue.Load();

            Assert.That(map.Nodes, Is.Not.Empty);

            var everything = AtlasProgress.Of(
                System.Linq.Enumerable.Select(map.Nodes, node => node.Id), essence: 0);

            var bonuses = map.BonusesFor(everything);

            Assert.That(bonuses.Skips + bonuses.Tokens + bonuses.EssencePerClear, Is.GreaterThan(0));
        }
    }
}
