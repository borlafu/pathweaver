using System;
using System.IO;
using Pathweaver.Core.Endless;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Stores which endless round the player is on, and how far they have ever got.
    /// </summary>
    /// <remarks>
    /// A separate file from campaign progress and from a run in progress, for the same reason those
    /// two are separate: losing one must not cost the others.
    /// </remarks>
    internal sealed class EndlessRunStore
    {
        private const string FileName = "endless-run.txt";
        private const string TemporaryExtension = ".writing";

        private readonly string _directory;

        internal EndlessRunStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        internal static EndlessRunStore ForPlayer()
            => new EndlessRunStore(Application.persistentDataPath);

        internal string Path => System.IO.Path.Combine(_directory, FileName);

        /// <summary>
        /// A seed for a run that has none yet.
        /// </summary>
        /// <remarks>
        /// The clock is read here rather than in the simulation, which may not read one at all: a
        /// generated board must be reproducible from its seed, and a seed that came from a clock is
        /// still a number once it has been written down. Ticks are used rather than a random number
        /// because the simulation deliberately offers no source of randomness that is not seeded.
        /// </remarks>
        internal static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;

        /// <summary>
        /// Reads the run, returning a fresh one for anything unreadable.
        /// </summary>
        internal EndlessRun Load()
        {
            try
            {
                return File.Exists(Path)
                    ? EndlessRunFormat.Read(File.ReadAllText(Path), NewSeed())
                    : EndlessRun.Start(NewSeed());
            }
            catch (IOException error)
            {
                Debug.LogWarning($"[endless] the run could not be read: {error.Message}");
                return EndlessRun.Start(NewSeed());
            }
        }

        /// <summary>
        /// Writes the run, via a temporary file so a kill cannot leave it half written.
        /// </summary>
        internal void Save(EndlessRun run)
        {
            try
            {
                Directory.CreateDirectory(_directory);

                var temporary = Path + TemporaryExtension;
                File.WriteAllText(temporary, EndlessRunFormat.Write(run));

                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }

                File.Move(temporary, Path);
            }
            catch (IOException error)
            {
                Debug.LogWarning($"[endless] the run could not be saved: {error.Message}");
            }
        }

        /// <summary>
        /// Forgets everything, as if the game had never been played.
        /// </summary>
        /// <remarks>
        /// Removes the round the player had reached and the best they had ever reached. Only the reset in
        /// <see cref="ProgressReset"/> calls this, so there is one place that knows what "all
        /// progress" means and no single screen can wipe half of it.
        /// </remarks>
        internal void Delete()
        {
            try
            {
                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }
            }
            catch (IOException error)
            {
                Debug.LogWarning($"[endless] could not be deleted: {error.Message}");
            }
        }
    }
}
