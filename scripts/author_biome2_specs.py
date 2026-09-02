"""The biome-two levels, as specifications rather than as transcribed cell lists.

Read by scripts/author-biome2.py, which turns each of these into a level file.  A route is a kind, the
cell its spring sits on, and the directions walked from there to its hub; the conduits, their shapes and
the bag are all derived.  `slack` is how many spare tiles of each shape the bag carries beyond what the
routes need, and `spur_depth` how far the dead ends beyond each endpoint chain outward.
"""


def line(direction, count):
    return [direction] * count


def zig(first, second, pairs):
    return ([first, second] * pairs)


def elbow(first, count_first, second, count_second):
    return line(first, count_first) + line(second, count_second)


SPECS = [
    dict(
        id="biome2-04",
        name="The Crossing",
        seed=31,
        target=600,
        routes=[
            ("water", (-5, -5), line(0, 5)),
            ("crystal", (3, -6), zig(1, 2, 4)),
        ],
        comment="""# Biome 2, level 4 — The Crossing
#
# A straight run along the northern rim and a zigzag falling the whole height of the board, laid so
# they pass close without ever wanting the same cell. Two work sites, one wide and one tall, and the
# only thing between them is distance.
#
# The zigzag is the board. Seven conduits of bends pay 605 on their own, which is five more than the
# target — so a player who reads the board can clear it with one route and never touch the other. The
# straight run is the safety margin, and finding that out is the lesson.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# The zigzag alone reaches 605 against this. Both routes reach 851.""",
        bag_comment="""# Bends for the zigzag, straights for the rim, and one spare of each in case a draw arrives before
# there is anywhere to put it.""",
    ),
    dict(
        id="biome2-05",
        name="The Far Shore",
        seed=37,
        target=600,
        routes=[
            ("water", (-6, -4), zig(1, 2, 3)),
            ("wind", (5, -4), zig(2, 1, 3)),
            ("crystal", (-4, 5), line(0, 6)),
        ],
        comment="""# Biome 2, level 5 — The Far Shore
#
# Two zigzags coming down the far edges of the board and a straight run along the bottom joining
# their feet. Three sites, and no two of them fit on one screen — which is the first time that is
# true of a level rather than of a level's ambition.
#
# The bottom run is the cheapest thing here and the most useful: it is the only route a player can
# reach from either descent without crossing the whole board.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Any two of the three reach 664 against this. One alone pays 332 and does not.""",
        bag_comment="""# Bends for the two descents, straights for the shore.""",
    ),
    dict(
        id="biome2-06",
        name="Three Rivers",
        seed=41,
        target=800,
        routes=[
            ("water", (-4, -5), zig(1, 2, 4)),
            ("wind", (1, -5), zig(1, 2, 4)),
            ("crystal", (6, -5), zig(1, 2, 4)),
        ],
        comment="""# Biome 2, level 6 — Three Rivers
#
# The same descent three times, side by side, at the full height of the board. Nothing here is a
# puzzle: it is a board about pace, and about whether a player has learned to finish a site before
# travelling rather than dabbling at all three.
#
# Identical routes on purpose. When three sites differ, a player picks the easy one; when they are the
# same, the only decision left is the order, and the order is about the tile bag rather than the board.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Two rivers clear it at 1,210. All three reach 1,815.""",
        bag_comment="""# Bends only, in three kinds. A river accepts any bend of its own colour, so the skip control is
# here for the wrong kind rather than the wrong shape.""",
    ),
    dict(
        id="biome2-07",
        name="The Long Bend",
        seed=43,
        target=650,
        routes=[
            ("water", (-5, -5), elbow(0, 3, 1, 6)),
            ("crystal", (3, 4), elbow(3, 3, 4, 6)),
        ],
        comment="""# Biome 2, level 7 — The Long Bend
#
# Two routes that each run straight, turn once, and run straight again — mirrored, so one comes down
# from the north-west and the other climbs from the south-east, and they end up pointing at each
# other's start.
#
# The corner is the whole idea. Seven of a route's eight conduits want a straight and exactly one wants
# a bend, so a bend drawn early has one place it belongs and a straight drawn at the corner has none.
# That is the first board on which the skip control is a plan rather than a rescue.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Either bend alone reaches 817 against this, so one route clears the board and the second is
# there to be attempted rather than needed.""",
        bag_comment="""# Almost all straights, and one bend per kind for the corner. The counts are exact plus a spare,
# because a bag with three spare bends would make the corner free.""",
        slack=1,
    ),
    dict(
        id="biome2-08",
        name="Switchback",
        seed=47,
        target=700,
        spur_depth=2,
        routes=[
            ("water", (-3, -5), [0, 0, 2, 2, 0, 0, 2, 2, 0]),
            ("wind", (4, 4), [3, 3, 5, 5, 3, 3, 5, 5, 3]),
        ],
        comment="""# Biome 2, level 8 — Switchback
#
# The first board built from sharp turns. A route runs two cells east, doubles back two cells
# south-west, and repeats — so its corner cells need a tile with its two edges one apart rather than
# two, which is a shape no board before this has asked for.
#
# The shape matters because it is the one a player will have mistaken for a bend. A wide bend turns
# sixty degrees and a sharp one a hundred and twenty; they look alike in the tray and are not
# interchangeable anywhere on this board.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Either switchback reaches 817. Both reach 1,634, which is what the board is for once the target
# stops being the question.""",
        bag_comment="""# Straights for the runs and sharp bends for the corners, in the proportions the routes actually
# need. Two shapes that are easy to confuse, so the counts are tight.""",
    ),
    dict(
        id="biome2-09",
        name="The Basin Road",
        seed=53,
        target=550,
        routes=[
            ("water", (-4, -4), [0, 0, 1, 1, 2, 2, 3, 3]),
            ("crystal", (4, 4), [3, 3, 4, 4, 5, 5, 0, 0]),
        ],
        comment="""# Biome 2, level 9 — The Basin Road
#
# Two roads curving round the rim of a basin nothing fills. Each turns four times by sixty degrees,
# so almost every cell is a bend and the two straights are the ones that read as mistakes.
#
# The empty middle is the point. It is the largest open space in the biome and no route may cross it,
# which makes the board feel like a place with a hole in it rather than a puzzle with a gap.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Either road reaches 605 against this. Both reach 1,210.""",
        bag_comment="""# Mostly bends, because a curve is mostly corners.""",
    ),
    dict(
        id="biome2-10",
        name="Two Cliffs",
        seed=59,
        target=900,
        routes=[
            ("water", (-4, -5), line(1, 8)),
            ("wind", (4, -5), line(1, 8)),
        ],
        comment="""# Biome 2, level 10 — Two Cliffs
#
# Two straight drops the full height of the board, as far apart as the board allows. Seven conduits
# each, every one of them a straight, and nothing between them.
#
# This is the plainest board in the biome and the one that answers the question the biome exists to
# ask: whether travelling a long way to do a simple thing is worth doing twice. The answer the board
# gives is no — one cliff clears it — and the second is there for a player who wants the score.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# One cliff pays 605 and both pay 1,210, so this is the first board in the biome that needs two
# routes rather than rewarding them.""",
        bag_comment="""# Straights, and one bend per kind that belongs nowhere on this board. Two skips will be spent on
# them, out of three.""",
        extra_tiles=[("water", 2, 1), ("wind", 2, 1)],
    ),
    dict(
        id="biome2-11",
        name="The Cistern Walk",
        seed=61,
        target=750,
        slack=2,
        routes=[
            ("water", (-5, -3), [0, 0, 0, 1, 1, 1, 0, 0]),
            ("crystal", (4, 4), [3, 3, 3, 4, 4, 4, 3, 3]),
            ("wind", (0, -6), line(1, 4)),
        ],
        comment="""# Biome 2, level 11 — The Cistern Walk
#
# Two long walks that each turn once and then straighten again, with a short wind drop between them
# for a player who wants the target quickly rather than the score.
#
# The short route is the interesting one. It is three conduits against the walks' seven, so it pays
# a sixth of what they do — and it is the only thing on the board reachable without a journey. A
# board that offers a cheap option and an expensive one is a board with a decision in it.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# One walk plus the wind drop reaches 787. Both walks reach 1,210.""",
        bag_comment="""# Straights for the walks with one bend each for their corners, and straights for the drop.""",
    ),
    dict(
        id="biome2-12",
        name="Chain of Wells",
        seed=67,
        target=500,
        routes=[
            ("water", (-4, -4), line(0, 4)),
            ("wind", (-3, -1), line(0, 4)),
            ("crystal", (-2, 2), line(0, 4)),
            ("trade", (-1, 5), line(0, 4)),
        ],
        comment="""# Biome 2, level 12 — Chain of Wells
#
# Four short runs stepping down and across the board, each three conduits long, each a different
# kind. The first board in the biome with four pairs on it.
#
# Short routes on a large board invert the usual pressure. Nothing here takes long to build and
# everything takes a while to reach, so the cost of a wasted tile is the journey rather than the tile
# — which is the first time the skip control is cheaper than walking.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Each well pays 182, so three reach 546 against this and four reach 728. Which three is the
# player's business.""",
        bag_comment="""# Straights in four kinds. With four bags interleaved a player holds the wrong colour more often
# than not, which is what makes the walking matter.""",
        slack=2,
    ),
    dict(
        id="biome2-13",
        name="The Split Ridge",
        seed=71,
        target=780,
        routes=[
            ("water", (0, -6), zig(2, 1, 4)),
            ("wind", (-6, 3), line(0, 4)),
            ("crystal", (3, 0), line(1, 5)),
        ],
        comment="""# Biome 2, level 13 — The Split Ridge
#
# A descent down the middle with two spurs leaving it east and south, so the board is one long
# journey with two short errands hanging off it.
#
# Three routes of three different lengths, which is what makes the order matter: the descent pays
# most and costs most, and a player who builds it first spends the rest of the board holding tiles
# for routes they have already finished.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# The descent alone pays 605; with either spur it clears. All three reach 1,183.""",
        bag_comment="""# Bends for the descent, straights for the two spurs.""",
    ),
    dict(
        id="biome2-14",
        name="Deep Country",
        seed=73,
        target=850,
        routes=[
            ("water", (-6, -4), line(0, 5)),
            ("wind", (4, -5), line(1, 7)),
            ("crystal", (-5, 5), line(0, 5)),
        ],
        comment="""# Biome 2, level 14 — Deep Country
#
# The widest board in the biome. A run along the north, a drop down the east, and a run along the
# south, arranged so that the three of them touch the four corners between them and the middle of
# the board is empty.
#
# It exists to be crossed. Nothing about the routes is hard and everything about reaching them is
# long, which is the biome's own idea taken as far as one board can take it.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# All three reach 940 against this. The drop and either run reach 694, so this is a board that
# has to be finished three times.""",
        bag_comment="""# Straights throughout, which is what a board about distance rather than difficulty wants.""",
    ),
    dict(
        id="biome2-15",
        name="The Four Corners",
        seed=79,
        target=700,
        routes=[
            ("water", (-6, -5), line(0, 4)),
            ("wind", (4, -5), line(1, 4)),
            ("crystal", (-6, 4), line(0, 4)),
            ("trade", (2, 2), line(1, 4)),
        ],
        comment="""# Biome 2, level 15 — The Four Corners
#
# One pair in each corner of the board and nothing in the middle. Four kinds, three conduits each,
# and the longest journey in the biome between any two of them.
#
# The board is a test of the tray rather than of the board. Four kinds interleaved means the tile in
# hand belongs to a quarter of the screen a player is probably not looking at, and the choice every
# time is to travel, to skip, or to leave the tile somewhere it does nothing.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Each corner pays 182, so all four reach 728 against this. Three reach 546. Every corner is
# needed, which is the point of putting them as far apart as the board allows.""",
        bag_comment="""# Straights in four kinds, generously, because the walking is the cost here and a dry bag would
# make it a wait instead.""",
        slack=2,
    ),
    dict(
        id="biome2-16",
        name="The Long Portage",
        seed=83,
        target=1100,
        routes=[
            ("water", (-7, -3), elbow(0, 5, 1, 6)),
            ("wind", (2, -6), line(0, 4)),
        ],
        comment="""# Biome 2, level 16 — The Long Portage
#
# The longest single route in the biome: ten conduits, east across the north of the board and then
# south-east down its far side, with one corner in the middle of it.
#
# Ten conduits pay 1,489, which is more than any two routes on any board before this. The board is
# an argument that length is worth more than breadth — and the four-conduit wind run beside it is
# the counter-argument, because it is a quarter of the work for a sixth of the pay.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# The portage alone reaches 1,489. Nothing else on the board comes close, and the target is set
# where the portage clears it and the wind run cannot.""",
        bag_comment="""# Straights, and the one bend the corner needs.""",
    ),
    dict(
        id="biome2-17",
        name="Contested Ground",
        seed=89,
        target=600,
        slack=3,
        routes=[
            ("water", (-7, 0), [0, 0, 0, 1, 1, 0]),
            ("crystal", (2, -7), [1, 1, 1, 2, 2, 1, 1]),
            ("wind", (2, 1), line(1, 4)),
        ],
        comment="""# Biome 2, level 17 — Contested Ground
#
# Two routes whose corridors run into the same part of the board from different sides, each turning
# twice to get through it. They do not share a cell — a cell holds one tile and one kind, so a shared
# cell would make one route impossible rather than difficult — but they share the room.
#
# What they contest is attention. The two work sites overlap on screen, so this is the one board in
# the biome where both routes are visible at once, and the temptation is to build them together and
# finish neither.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Nothing here clears it alone: the long corridor pays 448, the short one 332 and the wind drop
# 182. The cheapest pair is the long corridor and the drop at 630, and all three reach 962. This is
# the board in the biome that has to be finished twice.""",
        bag_comment="""# Straights with two bends each for the corridors that turn, and straights for the drop. Slack of
# three per shape, because a board that must be finished twice cannot also punish a bad draw.""",
    ),
    dict(
        id="biome2-18",
        name="The Great Circuit",
        seed=97,
        target=1200,
        routes=[
            ("water", (-3, -4), [0, 0, 1, 1, 1, 2, 2, 3, 3, 4, 4]),
            ("wind", (2, 3), line(0, 4)),
        ],
        comment="""# Biome 2, level 18 — The Great Circuit
#
# A route that goes almost all the way round and comes back nearly to where it started: ten conduits
# and five turns, enclosing a space it never enters.
#
# The circuit is the biome's answer to biome one's The Long Way Round, which asked the same question
# on a board a player could see. Here the far side of the circuit is off screen while the near side
# is being built, so the shape has to be held in the head rather than read off the board.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# The circuit alone reaches 1,489. The wind run beside it pays 246, which is not the difference.""",
        bag_comment="""# Bends where the circuit turns and straights where it runs, in the proportions it needs.""",
    ),
    dict(
        id="biome2-19",
        name="Watersheds",
        seed=101,
        target=780,
        routes=[
            ("water", (-2, -6), zig(1, 2, 4)),
            ("wind", (4, -6), zig(2, 1, 4)),
            ("crystal", (-6, 3), line(0, 5)),
            ("trade", (1, 5), line(0, 4)),
        ],
        comment="""# Biome 2, level 19 — Watersheds
#
# Two descents that lean away from each other and two runs beneath them, so the board reads as a
# ridge with the country falling away on both sides. Four pairs, and the last board before the one
# that uses everything.
#
# The two descents are mirror images and the two runs are not, which is deliberate: a player who has
# learned the biome will recognise the descents and have to read the runs.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# Either descent plus either run reaches at least 787 against this. All four reach 1,638.""",
        bag_comment="""# Bends for the descents, straights for the runs, in four kinds.""",
    ),
    dict(
        id="biome2-20",
        name="The Whole Country",
        seed=103,
        target=1300,
        routes=[
            ("water", (-6, -5), elbow(0, 3, 1, 4)),
            ("wind", (4, -6), line(1, 8)),
            ("crystal", (-6, 5), line(0, 6)),
            ("trade", (-1, -2), zig(1, 2, 3)),
        ],
        comment="""# Biome 2, level 20 — The Whole Country
#
# The last board in the biome, and every kind it has: an elbow across the north, a drop down the
# east, a run along the south, and a short descent through the middle. Four pairs, four kinds, and
# the largest footprint of anything shipped.
#
# It repeats rather than inventing. Nothing here is a shape the biome has not already taught, which
# is what a last level should be — the biome's whole vocabulary at once, on a board large enough
# that a player has to decide what to ignore.
#
# Edge directions run clockwise on screen from due east: 0 east, 1 south-east, 2 south-west, 3 west,
# 4 north-west, 5 north-east.""",
        target_comment="""# The drop pays 605, the elbow 448, the southern run 332 and the middle descent 332. Any three
# reach at least 1,385 against this; the best two reach 1,053 and do not. All four reach 1,717.""",
        bag_comment="""# Every shape the biome uses, in four kinds, at the counts the routes need plus a spare each.""",
        slack=1,
    ),
]
