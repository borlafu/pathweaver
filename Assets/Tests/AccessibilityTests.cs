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

        [Test]
        public void Text_meets_the_contrast_ratio_WCAG_asks_of_body_text()
        {
            // 4.5:1 is the AA threshold for text below large-print size, and the game's smallest label
            // is well below it. This is not a rule of thumb like the luminance gaps above: it is the
            // published formula, so it is worth applying properly.
            Assert.That(
                ContrastRatio(BoardPalette.TextPrimary, BoardPalette.Background),
                Is.GreaterThanOrEqualTo(4.5f));

            Assert.That(
                ContrastRatio(BoardPalette.TextSecondary, BoardPalette.Background),
                Is.GreaterThanOrEqualTo(4.5f));
        }

        [Test]
        public void A_control_colour_would_not_have_done_for_text()
        {
            // Why text has its own two colours rather than borrowing a control's. The first text
            // preview was drawn in TokenEmpty, the colour of an unfilled pip, and came out unreadable.
            // A dim pip is still legibly a pip; dim text is not legibly anything.
            Assert.That(
                ContrastRatio(BoardPalette.TokenEmpty, BoardPalette.Background),
                Is.LessThan(4.5f),
                "TokenEmpty now passes for text, so the reason these colours are separate has gone.");
        }

        /// <summary>
        /// Relative luminance, weighted the way human vision responds to each channel.
        /// </summary>
        private static float Luminance(Color colour)
            => (0.2126f * colour.r) + (0.7152f * colour.g) + (0.0722f * colour.b);

        /// <summary>
        /// The WCAG contrast ratio between two colours, from 1:1 to 21:1.
        /// </summary>
        /// <remarks>
        /// Uses WCAG's own relative luminance, which linearises each channel first. That differs from
        /// <see cref="Luminance"/>, which is a weighted average of the raw values — good enough for
        /// asking whether two fills are distinguishable, but not for a published threshold. Text gets
        /// the real formula.
        /// </remarks>
        private static float ContrastRatio(Color first, Color second)
        {
            var lighter = Mathf.Max(WcagLuminance(first), WcagLuminance(second));
            var darker = Mathf.Min(WcagLuminance(first), WcagLuminance(second));

            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float WcagLuminance(Color colour)
            => (0.2126f * Linearise(colour.r))
               + (0.7152f * Linearise(colour.g))
               + (0.0722f * Linearise(colour.b));

        private static float Linearise(float channel)
            => channel <= 0.03928f
                ? channel / 12.92f
                : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
