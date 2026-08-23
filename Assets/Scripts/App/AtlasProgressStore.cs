using System;
using System.IO;
using Pathweaver.Core.Atlas;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Stores Star Essence and which atlas nodes it has bought.
    /// </summary>
    /// <remarks>
    /// Its own file, alongside campaign progress and the endless run, for the same reason those two
    /// are separate: a damaged or deleted file should cost one thing rather than everything.
    /// </remarks>
    internal sealed class AtlasProgressStore
    {
        private const string FileName = "atlas-progress.txt";
        private const string TemporaryExtension = ".writing";

        private readonly string _directory;

        internal AtlasProgressStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        internal static AtlasProgressStore ForPlayer()
            => new AtlasProgressStore(Application.persistentDataPath);

        internal string Path => System.IO.Path.Combine(_directory, FileName);

        /// <summary>
        /// Reads progress, returning an empty atlas for anything unreadable.
        /// </summary>
        internal AtlasProgress Load()
        {
            try
            {
                return File.Exists(Path)
                    ? AtlasProgressFormat.Read(File.ReadAllText(Path))
                    : AtlasProgress.Empty;
            }
            catch (IOException error)
            {
                Debug.LogWarning($"[atlas] progress could not be read: {error.Message}");
                return AtlasProgress.Empty;
            }
        }

        /// <summary>
        /// Writes progress, via a temporary file so a kill cannot leave it half written.
        /// </summary>
        internal void Save(AtlasProgress progress)
        {
            try
            {
                Directory.CreateDirectory(_directory);

                var temporary = Path + TemporaryExtension;
                File.WriteAllText(temporary, AtlasProgressFormat.Write(progress));

                if (File.Exists(Path))
                {
                    File.Delete(Path);
                }

                File.Move(temporary, Path);
            }
            catch (IOException error)
            {
                Debug.LogWarning($"[atlas] progress could not be saved: {error.Message}");
            }
        }
    }
}
