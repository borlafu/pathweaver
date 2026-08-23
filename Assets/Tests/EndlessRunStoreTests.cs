using System.IO;
using NUnit.Framework;
using Pathweaver.Core.Endless;
using Pathweaver.Game.App;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Whether an endless run survives closing the game.
    /// </summary>
    /// <remarks>
    /// The round reached is the only thing Endless Wayfare keeps, so losing this file loses the mode
    /// entirely. Everything else about a round — its board, endpoints, tiles and target — is derived
    /// from the seed and the round number, which is why there is so little to store.
    /// </remarks>
    public class EndlessRunStoreTests
    {
        private string _directory;
        private EndlessRunStore _store;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pathweaver-endless-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
            _store = new EndlessRunStore(_directory);
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
        public void A_run_with_no_file_starts_at_round_one()
        {
            var run = _store.Load();

            Assert.That(run.Round, Is.EqualTo(1));
            Assert.That(run.Seed, Is.Not.EqualTo(0UL));
        }

        [Test]
        public void A_saved_run_comes_back_on_the_round_it_reached()
        {
            // Arrange
            var run = EndlessRun.Start(seed: 4242UL).Cleared().Cleared();

            // Act
            _store.Save(run);
            var restored = _store.Load();

            // Assert
            Assert.That(restored.Round, Is.EqualTo(3));
            Assert.That(restored.BestRound, Is.EqualTo(3));
            Assert.That(restored.Seed, Is.EqualTo(4242UL));
        }

        [Test]
        public void The_round_that_comes_back_generates_the_same_board()
        {
            // What persistence is actually for: the player must be handed the board they were
            // looking at, not another board of the same size.
            // Arrange
            var run = EndlessRun.Start(seed: 4242UL).Cleared();
            _store.Save(run);

            // Act
            var restored = _store.Load();

            // Assert
            Assert.That(restored.CurrentRound().Level.Id, Is.EqualTo(run.CurrentRound().Level.Id));
            CollectionAssert.AreEqual(run.CurrentRound().Level.Shape, restored.CurrentRound().Level.Shape);
            CollectionAssert.AreEqual(
                run.CurrentRound().Level.Endpoints, restored.CurrentRound().Level.Endpoints);
        }

        [Test]
        public void A_damaged_file_costs_the_run_rather_than_the_mode()
        {
            // Arrange
            File.WriteAllText(_store.Path, "this is not a run");

            // Act
            var run = _store.Load();

            // Assert
            Assert.That(run.Round, Is.EqualTo(1));
        }

        [Test]
        public void Saving_twice_leaves_one_file_and_no_temporary()
        {
            // The write goes via a temporary file so a kill cannot leave a half-written run. That
            // temporary must not survive the write, or the next launch reads a directory of debris.
            // Act
            _store.Save(EndlessRun.Start(seed: 1UL));
            _store.Save(EndlessRun.Start(seed: 2UL));

            // Assert
            var files = Directory.GetFiles(_directory);
            Assert.That(files.Length, Is.EqualTo(1));
            Assert.That(Path.GetFileName(files[0]), Is.EqualTo("endless-run.txt"));
        }
    }
}
