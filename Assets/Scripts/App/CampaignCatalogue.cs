using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Campaign;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Discovers the levels that ship with the build.
    /// </summary>
    /// <remarks>
    /// Read from the resources folder rather than from a hand-maintained list, so adding a level
    /// file is the only step needed to add a level. A list would be a second place to update and
    /// therefore a place to forget.
    /// <para>
    /// Play order is the identifier's order, which is why level files are named with a zero-padded
    /// number.
    /// </para>
    /// </remarks>
    internal static class CampaignCatalogue
    {
        private const string ResourceFolder = "Levels";

        internal static IReadOnlyList<string> LevelIds()
            => Resources.LoadAll<TextAsset>(ResourceFolder)
                .Select(asset => asset.name)
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToList();

        internal static Campaign Load() => Campaign.Of(LevelIds());
    }
}
