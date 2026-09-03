using System.Linq;
using NUnit.Framework;
using Pathweaver.Core.Atlas;
using Pathweaver.Game.App;
using Pathweaver.Game.Presentation.Menus;
using Pathweaver.Game.Presentation.Text;
using UnityEngine;

namespace Pathweaver.Game.EditorTests
{
    /// <summary>
    /// What the World Atlas says about itself.
    /// </summary>
    /// <remarks>
    /// The atlas was withheld from the closed test because it explained nothing, so the sentences are the
    /// feature. Whether they read well needs a person; whether they say the right number, agree with
    /// themselves about plurals, and cover every reason a node cannot be bought does not.
    /// </remarks>
    public class AtlasWordsTests
    {
        private static AtlasMap TwoNodes()
            => AtlasLoader.Parse(string.Join(
                "\n",
                "pack: test",
                "node: first cost 5 at 0,0 gives skip 1",
                "node: second cost 12 at 1,0 gives token 1 needs first"));

        /// <summary>
        /// A bonuses value, built the only way the presentation layer can build one.
        /// </summary>
        /// <remarks>
        /// <c>AtlasBonuses</c> has an internal constructor, so the game assembly cannot make one directly —
        /// it can only ask a map what a given progress adds up to. That is the right restriction and it is
        /// also how <c>GameFlow</c> gets the value, so the tests go the same way.
        /// </remarks>
        private static AtlasBonuses BonusesOf(params string[] unlocked)
        {
            var map = AtlasLoader.Parse(string.Join(
                "\n",
                "pack: test",
                "node: a-skip cost 1 at 0,0 gives skip 1",
                "node: b-essence cost 1 at 1,0 gives essence 2",
                "node: c-token cost 1 at 0,1 gives token 3"));

            return map.BonusesFor(AtlasProgress.Of(unlocked, 0));
        }

        [Test]
        public void A_balance_is_a_figure_and_its_name()
        {
            Assert.That(AtlasWords.Balance(0), Is.EqualTo("0 Star Essence"));
            Assert.That(AtlasWords.Balance(7), Is.EqualTo("7 Star Essence"));
        }

        [Test]
        public void A_large_balance_is_grouped_like_every_other_number_in_the_game()
        {
            // The score under the progress bar groups thousands, and two renderings of a figure in one
            // game read as two different kinds of figure.
            Assert.That(AtlasWords.Balance(1234), Is.EqualTo("1,234 Star Essence"));
        }

        [Test]
        public void One_of_something_is_written_as_a_word_and_a_singular()
        {
            Assert.That(
                AtlasWords.Effect(new AtlasEffect(AtlasEffectKind.Skip, 1)),
                Is.EqualTo("One more skip on every board."));

            Assert.That(
                AtlasWords.Effect(new AtlasEffect(AtlasEffectKind.Token, 1)),
                Is.EqualTo("One more Pivot Token on every board."));
        }

        [Test]
        public void More_than_one_takes_a_numeral_and_a_plural()
        {
            Assert.That(
                AtlasWords.Effect(new AtlasEffect(AtlasEffectKind.Skip, 2)),
                Is.EqualTo("2 more skips on every board."));

            Assert.That(
                AtlasWords.Effect(new AtlasEffect(AtlasEffectKind.Token, 3)),
                Is.EqualTo("3 more Pivot Tokens on every board."));
        }

        [Test]
        public void Essence_is_worded_as_a_rate_rather_than_a_holding()
        {
            // The other two effects are things a player holds during a board. This one is paid at the end
            // of one, and "one more Star Essence on every board" would read as the former.
            Assert.That(
                AtlasWords.Effect(new AtlasEffect(AtlasEffectKind.Essence, 1)),
                Does.Contain("for every board you clear"));
        }

        [Test]
        public void An_unlocked_node_says_so_and_nothing_else()
        {
            var map = TwoNodes();
            var progress = AtlasProgress.Of(new[] { "first" }, 0);

            Assert.That(AtlasWords.Status(map.Node("first"), map, progress), Is.EqualTo("Unlocked."));
        }

        [Test]
        public void An_affordable_node_invites_the_second_tap()
        {
            // Buying takes two taps, so the first one has to say that the second will spend.
            var map = TwoNodes();
            var progress = AtlasProgress.Of(System.Array.Empty<string>(), 5);

            var status = AtlasWords.Status(map.Node("first"), map, progress);

            Assert.That(status, Does.Contain("5 Star Essence"));
            Assert.That(status, Does.Contain("Tap again"));
        }

