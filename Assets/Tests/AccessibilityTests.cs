using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pathweaver.Core.Tiles;
using Pathweaver.Game.Presentation;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// Guards the rule that colour is never the only way to tell two things apart.
    /// </summary>
    /// <remarks>
    /// Four resources flow through visually identical conduits and never interconnect, so a
    /// player who cannot separate two kinds sees a board that appears to break its own rules.
    /// These are the checks that keep a future palette change from quietly reintroducing that.
    /// </remarks>
    public class AccessibilityTests
    {
        private static IEnumerable<ResourceKind> AllKinds => (ResourceKind[])System.Enum.GetValues(typeof(ResourceKind));

        [Test]
        public void Every_resource_has_a_motif()
        {
            foreach (var kind in AllKinds)
            {
                Assert.DoesNotThrow(
                    () => ResourceMotif.Create(kind, 0.5f),
                    $"{kind} has no motif, so it would be identified by colour alone.");
            }
        }

        [Test]
        public void No_two_resources_share_a_silhouette()
        {
            // The whole point: if two kinds looked the same in greyscale, colour would be
            // carrying the information on its own.
            var silhouettes = AllKinds
                .Select(kind => (ResourceMotif.SidesFor(kind), ResourceMotif.RotationFor(kind)))
                .ToList();

            Assert.That(silhouettes.Distinct().Count(), Is.EqualTo(silhouettes.Count));
        }

        [Test]
        public void No_two_resources_share_a_colour()
        {
            var colours = AllKinds.Select(BoardPalette.ForKind).ToList();

            Assert.That(colours.Distinct().Count(), Is.EqualTo(colours.Count));
        }

        [Test]
        public void Resource_colours_differ_in_brightness_as_well_as_hue()
        {
            // Two kinds separated only by hue can be indistinguishable to a colour-blind
            // player. A brightness difference survives that, so it is the property worth
            // asserting rather than merely "the colours are not equal".
            var luminances = AllKinds
                .Select(kind => Luminance(BoardPalette.ForKind(kind)))
                .OrderBy(value => value)
                .ToList();

            for (var index = 1; index < luminances.Count; index++)
            {
                Assert.That(
                    luminances[index] - luminances[index - 1],
                    Is.GreaterThan(0.03f),
                    "Two resources are too close in brightness to separate without colour.");
            }
        }

        [Test]
        public void A_conduit_stands_out_from_an_empty_cell()
        {
            // A player has to see at a glance which cells are built on.
            Assert.That(
                Mathf.Abs(Luminance(BoardPalette.CellOutline) - Luminance(BoardPalette.EmptyCell)),
                Is.GreaterThan(0.02f));
        }

        [Test]
        public void A_legal_cell_stands_out_from_an_ordinary_empty_one()
        {
            // Where a tile may go is the placement rule made visible; if the highlight is
            // subtle, a refused tap reads as the game ignoring the player.
            Assert.That(
                Luminance(BoardPalette.LegalCell) - Luminance(BoardPalette.EmptyCell),
                Is.GreaterThan(0.02f));
        }

        [Test]
        public void A_motif_fits_inside_its_hex_without_touching_the_spokes()
        {
            // If the motif overlapped the conduit it would obscure the very thing a player
            // traces with their eye.
            var mesh = ResourceMotif.Create(ResourceKind.Water, HexMetrics.Size);
            var reach = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.y);

            Assert.That(reach, Is.LessThan(TileVisual.SpokeLength));
            Assert.That(reach, Is.GreaterThan(HexMetrics.Size * 0.15f), "Too small to identify.");
        }

        [Test]
        public void A_spring_and_a_hub_are_told_apart_by_more_than_colour()
        {
            // Their backgrounds differ in hue, which is not enough on its own. The mark
            // pattern differs too, which is what CellView draws.
            Assert.That(
                Mathf.Abs(Luminance(BoardPalette.Spring) - Luminance(BoardPalette.Hub)),
                Is.GreaterThan(0.05f));
        }

        [Test]
        public void Nothing_in_the_game_depends_on_reaction_time()
        {
            // PRD section 1.2 promises no reflex stress. The simulation has no clock at all:
            // if a timer ever appears in the state, this is where the promise breaks.
            var stateProperties = typeof(Pathweaver.Core.State.GameState).GetProperties();

            Assert.That(
                stateProperties.Any(property =>
                    property.Name.Contains("Time") || property.Name.Contains("Timer")),
                Is.False,
                "Game state gained a time-dependent field, which the PRD forbids.");
        }

        /// <summary>
        /// Relative luminance, weighted the way human vision responds to each channel.
        /// </summary>
        private static float Luminance(Color colour)
            => (0.2126f * colour.r) + (0.7152f * colour.g) + (0.0722f * colour.b);
    }
}
