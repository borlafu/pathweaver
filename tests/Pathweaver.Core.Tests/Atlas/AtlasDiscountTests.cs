using Pathweaver.Core.Atlas;
using Xunit;

namespace Pathweaver.Core.Tests.Atlas;

/// <summary>
/// Discount relics: the one thing a second region had room to give.
/// </summary>
/// <remarks>
/// The first region already grants the most skips and the most Pivot Tokens the balance allows, so a
/// second region can only make a board easier by raising a ceiling that exists to stop a board becoming
/// impossible to deadlock. A relic that lowers a price changes no board at all, which is why it is the
/// effect the second region is built from.
/// </remarks>
public class AtlasDiscountTests
{
    private static AtlasMap Region()
        => AtlasLoader.Parse(string.Join(
            "\n",
            "pack: test",
            "node: cheap cost 3 at 0,0 gives discount 1",
            "node: dear cost 9 at 1,0 gives skip 1 needs cheap",
            "node: dearer cost 12 at 2,0 gives discount 2 needs dear"));

    [Fact]
    public void A_node_costs_its_face_value_before_any_discount()
    {
        var map = Region();

        Assert.Equal(9, map.CostOf("dear", AtlasProgress.Empty));
    }

    [Fact]
    public void A_discount_relic_lowers_every_price_still_to_be_paid()
    {
        var map = Region();
        var withRelic = AtlasProgress.Of(new[] { "cheap" }, essence: 0);

        Assert.Equal(8, map.CostOf("dear", withRelic));
        Assert.Equal(11, map.CostOf("dearer", withRelic));
    }

    [Fact]
    public void Discounts_add_up()
    {
        var map = Region();
        var both = AtlasProgress.Of(new[] { "cheap", "dearer" }, essence: 0);

        // One from cheap and two from dearer.
        Assert.Equal(6, map.CostOf("dear", both));
    }

    [Fact]
    public void A_price_never_falls_below_one()
    {
        // A free node is not a decision, and a region whose prices reached zero would unlock itself the
        // moment a player looked at it.
        var map = AtlasLoader.Parse(string.Join(
            "\n",
            "pack: test",
            "node: generous cost 1 at 0,0 gives discount 5",
            "node: trivial cost 2 at 1,0 gives skip 1 needs generous"));

        var withRelic = AtlasProgress.Of(new[] { "generous" }, essence: 0);

        Assert.Equal(AtlasMap.MinimumCost, map.CostOf("trivial", withRelic));
    }

    [Fact]
    public void A_discount_decides_what_can_be_afforded()
    {
        var map = Region();

        // Eight essence, against a face value of nine.
        Assert.False(map.CanUnlock("dear", AtlasProgress.Of(new[] { "cheap" }, essence: 0)));
        Assert.True(map.CanUnlock("dear", AtlasProgress.Of(new[] { "cheap" }, essence: 8)));
    }

    [Fact]
    public void A_discount_is_not_a_board_bonus()
    {
        // The whole reason this effect exists: it must not move the numbers the balance gate watches.
        var map = Region();
        var bonuses = map.BonusesFor(AtlasProgress.Of(new[] { "cheap", "dearer" }, essence: 0));

        Assert.Equal(0, bonuses.Skips);
        Assert.Equal(0, bonuses.Tokens);
        Assert.Equal(0, bonuses.EssencePerClear);
        Assert.Equal(3, bonuses.Discount);
    }

    [Fact]
    public void An_unknown_node_has_no_price()
    {
        var map = Region();

        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
            () => map.CostOf("nowhere", AtlasProgress.Empty));
    }

    [Fact]
    public void A_pack_file_may_say_discount()
    {
        var map = Region();

        Assert.Equal(new AtlasEffect(AtlasEffectKind.Discount, 1), map.Node("cheap").Effect);
    }

    [Fact]
    public void An_unknown_effect_is_still_refused()
    {
        // The parser gained a word; it must not have gained a shrug.
        Assert.Throws<AtlasFormatException>(() => AtlasLoader.Parse(string.Join(
            "\n",
            "pack: test",
            "node: odd cost 3 at 0,0 gives sunshine 1")));
    }
}
