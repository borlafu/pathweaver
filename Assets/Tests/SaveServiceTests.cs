using System;
using System.IO;
using NUnit.Framework;
using Pathweaver.Core.Levels;
using Pathweaver.Core.State;
using Pathweaver.Game.App;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Suspend and resume, which PRD section 2.1 treats as a core promise rather than a
    /// recovery path.
    /// </summary>
    public class SaveServiceTests
    {
        private const string LevelId = "test-level";

        // A verbatim string rather than a raw literal: Unity compiles C# 9, so the
        // triple-quoted form the Core tests use is unavailable here.
        private const string LevelText =
            "id: test-level\n" +
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

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pathweaver-save-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
            _saves = new SaveService(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static GameState NewGame() => LevelLoader.Parse(LevelText).CreateGame(seed: 42UL);

        [Test]
        public void There_is_nothing_to_resume_before_anything_is_saved()
        {
            Assert.That(_saves.HasSave(LevelId), Is.False);
            Assert.That(_saves.Load(LevelId), Is.Null);
        }

        [Test]
        public void A_saved_run_comes_back()
        {
            var state = NewGame();
            state = GameEngine.Apply(state, new PlaceTile(new Pathweaver.Core.Hex.HexCoord(-1, 0), 0));

            _saves.Save(LevelId, state);
            var resumed = _saves.Load(LevelId);

            Assert.That(resumed, Is.Not.Null);
            Assert.That(resumed.Score, Is.EqualTo(state.Score));
            Assert.That(resumed.HeldTile, Is.EqualTo(state.HeldTile));
            Assert.That(resumed.Board.OccupiedCount, Is.EqualTo(state.Board.OccupiedCount));
        }

        [Test]
        public void Saving_twice_keeps_the_newer_run()
        {
            var first = NewGame();
            _saves.Save(LevelId, first);

            var second = GameEngine.Apply(first, new PlaceTile(new Pathweaver.Core.Hex.HexCoord(-1, 0), 0));
            _saves.Save(LevelId, second);

            Assert.That(_saves.Load(LevelId).Board.OccupiedCount, Is.EqualTo(1));
        }

        [Test]
        public void Writing_leaves_no_temporary_file_behind()
        {
            // The temporary file is how the write stays atomic. Leaking it would fill
            // storage over a few hundred sessions.
            _saves.Save(LevelId, NewGame());

            Assert.That(Directory.GetFiles(_directory, "*.writing"), Is.Empty);
        }

        [Test]
        public void Each_level_keeps_its_own_run()
        {
            // Resuming into the wrong board would be worse than losing the run.
            _saves.Save(LevelId, NewGame());

            Assert.That(_saves.HasSave("some-other-level"), Is.False);
            Assert.That(_saves.Load("some-other-level"), Is.Null);
        }

        [Test]
        public void A_corrupt_save_yields_a_fresh_board_rather_than_a_crash()
        {
            File.WriteAllBytes(_saves.PathFor(LevelId), new byte[] { 9, 9, 9, 9, 9, 9, 9, 9, 9 });

            Assert.That(_saves.Load(LevelId), Is.Null, "A corrupt save must not load.");
        }

        [Test]
        public void A_corrupt_save_is_moved_aside_rather_than_retried_forever()
        {
            File.WriteAllBytes(_saves.PathFor(LevelId), new byte[] { 9, 9, 9, 9, 9, 9, 9, 9, 9 });

            _saves.Load(LevelId);

            Assert.That(File.Exists(_saves.PathFor(LevelId)), Is.False, "The bad save should be gone.");
            Assert.That(
                Directory.GetFiles(_directory, "*.corrupt"),
                Is.Not.Empty,
                "It should be kept for diagnosis rather than deleted.");
        }

        [Test]
        public void A_truncated_save_is_treated_as_corrupt()
        {
            // What a process killed mid-write would leave, if the write were not atomic.
            _saves.Save(LevelId, NewGame());

            var path = _saves.PathFor(LevelId);
            var bytes = File.ReadAllBytes(path);
            var truncated = new byte[bytes.Length / 2];
            Array.Copy(bytes, truncated, truncated.Length);
            File.WriteAllBytes(path, truncated);

            Assert.That(_saves.Load(LevelId), Is.Null);
        }

        [Test]
        public void Deleting_a_run_removes_it()
        {
            _saves.Save(LevelId, NewGame());
            _saves.Delete(LevelId);

            Assert.That(_saves.HasSave(LevelId), Is.False);
        }

        [Test]
        public void Deleting_a_run_that_does_not_exist_is_harmless()
        {
            Assert.DoesNotThrow(() => _saves.Delete(LevelId));
        }

        [Test]
        public void Resuming_continues_the_same_tile_order()
        {
            // The bag's generator travels with the save, so a resumed run must draw what
            // the original would have drawn. Otherwise a suspended Daily Expedition
            // diverges from everyone else's.
            var original = NewGame();
            _saves.Save(LevelId, original);

            var resumed = _saves.Load(LevelId);

            var coordinate = new Pathweaver.Core.Hex.HexCoord(-1, 0);
            var afterOriginal = GameEngine.Apply(original, new PlaceTile(coordinate, 0));
            var afterResumed = GameEngine.Apply(resumed, new PlaceTile(coordinate, 0));

            Assert.That(afterResumed.HeldTile, Is.EqualTo(afterOriginal.HeldTile));
        }
    }
}
