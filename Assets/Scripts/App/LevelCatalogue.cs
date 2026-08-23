using Pathweaver.Core.Levels;
using UnityEngine;

namespace Pathweaver.Game.App
{
    /// <summary>
    /// Loads authored levels at runtime.
    /// </summary>
    /// <remarks>
    /// Levels live in <c>Resources</c> rather than <c>StreamingAssets</c> because
    /// reading them has to be synchronous and work identically on Android, where
    /// StreamingAssets sits inside the APK and needs an asynchronous request. A
    /// blocking web request during boot would eat into the 1.5 second budget.
    /// <para>
    /// The files under <c>levels/</c> are the source of truth;
    /// <c>scripts/build-core.sh</c> copies them in.
    /// </para>
    /// </remarks>
    internal static class LevelCatalogue
    {
        private const string ResourceFolder = "Levels";

        internal static LevelDefinition Load(string id)
        {
            var asset = Resources.Load<TextAsset>($"{ResourceFolder}/{id}");
            if (asset == null)
            {
                throw new LevelFormatException(
                    $"No level resource named \"{id}\". Run scripts/build-core.sh to copy levels in.");
            }

            return LevelLoader.Parse(asset.text);
        }
    }
}
