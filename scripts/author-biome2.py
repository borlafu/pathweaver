#!/usr/bin/env python3
"""Generates the biome-two level files from the specifications in author_biome2_specs.py.

A route is a kind, a starting cell — its spring — and a sequence of hex directions walked to its hub.
Everything else follows: the cells, the dead ends beyond each endpoint, the shape every conduit must
be, and therefore the tile bag.  Directions run clockwise on screen from due east.

This exists because twenty large boards are not maintainable as hand-written cell lists.  Every
mistake it now refuses was one it caught while these levels were being written: two routes wanting the
same cell, a spur placed on a route, a board too small to need panning or too large to walk, and — the
one worth the whole script — a target score higher than everything on the board pays, which three of
the seventeen had.

    python3 scripts/author-biome2.py

Run it after editing a specification.  It rewrites the level files in place and keeps any authored
solution they carry, since those lines depend on the order the tile bag deals and not on geometry.
"""
import os
import random
import sys

DIRS = [(1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1), (1, -1)]
LEVELS_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "levels")
ROUTES_DIR = os.environ.get("PATHWEAVER_ROUTES_DIR", "")

SHAPE_NAME = {1: "sharp bend", 2: "bend", 3: "straight"}
SHAPE_LETTER = {1: "^", 2: "b", 3: "-"}


def step(cell, direction):
    dq, dr = DIRS[direction]
    return (cell[0] + dq, cell[1] + dr)


def walk(start, dirs):
    cells = [start]
    for d in dirs:
        cells.append(step(cells[-1], d))
    return cells


def direction_between(a, b):
    for index, (dq, dr) in enumerate(DIRS):
        if (a[0] + dq, a[1] + dr) == b:
            return index
    raise AssertionError(f"{b} is not a neighbour of {a}")


def separation(a, b):
    diff = abs(a - b) % 6
    return min(diff, 6 - diff)


def hex_distance(a, b):
    dq = a[0] - b[0]
    dr = a[1] - b[1]
    return max(abs(dq), abs(dr), abs(dq + dr))


def disc(centre, radius):
    """Every cell within the given hex distance of a centre."""
    q0, r0 = centre
    out = []
    for dq in range(-radius, radius + 1):
        for dr in range(max(-radius, -dq - radius), min(radius, -dq + radius) + 1):
            out.append((q0 + dq, r0 + dr))
    return out


def world(cell):
    q, r = cell
    return (0.8660254 * (q + r / 2), -0.75 * r)


class Route:
    def __init__(self, kind, start, dirs):
        self.kind = kind
        self.cells = walk(start, dirs)
        assert len(set(self.cells)) == len(self.cells), f"{kind} route crosses itself"

    @property
    def spring(self):
        return self.cells[0]

    @property
    def hub(self):
        return self.cells[-1]

    @property
    def conduits(self):
        return self.cells[1:-1]

    def shapes(self):
        """The edge separation each conduit needs, in order."""
        out = []
        for index, cell in enumerate(self.conduits):
            before = self.cells[index]
            after = self.cells[index + 2]
            out.append(
                separation(direction_between(cell, before), direction_between(cell, after)))
        return out


def score(conduits):
    total = 100.0
    for _ in range(conduits - 1):
        total *= 1.35
    return int(total)


