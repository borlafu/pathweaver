using Pathweaver.Core.Hex;
using Pathweaver.Core.Levels;
using UnityEngine;

namespace Pathweaver.Game
{
    /// <summary>
    /// Proves the simulation assembly is reachable from Unity.
    /// </summary>
    /// <remarks>
    /// Temporary scaffolding for the first Unity milestone. It exists so a broken
    /// plugin import fails at compile time rather than being discovered later, when
    /// the cause is buried under gameplay code. Delete once GridView renders a real
    /// board.
    /// </remarks>
    internal static class CoreLinkCheck
    {
        internal static string Describe()
        {
            var coordinate = new HexCoord(2, -1);
            var neighbour = coordinate.Neighbour(0);

            return $"{coordinate} -> {neighbour}, distance {coordinate.DistanceTo(neighbour)}";
        }

        internal static string DescribeLevel(string levelText)
        {
            var level = LevelLoader.Parse(levelText);
            return $"{level.Id}: {level.Shape.Count} cells, target {level.TargetScore}";
        }
    }
}