        [Test]
        public void A_node_the_player_cannot_afford_says_the_cost_and_the_balance()
        {
            // The gap is the answer. A cost alone leaves the player to remember what they hold, which is
            // the arithmetic the silence used to leave them doing.
            var map = TwoNodes();
            var progress = AtlasProgress.Of(new[] { "first" }, 7);

            var status = AtlasWords.Status(map.Node("second"), map, progress);

            Assert.That(status, Does.Contain("12 Star Essence"));
            Assert.That(status, Does.Contain("You have 7"));
        }

        [Test]
        public void A_node_behind_another_says_that_rather_than_its_cost()
        {
            // Its cost is not why it is refused, and answering with a cost the player could meet would be
            // worse than the silence it replaces.
            var map = TwoNodes();
            var progress = AtlasProgress.Of(System.Array.Empty<string>(), 100);

            var status = AtlasWords.Status(map.Node("second"), map, progress);

            Assert.That(status, Does.Contain("grows from"));
            Assert.That(status, Does.Not.Contain("12"));
        }

        [Test]
        public void Every_reason_a_node_cannot_be_bought_produces_a_sentence()
        {
            // Four states, four answers, none of them empty. A blank line here is the bug this screen was
            // withheld for.
            var map = TwoNodes();

            var cases = new[]
            {
                AtlasProgress.Of(System.Array.Empty<string>(), 0),
                AtlasProgress.Of(System.Array.Empty<string>(), 100),
                AtlasProgress.Of(new[] { "first" }, 0),
                AtlasProgress.Of(new[] { "first", "second" }, 0),
            };

            foreach (var progress in cases)
            {
                foreach (var node in map.Nodes)
                {
                    Assert.That(
                        AtlasWords.Status(node, map, progress),
                        Is.Not.Empty,
                        $"{node.Id} says nothing at {progress}.");
                }
            }
        }

        [Test]
        public void A_discount_relic_says_what_it_lowers()
        {
            // The one effect the second region had room for, and the only one that changes nothing on a
            // board — so the sentence has to say what it does change.
            Assert.That(
                AtlasWords.Effect(new AtlasEffect(AtlasEffectKind.Discount, 1)),
                Is.EqualTo("Every node you have not bought yet costs 1 less."));

            Assert.That(
                AtlasWords.Effect(new AtlasEffect(AtlasEffectKind.Discount, 2)),
                Does.Contain("2 less"));
        }

        [Test]
        public void A_discounted_node_quotes_the_price_it_will_charge()
        {
            // Quoting the pack file's number would contradict both the numeral drawn on the node and what
            // the purchase actually takes. Three places, one figure.
            var map = AtlasLoader.Parse(string.Join(
                "\n",
                "pack: test",
                "node: cheap cost 3 at 0,0 gives discount 1",
                "node: dear cost 9 at 1,0 gives skip 1 needs cheap"));

            var withRelic = AtlasProgress.Of(new[] { "cheap" }, essence: 20);

            Assert.That(
                AtlasWords.Status(map.Node("dear"), map, withRelic),
                Does.Contain("8 Star Essence"));

            Assert.That(
                AtlasWords.Status(map.Node("dear"), map, withRelic),
                Does.Not.Contain("9 Star Essence"));
        }

        [Test]
        public void A_discount_is_not_named_among_the_relics_in_force_on_a_board()
        {
            // The relics line is shown while paused, and answers "why does this board deal four skips".
            // A discount answers nothing about a board, so naming it there would be noise at the one
            // moment the line has a job.
            var map = AtlasLoader.Parse(string.Join(
                "\n",
                "pack: test",
                "node: thrift cost 3 at 0,0 gives discount 2"));

            var bonuses = map.BonusesFor(AtlasProgress.Of(new[] { "thrift" }, essence: 0));

            Assert.That(AtlasWords.Relics(bonuses), Is.Empty);
        }

        [Test]
        public void No_relics_means_no_line_rather_than_a_line_saying_none()
        {
            // A player who has bought none has probably never opened the atlas, and a line about a screen
            // they have not seen explains less than an empty space.
            Assert.That(AtlasWords.Relics(AtlasBonuses.None), Is.Empty);
        }

