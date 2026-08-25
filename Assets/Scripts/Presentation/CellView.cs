using Pathweaver.Core.Flow;
using Pathweaver.Core.Hex;
using Pathweaver.Core.Tiles;
using UnityEngine;

namespace Pathweaver.Game.Presentation
{
    /// <summary>
    /// Draws one cell: its background, and the conduit or endpoint on it.
    /// </summary>
    /// <remarks>
    /// Presentation only. A cell holds no game rule and decides nothing — it is told
    /// what to show. Keeping that boundary is what lets the simulation stay testable
    /// without Unity.
    /// </remarks>
    internal sealed class CellView : MonoBehaviour
    {
        private TileVisual _visual;
        private Transform _pulse;
        private Material _pulseMaterial;
        private Color _pulseResting;
        private Color _pulseLit;

        internal HexCoord Coordinate { get; private set; }

        /// <summary>Whether this cell carries an endpoint's breathing ring.</summary>
        internal bool HasPulse => _pulse != null;

        internal void Initialise(
            HexCoord coordinate, Mesh hexMesh, Mesh spokeMesh, Material material, BoardTheme theme)
        {
            Coordinate = coordinate;
            transform.localPosition = HexMetrics.ToWorld(coordinate);

            _visual = new GameObject("Visual").AddComponent<TileVisual>();
            _visual.transform.SetParent(transform, worldPositionStays: false);
            _visual.Initialise(hexMesh, spokeMesh, material, theme);

            ShowEmpty();
        }

        /// <summary>
        /// Gives this cell the ring that breathes in or out, once, for good.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A sibling of the tile's visual rather than a child of it. Every display state rebuilds the
        /// visual's spokes and motif from scratch, so anything living inside it is churned on every state
        /// change; the ring belongs to the cell's <em>role</em>, which never changes, not to its drawing.
        /// </para>
        /// <para>
        /// It sits between the background and the spokes, and it is a ring rather than a disc, so it
        /// travels across the cell without ever hiding the resource mark in the middle.
        /// </para>
        /// </remarks>
        internal void AttachPulse(FlowEndpoint endpoint, Mesh ringMesh, Material material)
        {
            if (_pulse != null || ringMesh == null || material == null)
            {
                return;
            }

            var isSpring = endpoint.Role == EndpointRole.Spring;
            _pulseResting = isSpring ? BoardPalette.Spring : BoardPalette.Hub;

            // Lit is the cell's own colour brightened rather than a colour of its own: the ring has to
            // dissolve into the cell exactly, because an opaque material has no alpha to fade.
            _pulseLit = Color.Lerp(_pulseResting, BoardPalette.HarvestFlash, 0.7f);

            var pulse = new GameObject("Pulse");
            pulse.transform.SetParent(transform, worldPositionStays: false);
            pulse.transform.localPosition = new Vector3(0f, 0f, -0.005f);
            pulse.transform.localScale = Vector3.one * EndpointPulse.RestingScaleFor(endpoint.Role);

            pulse.AddComponent<MeshFilter>().sharedMesh = ringMesh;

            var renderer = pulse.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.material.color = _pulseResting;

            _pulse = pulse.transform;
            _pulseMaterial = renderer.material;
            PulseRole = endpoint.Role;
        }

        /// <summary>Which way this cell's ring travels.</summary>
        internal EndpointRole PulseRole { get; private set; }

        /// <summary>
        /// Poses the ring at a point in its cycle.
        /// </summary>
        /// <remarks>
        /// Scale and colour only — no rebuild, no allocation — so this is safe to call every frame.
        /// </remarks>
        internal void SetPulse(float scale, float fade)
        {
            if (_pulse == null)
            {
                return;
            }

            _pulse.localScale = Vector3.one * scale;
            _pulseMaterial.color = Color.Lerp(_pulseLit, _pulseResting, fade);
        }

        /// <summary>
        /// Settles the ring where it still says which role this cell plays, for reduced motion.
        /// </summary>
        /// <remarks>
        /// Not hidden. Now that the edge marks are gone, the ring is the only thing separating a source
        /// from a destination other than colour — so switching motion off must leave a silhouette
        /// behind, not take the distinction away from the players most likely to need it. A spring rests
        /// open at its rim; a hub rests closed at its centre.
        /// </remarks>
        internal void RestPulse()
        {
            if (_pulse == null)
            {
                return;
            }

            _pulse.localScale = Vector3.one * EndpointPulse.RestingScaleFor(PulseRole);
            _pulseMaterial.color = _pulseLit;
        }

        internal void ShowEmpty()
        {
            _visual.SetBackground(BoardPalette.EmptyCell);
            _visual.ClearSpokes();
            _visual.ClearMotif();
        }

        /// <summary>
        /// Shows an empty cell the held tile could legally occupy.
        /// </summary>
        internal void ShowAvailable()
        {
            _visual.SetBackground(BoardPalette.LegalCell);
            _visual.ClearSpokes();
            _visual.ClearMotif();
        }

        internal void ShowEndpoint(FlowEndpoint endpoint)
        {
            // No edge marks. A spring used to be starred on all six edges and a hub barred across,
            // which was the only non-colour signal of the role — and now the ring says it better: a
            // spring's travels outward, a hub's inward. Two signals for one fact left the cell busy,
            // and the marks were the weaker of the two.
            //
            // What remains carries everything: the background says the role, the resource motif in the
            // middle says the kind by shape as well as colour, and the ring says the role by direction.
            var isSpring = endpoint.Role == EndpointRole.Spring;

            _visual.SetBackground(isSpring ? BoardPalette.Spring : BoardPalette.Hub);
            _visual.UseResourceArt(null);
            _visual.ClearSpokes();
            _visual.ShowResource(endpoint.Kind, BoardPalette.ForKind(endpoint.Kind));
        }

        internal void ShowConduit(ConduitTile tile)
        {
            _visual.SetBackground(BoardPalette.CellOutline);
            _visual.UseResourceArt(tile.Kind);
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));
            _visual.ShowResource(tile.Kind, BoardPalette.ForKind(tile.Kind));
        }

        /// <summary>
        /// Draws a conduit as part of a route that has just harvested.
        /// </summary>
        /// <remarks>
        /// The whole path lights up rather than the last tile placed, because what paid out is
        /// the route, and a player needs to see which one.
        /// </remarks>
        internal void ShowHarvestedConduit(ConduitTile tile)
        {
            _visual.SetBackground(BoardPalette.HarvestFlash);
            _visual.UseResourceArt(tile.Kind);
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));
            _visual.ShowResource(tile.Kind, BoardPalette.ForKind(tile.Kind));
        }

        /// <summary>
        /// Draws a conduit a Pivot Token could act on.
        /// </summary>
        /// <remarks>
        /// Backed in the token's own colour, so an armed token and the cells it can reach are
        /// recognisably the same thing. Without a mark, arming a token would change nothing on
        /// screen and the mode would be invisible.
        /// </remarks>
        internal void ShowPivotable(ConduitTile tile)
        {
            _visual.SetBackground(BoardPalette.PivotTarget);
            _visual.UseResourceArt(tile.Kind);
            _visual.ShowEdges(tile.Edges, BoardPalette.ForKind(tile.Kind));
            _visual.ShowResource(tile.Kind, BoardPalette.ForKind(tile.Kind));
        }
    }
}
