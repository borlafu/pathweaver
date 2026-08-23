# Play Store listing

Everything the store listing needs, in the form the Console asks for it. Text is here rather than
only in the Console so it can be reviewed in a pull request and so a rewrite has a history.

## Identity

| Field | Value |
|---|---|
| App name (30 characters max) | `Pathweaver: Pocket Realms` |
| Default language | English (United Kingdom) |
| Application type | Game |
| Category | Puzzle |
| Tags | Casual, Offline, Logic |

## Short description (80 characters max)

```
Link springs to hubs on a hex grid. One thumb, no internet, no interruptions.
```

76 characters. It says the verb, the shape, and the two promises the game is actually built around,
because a short description is the only text most people read.

## Full description (4000 characters max)

```
Pathweaver is a quiet puzzle about routes.

Tiles arrive one at a time. Turn each one, place it, and join a spring to a hub across a grid of
hexagons. A long route is worth far more than a short one — every extra conduit multiplies what the
route pays — so the question is never "can I connect these two?" but "how much further can I go
before the board runs out of room?".

Finish a route of four conduits or more and you earn a Pivot Token: turn a conduit already on the
board, or lift one off to free its cell. Close a shorter route instead and you earn a skip, which
throws away the tile in your hand and deals the next one. Neither reward dominates. Power comes from
length; room to manoeuvre comes from closing early; a player who only ever does one runs short of
the other.

TWENTY HANDMADE BOARDS
The first realm is twenty levels, each built around one idea: a corridor that turns at every cell, a
ring where the obvious single-tile answer pays a fraction of the target, a junction whose trunk is
paid for twice, and boards where water, wind and crystal compete for the same handful of cells. Every
one is verified solvable before it ships.

ENDLESS WAYFARE
A generated board every round, growing as you go: longer routes, more networks, less room. Rounds are
built from their own solution, so a round is always finishable. Your tokens come with you from one
round to the next.

BUILT TO BE PLAYED IN SHORT BURSTS
Nothing here needs the internet — not one board, not your progress, not a single feature. There are
no accounts, no energy meters, no daily streaks to protect, and no advertising. Close the game
mid-route and it opens exactly where you left it. It starts in under a second and drops to a low
frame rate while you think, so a puzzle on the bus does not cost you your battery.

ONE THUMB
Every control sits within reach of one hand. Tap a highlighted cell to place, tap the tile to turn
it, and hold a conduit to take it back.

No adverts. No purchases. No accounts. Just the next tile.
```

Under 2000 characters, which is deliberate: everything above the first fold is the first two
paragraphs, and the rest is for people already reading.

Three claims in there are load-bearing and are checked elsewhere, so they must not be edited into
something untrue:

- **"verified solvable before it ships"** — the level solvability gate in
  `tests/Pathweaver.Core.Tests/Solving/ShippedLevelsTests.cs`, which fails CI for an unsolvable level.
- **"starts in under a second"** — measured at 608–702 ms on a mid-range device with
  `adb shell am start -W`. Recorded in #30. If a future build slows down, this line changes.
- **"no advertising, no purchases"** — true of the MVP, and the reason the Data Safety form declares
  no collection. If rewarded video is ever added (#58), this text and that form both change.

## Graphics

Generated from the game's own cells and palette, so the listing cannot show something the game does
not look like:

```bash
unity -batchmode -quit -projectPath . \
  -executeMethod Pathweaver.EditorTools.StoreArt.Capture -logFile /tmp/unity.log
```

| File | Size | Play requirement |
|---|---|---|
| `Artifacts/store/icon-512.png` | 512×512 | PNG, 32-bit, no transparency |
| `Artifacts/store/feature-1024x500.png` | 1024×500 | PNG or JPEG, no transparency |

They are build outputs, so they are not committed — regenerate with the command above.

Both are placeholder art, drawn from the generated geometry the game currently renders. When real
tiles arrive the command produces the new ones with no edits here.

## Screenshots

Still to capture: Play needs between two and eight phone screenshots, at least 320 px on the short
edge. Take them from a device rather than the Editor, since the Editor's aspect is not a phone's:

```bash
adb exec-out screencap -p > Artifacts/store/screen-1.png
```

Worth showing, in this order: a board mid-route with the next tile in hand, a completed route paying
out, the level list, and an endless round with three networks.

## What is not in this listing

- No promo video. Optional, and the game has no soundtrack yet.
- No tablet or TV screenshots. Phone only for the MVP.
- No pre-registration. Not available to a personal account before production access.