        [Test]
        public void A_relics_line_names_only_what_is_in_force()
        {
            var line = AtlasWords.Relics(BonusesOf("a-skip", "b-essence"));

            Assert.That(line, Does.Contain("+1 skip"));
            Assert.That(line, Does.Contain("+2 Star Essence"));
            Assert.That(line, Does.Not.Contain("Pivot"));
        }

        [Test]
        public void A_clear_that_paid_nothing_says_nothing()
        {
            Assert.That(AtlasWords.Earned(0), Is.Empty);
            Assert.That(AtlasWords.Earned(-3), Is.Empty);
        }

        [Test]
        public void A_clear_that_paid_says_how_much()
        {
            Assert.That(AtlasWords.Earned(4), Is.EqualTo("+4 Star Essence"));
        }

        [Test]
        public void Every_effect_has_its_own_mark_and_its_own_sentence()
        {
            // Discount shipped wearing the essence diamond, because the switch that picks a mark had a
            // catch-all and a new effect silently borrowed an existing one. Two different relics looked
            // like the same relic. This is the test that would have said so.
            var shapes = new System.Collections.Generic.Dictionary<string, AtlasEffectKind>();
            var sentences = new System.Collections.Generic.HashSet<string>();

            foreach (AtlasEffectKind kind in System.Enum.GetValues(typeof(AtlasEffectKind)))
            {
                var mesh = AtlasView.MarkFor(kind, radius: 1f);
                Assert.That(mesh, Is.Not.Null, $"{kind} has no mark.");

                // The geometry itself, not its vertex count: a bar and a triangle both have four
                // vertices, so counting them called two obviously different shapes the same.
                var fingerprint = string.Join(
                    ";",
                    mesh.vertices
                        .Select(v => $"{v.x:F3},{v.y:F3}")
                        .OrderBy(text => text, System.StringComparer.Ordinal));

                UnityEngine.Object.DestroyImmediate(mesh);

                Assert.That(
                    shapes.ContainsKey(fingerprint),
                    Is.False,
                    $"{kind} wears the same shape as {(shapes.TryGetValue(fingerprint, out var other) ? other.ToString() : "another effect")}.");

                shapes[fingerprint] = kind;

                Assert.That(
                    sentences.Add(AtlasWords.Effect(new AtlasEffect(kind, 1))),
                    Is.True,
                    $"{kind} says the same thing as another effect.");
            }
        }

        [Test]
        public void Every_character_the_atlas_writes_is_in_the_font_atlas()
        {
            // The same guard the help screen carries. The atlas is static and stops at Latin-1 plus seven
            // marks; a character outside it renders as nothing, which here would be a sentence with a hole
            // in it on the one screen whose entire purpose is being readable.
            var allowed = Enumerable.Range(0x20, 0x7F - 0x20)
                .Concat(Enumerable.Range(0xA0, 0x100 - 0xA0))
                .Select(code => (char)code)
                .Concat("–—''\"\"…")
                .ToHashSet();

            var map = TwoNodes();
            var progress = AtlasProgress.Of(new[] { "first" }, 9);

            var lines = new[]
            {
                AtlasWords.Introduction,
                AtlasWords.Balance(1234),
                AtlasWords.Earned(4),
                AtlasWords.Relics(BonusesOf("a-skip", "b-essence", "c-token")),
            }.Concat(map.Nodes.Select(node => AtlasWords.Effect(node.Effect)))
             .Concat(map.Nodes.Select(node => AtlasWords.Status(node, map, progress)));

            foreach (var line in lines)
            {
                foreach (var character in line)
                {
                    Assert.That(
                        allowed.Contains(character),
                        Is.True,
                        $"U+{(int)character:X4} is not in the font atlas, in: \"{line}\"");
                }
            }
        }

        [Test]
        public void Essence_banked_while_the_atlas_was_hidden_is_spendable()
        {
            // The promise made when the screen was withheld: AwardEssence kept paying, so a player who
            // cleared the campaign meanwhile should find the first region affordable now.
            var map = AtlasCatalogue.Load();
            var banked = AtlasProgress.Of(System.Array.Empty<string>(), 77);

            var affordable = map.Nodes.Where(node => map.CanUnlock(node.Id, banked)).ToList();

            Assert.That(affordable, Is.Not.Empty, "Nothing in the atlas can be bought with a full bank.");
        }
    }
}
