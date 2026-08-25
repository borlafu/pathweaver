using System;
using System.IO;
using Pathweaver.Core.Campaign;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Stores which levels have been cleared.
    /// </summary>
    /// <remarks>
    /// A separate file from a run in progress, and deliberately so: restarting a level wipes the
    /// run but must never cost the player a level they have already finished.
    /// </remarks>
    internal sealed class CampaignProgressStore
    {
        private const string FileName = "campaign-progress.txt";
        private const string TemporaryExtension = ".writing";

        private readonly string _directory;

        internal CampaignProgressStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        internal static CampaignProgressStore ForPlayer()
            => new CampaignProgressStore(Application.persistentDataPath);

        internal string Path => System.IO.Path.Combine(_directory, FileName);

        /// <summary>
        /// Reads progress, returning an empty campaign for anything unreadable.
        /// </summary>
        internal CampaignProgress Load()
        {
            try
            {
                return File.Exists(Path)
                    ? CampaignProgressFormat.Read(File.ReadAllText(Path))
                    : CampaignProgress.Empty;
            }
            catch (IOException error)
            {
                Debug.LogWarning($"[progress] could not be read: {error.Message}");
                return CampaignProgress.Empty;
            }
        }

        /// <summary>
        /// Writes progress, via a temporary file so a kill cannot leave it half written.
        /// </summary>
        internal void Save(CampaignProgress progress)
        {
            try
            {
                Directory.CreateDirectory(_directory);

                var temporary = Path + TemporaryExtension;
                File.WriteAllText(temporary, CampaignProgressFormat.Write(progress));

                if (File.Exists(Path))
                {
                    File.Replace(temporary, Path, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temporary, Path);
                }
            }
            catch (IOException error)
            {
                // Losing a level's credit is a disappointment; crashing on the way out of a level
                // is worse.
                Debug.LogWarning($"[progress] could not be written: {error.Message}");
            }
        }

        /// <summary>
        /// Forgets everything, as if the game had never been played.
        /// </summary>
        /// <remarks>
        /// Removes every level the player had cleared, and the Pivot Tokens they were carrying. Only the reset in
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
                Debug.LogWarning($"[progress] could not be deleted: {error.Message}");
            }
        }
    }
}