def render(spec):
    routes = [Route(*r) for r in spec["routes"]]

    cells = []
    for route in routes:
        for cell in route.cells:
            cells.append(cell)

    duplicates = [c for c in set(cells) if cells.count(c) > 1]
    assert not duplicates, f"{spec['id']}: two routes want the same cell: {sorted(duplicates)}"

    # Terrain, not corridors. The first version of biome two made the board *be* the route: a line of
    # cells with a dead end at either end, which left a player exactly one place to put each tile and
    # made a large board read as a diagram of a route rather than as somewhere to be.
    #
    # So each route is now the spine of an island, and every cell within `island_radius` of it is open
    # ground. That gives a player somewhere to lay a conduit that is not the way the board expects them
    # to reach the hub — including longer ways round, which pay more — and it is what makes panning feel
    # like crossing a place rather than following a wire.
    #
    # Craters are then carved back out, and the gaps between islands are the mountains. Both exist so
    # that open ground has a shape, because an unbroken blob is as characterless as a line. A route cell
    # is never carved, so no crater can make a board unsolvable.
    route_cells = set(cells)

    # Some boards are built around a hole: The Basin Road curves past an empty middle no route may
    # cross, and The Great Circuit encloses a space it never enters. Flooding those with island would
    # delete the only idea those boards have, so a spec may declare them off limits.
    forbidden = set()
    for centre, radius in spec.get("voids", []):
        forbidden.update(disc(centre, radius))

    ground = []
    for route in routes:
        for cell in route.cells:
            for candidate in disc(cell, spec.get("island_radius", 2)):
                if (candidate not in route_cells
                        and candidate not in forbidden
                        and candidate not in ground):
                    ground.append(candidate)

    # How far each piece of open ground is from the nearest route. Craters are carved from the high
    # ground — the cells furthest from any route — which is what turns one slab into islands rather
    # than punching arbitrary holes in a continent. The first attempt chose crater centres uniformly and
    # produced exactly that: a broad, regular shelf with two dents in it.
    height = {cell: min(hex_distance(cell, r) for r in route_cells) for cell in ground}

    craters = set()
    chooser = random.Random(spec["seed"])

    # Fractions of the ground rather than counts of cells. Counts were tuned per board and did not
    # survive a change to the island radius: the same "ten craters" that dented a ninety-cell board ate a
    # forty-cell one, and half the biome came out smaller after the islands were made larger.
    target_craters = int(len(ground) * spec.get("crater_fraction", 0.16))

    while len(craters) < target_craters:
        remaining = [cell for cell in ground if cell not in craters]
        if not remaining:
            break

        summit = max(height[cell] for cell in remaining)
        centre = chooser.choice(sorted(cell for cell in remaining if height[cell] == summit))

        for hollow in disc(centre, spec.get("crater_radius", 1)):
            if hollow not in route_cells:
                craters.add(hollow)

    # And a ragged coast. An island whose outline is a clean hexagon reads as a diagram of an island;
    # taking cells off the shore is the cheapest way to make it read as ground.
    edges = sorted(
        cell for cell in ground
        if cell not in craters and height[cell] == spec.get("island_radius", 2))

    for cell in chooser.sample(edges, int(len(edges) * spec.get("shore_fraction", 0.3))):
        craters.add(cell)

    cells.extend(cell for cell in ground if cell not in craters)

    xs = [world(c)[0] for c in cells]
    ys = [world(c)[1] for c in cells]
    span = (max(xs) - min(xs) + 0.87, max(ys) - min(ys) + 1.0)

    # Every biome-two board has to be one a player must navigate. The default zoom fits a
    # hexagon-3 board, about 5.2 by 5.2 world units, so anything under that is a biome-one board
    # wearing a biome-two name.
    assert span[0] >= 7.0 and span[1] >= 7.0, \
        f"{spec['id']}: span {span[0]:.1f} by {span[1]:.1f} does not need panning"

    # And an upper bound. Panning is travel; a board four screens wide is a commute. Raised when the
    # routes became islands: terrain adds an island radius of ground on every side, so a layout tuned to
    # fourteen units of route grew to eighteen of board. The bound is on the board, because that is what
    # a player crosses — a spec over it wants a smaller island rather than a bigger allowance.
    assert span[0] <= 17.0 and span[1] <= 14.0, \
        f"{spec['id']}: span {span[0]:.1f} by {span[1]:.1f} is too far to walk"

    # The bag: exactly what the routes need, plus the slack the spec asks for.
    needs = {}
    for route in routes:
        for shape in route.shapes():
            needs[(route.kind, shape)] = needs.get((route.kind, shape), 0) + 1

    bag = []
    for (kind, shape), count in sorted(needs.items()):
        bag.append((kind, shape, count + spec.get("slack", 1)))
    for kind, shape, count in spec.get("extra_tiles", []):
        bag.append((kind, shape, count))

    lines = [spec["comment"].rstrip(), ""]
    lines.append(f"id: {spec['id']}")
    lines.append(f"name: {spec['name']}")
    lines.append("")
    lines.append("base-score: 100")
    lines.append("")
    lines.append(spec["target_comment"].rstrip())
    lines.append(f"target-score: {spec['target']}")
    lines.append("")
    lines.append(f"tokens: {spec.get('tokens', 1)}")
    lines.append(f"skips: {spec.get('skips', 3)}")
    lines.append("")
    lines.append(f"seed: {spec['seed']}")
    lines.append("")

    seen = []
    for cell in cells:
        if cell not in seen:
            seen.append(cell)
    lines.append(f"# {len(seen)} cells, {span[0]:.1f} by {span[1]:.1f} world units.")
    for q, r in seen:
        lines.append(f"cell: {q},{r}")
    lines.append("")

    for route in routes:
        lines.append(f"spring: {route.spring[0]},{route.spring[1]} {route.kind}")
        lines.append(f"hub: {route.hub[0]},{route.hub[1]} {route.kind}")
    lines.append("")

    lines.append(spec["bag_comment"].rstrip())
    for kind, shape, count in bag:
        lines.append(f"tile: 0,{shape} {kind} x{count}")
    lines.append("")

    path = os.path.join(LEVELS_DIR, f"{spec['id']}.pwlevel")

    # An authored solution survives a regeneration. Eight of these boards carry one because the
    # solvability search cannot finish on them, and those lines are not derivable from the spec —
    # they depend on the order the tile bag deals, so they come from replaying a game rather than
    # from geometry. Rewriting the file without them would quietly uncertify the level.
    kept = ""
    if os.path.exists(path):
        existing = open(path).read()
        marker = "# The board's own solution"
        if marker in existing:
            kept = "\n" + existing[existing.index(marker):].rstrip() + "\n"

    with open(path, "w") as handle:
        handle.write("\n".join(lines) + "\n" + kept)

    # The route order, for whatever finds an authored solution. Written only when asked, because it
    # is an input to a tool that does not live here rather than part of the level.
    if ROUTES_DIR:
        os.makedirs(ROUTES_DIR, exist_ok=True)
        with open(os.path.join(ROUTES_DIR, f"{spec['id']}.txt"), "w") as handle:
            for route in routes:
                coords = " ".join(f"{q},{r}" for q, r in route.cells)
                handle.write(f"{route.kind} {coords}\n")

    summary = ", ".join(
        f"{r.kind[:2]}{len(r.conduits)}={score(len(r.conduits))}[{''.join(SHAPE_LETTER[s] for s in r.shapes())}]"
        for r in routes)
    total = sum(score(len(r.conduits)) for r in routes)
    return (f"{spec['id']:11s}{len(seen):3d}c {span[0]:5.1f}x{span[1]:4.1f} "
            f"target {spec['target']:5d} all {total:5d}  {summary}")


if __name__ == "__main__":
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from author_biome2_specs import SPECS  # noqa: E402

    wanted = set(sys.argv[1:])
    failures = 0
    for spec in SPECS:
        if wanted and spec["id"] not in wanted:
            continue

        try:
            print(render(spec))
        except AssertionError as error:
            failures += 1
            print(f"FAIL {error}")
    print(f"{failures} spec(s) failed")
