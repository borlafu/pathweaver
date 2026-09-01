using System.Collections.Generic;
using System.IO;
using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;
using Pathweaver.Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Pathweaver.EditorTools
{
    /// <summary>
    /// Renders the images the Play Store listing needs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Drawn from the game's own cells, meshes and palette rather than mocked up in an image editor,
    /// so the listing cannot promise something the game does not look like. It also means the art
    /// updates itself: when real tiles replace the generated geometry, these images change with them.
    /// </para>
    /// <para>
    /// Play's rules for both files: no transparency, exact pixel sizes, and PNG is accepted for each.
    /// The icon is masked into whatever shape the launcher wants, so it is drawn full-bleed with the
    /// subject well inside the edges.
    /// </para>
    /// </remarks>
    internal static class StoreArt
    {
        /// <summary>The icon size Play requires, exactly.</summary>
        private const int IconSize = 512;

        private const int FeatureWidth = 1024;
        private const int FeatureHeight = 500;

        /// <summary>
        /// The background both images sit on, matching the game's own.
        /// </summary>
        /// <remarks>
        /// A shade lighter than the in-game background. On a store page the surrounding chrome is
        /// white, and the game's near-black read as a hole in the page rather than as a screen.
        /// </remarks>
        private static readonly Color Backdrop = new Color(0.11f, 0.13f, 0.17f);

        /// <summary>
        /// Writes the icon and the feature graphic.
        /// </summary>
        /// <remarks>
        /// Run headless:
        /// <c>unity -batchmode -quit -projectPath . -executeMethod
        /// Pathweaver.EditorTools.StoreArt.Capture -logFile /tmp/unity.log</c>
        /// </remarks>
        internal static void Capture()
        {
            var folder = ProjectBootstrap.ArgumentOr("-storeArtFolder", "Artifacts/store");

            CaptureIcon(Path.Combine(folder, "icon-512.png"));
            CaptureFeatureGraphic(Path.Combine(folder, "feature-1024x500.png"));
        }

        /// <summary>
        /// A single conduit turning a corner: the one thing every board is made of.
        /// </summary>
        /// <remarks>
        /// One tile rather than a whole board, because a launcher shows this at about forty pixels
        /// and a board at that size is a grey smudge. A bend rather than a straight, so the shape
        /// says "route" rather than "pipe".
        /// </remarks>
        private static void CaptureIcon(string outputPath)
        {
            // Room around the tile. At 0.62 the conduit's arms ran into the edge of the image, and
            // Play masks the icon into a rounded shape that eats the corners as well.
            var (camera, board) = NewScene(orthographicSize: 0.78f, aspect: 1f);

            var cells = new[] { HexCoord.Zero };
            var tiles = new Dictionary<HexCoord, ConduitTile>
            {
                [HexCoord.Zero] = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 2)),
            };

            Draw(board, cells, tiles, endpoints: new FlowEndpoint[0]);

            Render(camera, outputPath, IconSize, IconSize);
        }

        /// <summary>
        /// A finished route: spring, four conduits, hub.
        /// </summary>
        /// <remarks>
        /// The whole game in one line. It is also the shape of the reward: a route of four conduits
        /// is exactly what earns a Pivot Token, so the graphic shows a play worth making rather than
        /// an arbitrary arrangement.
        /// </remarks>
        private static void CaptureFeatureGraphic(string outputPath)
        {
            // Centred on the route rather than on the origin, and zoomed out far enough to keep a
            // margin: Play crops the feature graphic differently in different placements, so a
            // subject touching the edge is a subject that will be cut in half somewhere.
            var (camera, board) = NewScene(
                orthographicSize: 1.6f,
                aspect: (float)FeatureWidth / FeatureHeight,
                centreX: -0.43f);

            // A row across the middle, with a cell above and below each end so the graphic reads as
            // part of a board rather than as a diagram floating in space.
            var route = new[]
            {
                new HexCoord(-3, 0),
                new HexCoord(-2, 0),
                new HexCoord(-1, 0),
                new HexCoord(0, 0),
                new HexCoord(1, 0),
                new HexCoord(2, 0),
            };

            var cells = new List<HexCoord>(route)
            {
                new HexCoord(-2, -1),
                new HexCoord(0, -1),
                new HexCoord(2, -1),
                new HexCoord(-3, 1),
                new HexCoord(-1, 1),
                new HexCoord(1, 1),
            };

            var straight = new ConduitTile(ResourceKind.Water, EdgeMask.FromDirections(0, 3));
            var tiles = new Dictionary<HexCoord, ConduitTile>
            {
                [route[1]] = straight,
                [route[2]] = straight,
                [route[3]] = straight,
                [route[4]] = straight,
            };

            var endpoints = new[]
            {
                FlowEndpoint.Spring(route[0], ResourceKind.Water),
                FlowEndpoint.Hub(route[5], ResourceKind.Water),
            };

            Draw(board, cells, tiles, endpoints);

            Render(camera, outputPath, FeatureWidth, FeatureHeight);
        }

        /// <summary>
        /// Half the drawn cells' height in board coordinates, before the lean foreshortens it.
        /// </summary>
        /// <remarks>
        /// How far back the board has to sit depends on how tall it is, because the lean swings its near
        /// rim toward the viewer in proportion.
        /// </remarks>
        private static float LocalHalfHeight(IReadOnlyList<HexCoord> cells)
        {
            var lowest = float.MaxValue;
            var highest = float.MinValue;

            foreach (var coordinate in cells)
            {
                var y = HexMetrics.ToWorld(coordinate).y;
                lowest = Mathf.Min(lowest, y);
                highest = Mathf.Max(highest, y);
            }

            return cells.Count == 0 ? 0f : (highest - lowest) * 0.5f;
        }

        /// <summary>
        /// Builds an empty scene with a camera and a board view, and returns both.
        /// </summary>
        private static (Camera Camera, BoardView Board) NewScene(
            float orthographicSize, float aspect, float centreX = 0f)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(centreX, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.aspect = aspect;
            camera.backgroundColor = Backdrop;
            camera.clearFlags = CameraClearFlags.SolidColor;

            var board = new GameObject("Board").AddComponent<BoardView>();
            ProjectBootstrap.WireTileMaterial(board);

            return (camera, board);
        }

        /// <summary>
        /// Draws cells straight onto the board view, without a game state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A state would have to be a legal, reachable position, and a promotional image does not
        /// need one — it needs the tidiest arrangement of the game's own cells. Drawing the cells
        /// directly keeps the pixels honest while leaving the rules out of it.
        /// </para>
        /// <para>
        /// The lean has to be applied here for the same reason. <c>BoardView.Build</c> is what normally
        /// sets it, and this path never calls it — so the icon and the feature graphic went on showing a
        /// flat board for as long as the board had been leaning, which is exactly the thing this file
        /// exists to prevent.
        /// </para>
        /// </remarks>
        private static void Draw(
            BoardView board,
            IReadOnlyList<HexCoord> cells,
            IReadOnlyDictionary<HexCoord, ConduitTile> tiles,
            IReadOnlyList<FlowEndpoint> endpoints)
        {
            board.transform.SetPositionAndRotation(
                BoardTilt.PositionFor(LocalHalfHeight(cells)), BoardTilt.Rotation);

            var endpointsByCell = new Dictionary<HexCoord, FlowEndpoint>();
            foreach (var endpoint in endpoints)
            {
                endpointsByCell[endpoint.Coordinate] = endpoint;
            }

            foreach (var coordinate in cells)
            {
                var cell = board.CreateCell(coordinate);

                if (endpointsByCell.TryGetValue(coordinate, out var endpoint))
                {
                    cell.ShowEndpoint(endpoint);
                    continue;
                }

                if (tiles.TryGetValue(coordinate, out var tile))
                {
                    cell.ShowConduit(tile);
                    continue;
                }

                cell.ShowEmpty();
            }
        }

        private static void Render(Camera camera, string outputPath, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            // RGB24 rather than RGBA32: Play rejects an icon with an alpha channel, and the simplest
            // way not to have one is not to read one.
            var image = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, image.EncodeToPNG());

            Debug.Log($"[store] wrote {outputPath} at {width}x{height}");
        }
    }
}
