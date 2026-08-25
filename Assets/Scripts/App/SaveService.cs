using System;
using System.IO;
using Pathweaver.Core.Save;
using Pathweaver.Core.State;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Stores and restores an in-progress run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Takes its directory rather than reaching for
    /// <c>Application.persistentDataPath</c> itself, so the whole thing can be exercised
    /// against a temporary folder in tests.
    /// </para>
    /// <para>
    /// One file per level, named after the level. A run belongs to the level it started
    /// in, and resuming into the wrong board would be worse than losing the run.
    /// </para>
    /// </remarks>
    internal sealed class SaveService
    {
        private const string Extension = ".pwsave";
        private const string TemporaryExtension = ".writing";
        private const string QuarantineExtension = ".corrupt";

        private readonly string _directory;

        internal SaveService(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        /// <summary>The service the game uses, writing where the platform allows.</summary>
        internal static SaveService ForPlayer() => new SaveService(Application.persistentDataPath);

        internal string PathFor(string levelId) => Path.Combine(_directory, levelId + Extension);

        internal bool HasSave(string levelId) => File.Exists(PathFor(levelId));

        /// <summary>
        /// Writes a run, replacing any previous one.
        /// </summary>
        /// <remarks>
        /// Written to a temporary file and then moved into place. A kill during a direct
        /// write would leave a half-written save that parses as corrupt, costing the
        /// player their run for the sake of one fewer file operation. PRD section 2.1
        /// expects suspend and resume to be dependable during a three-minute transit
        /// session, and a process can be killed at any moment on Android.
        /// </remarks>
        internal void Save(string levelId, GameState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Directory.CreateDirectory(_directory);

            var destination = PathFor(levelId);
            var temporary = destination + TemporaryExtension;

            File.WriteAllBytes(temporary, SaveGame.Write(state));

            if (File.Exists(destination))
            {
                File.Replace(temporary, destination, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporary, destination);
            }
        }

        /// <summary>
        /// Reads a run, or returns null when there is nothing usable to read.
        /// </summary>
        /// <remarks>
        /// A save that cannot be read is moved aside rather than deleted or retried. The
        /// player gets a fresh board instead of a crash loop, and the file survives for
        /// diagnosis — silently deleting the evidence would make the bug that produced it
        /// unfindable.
        /// </remarks>
        internal GameState Load(string levelId)
        {
            var path = PathFor(levelId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return SaveGame.Read(File.ReadAllBytes(path));
            }
            catch (Exception error) when (error is SaveFormatException || error is IOException)
            {
                Quarantine(path, error);
                return null;
            }
        }

        internal void Delete(string levelId)
        {
            var path = PathFor(levelId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Deletes every board this player has in progress.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Written by pattern rather than by asking the campaign for its level identifiers, because a
        /// generated endless round is not in any catalogue and its save would otherwise survive a
        /// wipe — the player would ask for a fresh start and be dropped back onto the board they
        /// wanted rid of.
        /// </para>
        /// <para>
        /// Quarantined and half-written files go too. They are diagnostics, and a player asking for
        /// everything to be forgotten has not asked to keep the evidence.
        /// </para>
        /// </remarks>
        /// <returns>How many files were removed.</returns>
        internal int DeleteAll()
        {
            if (!Directory.Exists(_directory))
            {
                return 0;
            }

            var removed = 0;

            foreach (var pattern in new[]
                     {
                         "*" + Extension,
                         "*" + Extension + TemporaryExtension,
                         "*" + Extension + QuarantineExtension,
                     })
            {
                foreach (var path in Directory.GetFiles(_directory, pattern))
                {
                    try
                    {
                        File.Delete(path);
                        removed++;
                    }
                    catch (IOException error)
                    {
                        // One stubborn file must not stop the rest being cleared: a wipe that gave up
                        // half way would leave the player worse off than not asking for one.
                        Debug.LogWarning($"[save] could not delete {Path.GetFileName(path)}: {error.Message}");
                    }
                }
            }

            return removed;
        }

        private static void Quarantine(string path, Exception error)
        {
            Debug.LogWarning($"[save] {Path.GetFileName(path)} is unusable: {error.Message}");

            var quarantined = path + QuarantineExtension;

            try
            {
                if (File.Exists(quarantined))
                {
                    File.Delete(quarantined);
                }

                File.Move(path, quarantined);
            }
            catch (IOException moveFailure)
            {
                // If it cannot even be moved aside, deleting it is better than looping on
                // it every launch.
                Debug.LogWarning($"[save] could not quarantine: {moveFailure.Message}");
                File.Delete(path);
            }
        }
    }
}
