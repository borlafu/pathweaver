using System.Collections.Generic;
using System.Linq;
using Pathweaver.Core.Atlas;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Loads the atlas packs that ship with the build.
    /// </summary>
    /// <remarks>
    /// Every pack in the folder is loaded and combined, so a future biome pack is a new file under
    /// <c>atlas/</c> and nothing else — no list to update here, and no existing node to edit. The
    /// files are the source of truth; <c>scripts/build-core.sh</c> copies them into Resources.
    /// </remarks>
    internal static class AtlasCatalogue
    {
        private const string ResourceFolder = "Atlas";

        internal static AtlasMap Load()
        {
            var packs = Resources.LoadAll<TextAsset>(ResourceFolder)
                .OrderBy(asset => asset.name, System.StringComparer.Ordinal)
                .Select(asset => AtlasLoader.Parse(asset.text))
                .ToArray();

            if (packs.Length == 0)
            {
                Debug.LogWarning(
                    "[atlas] no packs found. Run scripts/build-core.sh to copy them in.");

                return AtlasMap.Combine(System.Array.Empty<AtlasMap>());
            }

            return AtlasMap.Combine(packs);
        }

        /// <summary>The pack identifiers found, for diagnostics.</summary>
        internal static IReadOnlyList<string> PackNames()
            => Resources.LoadAll<TextAsset>(ResourceFolder)
                .Select(asset => asset.name)
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToList();
    }
}
