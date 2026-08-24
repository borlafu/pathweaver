# Pathweaver: Pocket Realms

Pathweaver is a cozy, offline-first hex route-building puzzle game for Android, built
for single-thumb play in the three-to-six minute gaps of a commute. You draw
hexagonal conduit tiles and lay them across a hex board to carry water, wind,
crystal, and trade from source springs to destination hubs, choosing each turn
between closing a short route for a guaranteed payout and extending a longer one for
a much larger multiplier at the cost of congesting the board. Nothing requires a
network connection, there are no energy gates, no gacha, and no forced or
unskippable ads. See [PRD.md](PRD.md) for the full product specification and
[CLAUDE.md](CLAUDE.md) for toolchain setup and the decision log.

> **Status:** pre-release. The simulation is complete and test-covered, and a
> playable vertical slice runs on real hardware. All art is placeholder procedural
> geometry and there is one level. See [Project status](#project-status).

---

## How the game works

### The board

A level defines a hex board of arbitrary shape, plus the **springs** and **hubs**
placed on it. Springs and hubs each occupy a cell of their own, so you cannot build
on them. There is no authored facing: flow enters or leaves through *any* adjacent
conduit that is open towards the endpoint and carries the matching resource, which
means a spring works the same whether it sits on the rim or deep inside the board.

Each of the four resource kinds flows only from its own springs to its own hubs.
Kinds never interconnect, so a route always carries a single resource — water will
not flow down a crystal conduit even when the edges line up.

### Tiles and placement

A **conduit tile** is one resource kind plus a six-bit mask of which of its edges are
open. Edges are indexed 0 to 5 clockwise from due east, the same indexing
`HexCoord.Directions` uses, which is what makes rotating a tile a six-bit rotate and
rotating a coordinate one step along the direction list. A conduit must have at least
two open edges: one opening is a dead end and none is a blank, and neither is
something a player could usefully be dealt.

You always hold exactly one tile, drawn from the level's **tile bag**. The bag deals
a shuffled cycle rather than making independent picks — it deals every tile in its
definition once, reshuffles, and starts again — which bounds droughts. A level
weights its supply simply by repeating a tile in the definition.

A placement is legal when three things hold:

1. the target cell is on the board and empty,
2. it is not occupied by a spring or hub, and
3. the tile, at the rotation you have dialled up, has an open edge facing a
   **neighbour of its own resource kind** that is either an endpoint or an existing
   conduit open on the facing edge.

That third rule is the load-bearing one, and it is a design decision rather than a
deduction. Under free placement you could always drop a tile somewhere, so the only
dead end would be a full board and Pivot Tokens would have nothing to rescue.
Requiring growth from an existing network is what produces the grid congestion and
deadlock risk that the score curve is trading against.

### Scoring

A completed route runs from a conduit adjacent to a matching spring to a conduit
adjacent to a matching hub. It pays:

```
S = S_base * 1.35^(L-1)
```

`L` counts **player-placed conduits only**. Springs and hubs occupy their own cells
and are not tiles, so a single conduit bridging two adjacent endpoints is length 1
and earns the unmultiplied base score. Where a network offers several paths between
the same spring and hub, the shortest is the one reported and paid — a player who
builds a loop cannot claim the longer way round.

Each spring-and-hub pair pays for the **best route it has managed**, and only ever the
difference. Connect a pair the short way for 100, find a longer way later, and the pair pays
what the longer route is worth minus the 100 already taken. Retrieving a conduit and putting
it back pays nothing, because the route is no better than it was.

The rule used to be one payout per pair, ever, at whatever length it first completed. That
turned the ring level into a trap: joining the spring to the hub through the single cell
between them took 100 points and made its 800 target unreachable, with a restart the only way
out — and the Pivot Token the level grants could not rescue it, because retrieving the short
cut did not clear the record. Tokens are still granted on a first completion only, so a route
extended a cell at a time is not a way to farm them.

A level is cleared by reaching its **target score**, and clearing it ends the board. Every
control is withdrawn and one button, centred on screen, moves on. An earlier version let
play continue so that routes could be extended for a bigger score, which meant a finished
level looked exactly like an unfinished one: full bar, live tray, nothing to say it was
over.

### The World Atlas

Every cleared board harvests **Star Essence**: one per base score, so a level with a base of 100
finished on 246 pays two. Essence buys nodes on a constellation reached from the main menu — relics
that add a skip, a Pivot Token, or extra essence to every board from then on. Bonuses are added to
what a board already grants rather than replacing it.

The first region is eight nodes costing 51 essence in total, against at least 77 from clearing the
twenty levels once, so it is reachable by playing rather than by grinding — and a test enforces that
relationship. Nodes are authored under `atlas/` as one line each, and a future biome pack docks onto
the outer edge by adding a file that names the nodes it attaches to.

### Tokens

Every completed route pays out in one of two currencies, so none feels wasted:

| Route length | Reward | What it buys |
|---|---|---|
| 4 or more conduits | 1 **Pivot Token** | Take a conduit back off the board, freeing its cell |
| 1 to 3 conduits | 1 **skip** | Discard the tile in hand and draw the next one |

Neither strategy dominates. Power comes from length; room to manoeuvre comes from
closing early; a player who only ever does one runs short of the other. A retrieved
conduit is **discarded, not returned to hand** — the token buys back the space, not
the tile.

A token frees a cell and nothing else. PRD section 3.2B also allows turning a placed
conduit, and that half is deliberately not implemented: a conduit was placed connected
to something, so turning it in place usually only disconnects it. A player who wants a
different shape in that cell wants the cell back.

To spend a Pivot Token, **tap the remove button** below the pip column on the left: the
pips brighten and every conduit is marked. Then **tap a conduit** to take it off the
board. Arming spends nothing, and a tap anywhere else cancels — the mode exists because
the board is the one thing a thumb touches constantly, and a token is the scarcest thing
a player holds.

Clearing the quota **ends the board**. Every control is withdrawn and one button, centred,
moves on: the next level in the campaign, the next round in Endless.

Unspent Pivot Tokens **travel with you** — to the next campaign level, or the next endless
round, where the board's own allowance acts as a floor rather than a replacement. Without
that, a token earned by the very route that clears a board could never be spent. Skips
travel between endless rounds only; in the campaign each level grants its own three.

If the held tile fits nowhere in any rotation, the board is deadlocked, and a Pivot
Token is the way out. With no tokens left the run is lost and restart is the only
exit; a rewarded video granting a token is the intended softener later.

The tutorial level is tuned around this: its target of 246 with a base of 100 needs
exactly one route of four conduits (`100 * 1.35^3 = 246.04`), which is also the Pivot
Token threshold, so a player who finishes the level earns their first token by doing
so.

---

## Architecture

The repository root is simultaneously a .NET solution and a Unity project. They meet
at exactly one file: a compiled DLL.

| Assembly | Target | Contains | Needs Unity? |
|---|---|---|---|
| `Pathweaver.Core` | `netstandard2.1` | The entire simulation | No |
| `Pathweaver.Core.Tests` | `net10.0`, xUnit | Tests, and the level solver | No |
| `Pathweaver.Game` | Unity | Presentation, input, platform services | Yes |
| `Pathweaver.Game.EditorTests` | Unity, NUnit | EditMode tests | Yes |

### Why the simulation is a separate library

`Pathweaver.Core` holds the hex grid, deterministic seeding, tile model, flow
resolution, scoring, placement rules, deadlock detection, tokens, the save format,
and the level loader. It contains **zero `UnityEngine` references**, by rule and not
by accident.

The payoff is that the whole simulation builds and tests on a plain Linux runner
under `dotnet test` — no Unity, no Editor, no licence, and no device. CI can
therefore gate every rule in the game on every pull request, which would be
impossible if the rules lived in `MonoBehaviour`s. The 362-test suite runs in well
under a second.

Unity supplies presentation and input only. No game rule belongs in `Assets/`. The
flow is one-directional: input asks the session to do something, the session
translates that into a command, the simulation decides whether it is allowed, and the
answer is authoritative. `GameSession` is the only thing that mutates state, and it
does so exclusively through `GameEngine.Apply`. Views read state and never write it.

Input is single-touch through the legacy Input Manager, with a mouse fallback so the
game can be driven in the Editor without a device attached. There are two ways to
place a tile, deliberately: drag it out of the tray and release over a cell, which
shows the tile before committing, or tap a highlighted cell, which avoids the reach a
drag to the top of a large phone demands. Tapping the tray rotates. Which gesture wins
is a question for a real device rather than an argument to have in code.

Because Unity cannot reference a `.csproj`, the simulation is consumed as a **managed
plugin** built into `Assets/Plugins/`. `netstandard2.1` is the only modern target
Unity loads that way, and `LangVersion` is pinned to 9 to match Unity 6's compiler so
the code stays valid if it is ever consumed as source instead. The DLL is a build
output and is not committed — which is why `./scripts/build-core.sh` is the first
thing you run on a fresh clone.

### Immutability

Nothing in the simulation is mutated. `GameEngine.Apply(state, command)` returns the
*next* `GameState`; placing a tile returns a new `HexGrid`; drawing from a `TileBag`
returns the tile alongside the bag that follows; drawing from a `Pcg32` returns a new
generator rather than advancing the old one.

This is not stylistic. It is what makes undo a matter of keeping an earlier value,
makes replay-from-seed safe, and lets the level solver explore thousands of branches
with no unwind logic at all — the parent state simply still exists.

An illegal command **throws** rather than returning the state unchanged. A silent
refusal would let the interface believe a move happened and leave the two out of
step.

### Determinism, and why it is strict

The Daily Expedition must present an identical puzzle to every player who opens the
game on a given date, computed on the device from the date alone with no server call.
Three rules follow, and each is enforced in code:

- **No `System.Random`.** Its algorithm is implementation-defined and has changed
  between .NET versions, so it offers no promise that two devices agree.
  `Determinism/Pcg32.cs` implements PCG32 (XSH-RR) instead, with rejection sampling
  for bounded draws so there is no modulo bias.
- **No `Math.Pow` in scoring.** IEEE 754 permits differing last-bit results for
  transcendental functions across platforms and runtimes, so two devices could
  disagree about a score — unacceptable when players compare results on the same
  puzzle. `Scoring/ScoreTable.cs` precomputes every multiplier once as a scaled
  integer using exact `BigInteger` rational arithmetic, `round(135^(L-1) * 10^6 /
  100^(L-1))`. Repeatedly multiplying by 135/100 was rejected because the compounding
  rounding error lands about 5.5 million scaled units low by length 64.
- **No floating point in the simulation at all.** Axial coordinates keep two integers
  per cell and every hex operation stays in integer arithmetic. There is not a single
  `float` or `double` under `src/`.

Determinism also shapes things that look incidental. Grid enumeration is sorted, so
generation walking the board cannot depend on the order cells were authored in. Edge
masks yield open directions in ascending order, and flow traversal is breadth-first
over that order, so when two paths tie in length the same one is reported on every
device. Legal placements are ordered by cell then rotation. Each subsystem draws from
its own numbered PCG stream (`GridLayout`, `TileBag`, `Objectives`, `Environment`) so
that adding a consumer, or changing how often an existing one draws, cannot shift the
numbers another subsystem sees — without that, a tweak to objective generation would
silently reshuffle every daily puzzle. `<Deterministic>true</Deterministic>` is set
on the project because determinism is a product requirement, not a build nicety.

For the same reason, the Editor version is pinned: an engine upgrade must never alter
simulation output, so re-running the determinism tests is part of the cost of moving.

### Level and save formats

Levels are hand-authored `.pwlevel` files in a line-oriented `key: value` format, not
JSON. `netstandard2.1` ships no JSON reader, and adding a package would mean another
DLL to hand-manage inside Unity and another thing IL2CPP might strip. Every parse
failure names the offending line, because levels are written by hand.

Saves are a versioned binary snapshot (`PWSV`, format version 2, readable back to 1)
written without any serialisation library, keeping `Pathweaver.Core` free of NuGet
dependencies entirely. It is a full snapshot rather than a seed-plus-command-log:
the log would be far smaller and self-validating, but any future rules change would
invalidate every in-progress run, and losing a player's board to an app update is
worse than carrying a few hundred bytes. The tile bag's generator state travels with
the save, because reshuffling on load would make a resumed Daily Expedition diverge
from everyone else's the moment someone suspended the app.

---

## Getting started

### Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.400 | The solution is `.slnx`, which the .NET 10 SDK emits instead of `.sln` |
| Unity | **6000.5.9f1** (Unity 6.5) | Pinned. Android build support modules required |

Unity's version is pinned for two concrete reasons: it offers **Target API Level
36**, which Google Play requires for new submissions, and its splash screen is
disableable on a Personal licence, which the 1.5 second cold-boot target depends on.
An Editor that caps below API 36 cannot ship this game.

```bash
# macOS, Apple Silicon
brew install --cask dotnet-sdk
brew install --cask unity-hub

# Sign in to the Hub once (a free Personal licence is enough), then:
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install \
  --version 6000.5.9f1 \
  --module android android-sdk-ndk-tools android-open-jdk \
  --childModules
```

Without the `android` module there is no Android build target; without the two
sub-modules the Hub provides no SDK/NDK or JDK. There is no `Unity` on `PATH` — the
binary lives at
`/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity`. A useful
alias:

```bash
alias unity='/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity'
```

### First run on a fresh clone

```bash
git clone git@github.com:borlafu/pathweaver.git
cd pathweaver
./scripts/build-core.sh    # DO THIS FIRST
```

**Run `./scripts/build-core.sh` before opening the Unity project.** Unity cannot
reference a `.csproj`, so the simulation is consumed as a compiled plugin, and that
plugin is a build output that is not committed. Until it exists the Unity project
will not compile. The script also copies `levels/*.pwlevel` into
`Assets/Resources/Levels/` as `.txt`, because Unity can only load assets under
`Assets/` and they must be readable synchronously on Android.

Run it again after changing anything under `src/`. Forgetting is the single most
likely cause of Unity reporting that `Pathweaver.Core` cannot be found.

### Working on the simulation

No Unity needed. This is also exactly what CI runs.

```bash
dotnet build Pathweaver.slnx
dotnet test  Pathweaver.slnx
dotnet test  Pathweaver.slnx --collect:"XPlat Code Coverage"
dotnet format Pathweaver.slnx                          # CI fails on unformatted code
```

### Working on the Unity project

Unity holds an exclusive lock on the project while the Editor is open, so every
batch-mode command below fails until you close it. `-runTests` writes results to an
XML file rather than stdout, so pass `-testResults` and read that file.

```bash
# Compile only
unity -batchmode -quit -projectPath . -logFile /tmp/unity.log

# EditMode tests
unity -batchmode -runTests -projectPath . -testPlatform EditMode \
  -testResults /tmp/unity-tests.xml -logFile /tmp/unity.log

# Render a board to a PNG without opening the Editor or a device
unity -batchmode -quit -projectPath . \
  -executeMethod Pathweaver.EditorTools.ProjectBootstrap.CaptureBoardPreview \
  -levelId biome1-01 -output Artifacts/board-preview.png -logFile /tmp/unity.log

# Build an APK
unity -batchmode -quit -projectPath . -buildTarget Android \
  -executeMethod Pathweaver.EditorTools.AndroidBuild.BuildApk \
  -apkOutput Artifacts/pathweaver.apk -logFile /tmp/unity.log
```

The board preview renderer is worth using after any change to the presentation layer.
It caught back-face culling silently hiding every hexagon, which no test would have
noticed.

`Assets/Editor/ProjectBootstrap.cs` holds the project setup steps that were performed
through batch mode — `AddUniversalRenderPipeline`, `ConfigureUniversalRenderPipeline`,
`CreateGameScene` — so configuration is reproducible rather than a sequence of
remembered clicks. `Assets/Editor/AndroidBuild.cs` holds `ConfigurePlayerSettings` and
`BuildApk`.

### Running on a device

```bash
./scripts/deploy.sh              # build core, build APK, install, launch, time cold boot
./scripts/deploy.sh --no-build   # reinstall the existing APK
```

The script force-stops the app before launching so that `am start -W` reports a
genuine cold start, which is the number the 1.5 second budget is measured against. It
uses the `adb` from the Editor install rather than `PATH`, so the tool matches the SDK
the game was built against. The launch activity is
`com.unity3d.player.UnityPlayerGameActivity` — Unity 6 renamed it from
`UnityPlayerActivity`, and the old name fails silently.

### Settings that must not regress

| Setting | Path | Required value |
|---|---|---|
| Target API Level | Player → Other Settings → Identification | 36 or higher |
| Show Splash Screen | Player → Splash Image | unchecked |
| Package Name | Player → Other Settings → Identification | `es.borlafu.pathweaver` |

The application ID is **permanent**: it cannot be changed after the first upload and
cannot be reused even if the app is deleted.

Unity regenerates its own `.sln` and `.csproj` files under the project root. Those are
gitignored; never hand-edit them, and do not raise `LangVersion` or `TargetFramework`
in them, because Unity overwrites those files on every regeneration. The hand-authored
projects under `src/` and `tests/` are tracked via negation rules in `.gitignore`.

---

## Testing

| Suite | Count | Framework | Runs in CI |
|---|---|---|---|
| `Pathweaver.Core.Tests` | **362** tests (291 methods, expanded by `[InlineData]`/`[MemberData]`) | xUnit | Yes |
| `Pathweaver.Game.EditorTests` | **48** cases (45 methods) | NUnit, EditMode | No |

The Core suite covers every part of the simulation: hex coordinates and grids, the
PCG32 generator and date-derived seeding (including golden-value tests that pin exact
outputs so a refactor cannot silently change every daily puzzle), edge masks and tile
rotation, the tile bag, flow resolution, the score table, placement rules, deadlock
detection, tokens, the game engine and state, the save format across versions, and
the level loader.

The Unity suite covers the parts of presentation that are genuinely testable without
a device: the hex-to-world layout maths and its tap-to-cell inverse (2,000
pseudorandom points must all resolve to a cell, which is the test that catches cube
rounding being replaced by naive independent axial rounding), the frame-rate plan, the
haptics patterns, the rotation-hint animation curve, and `SaveService` suspend and
resume against a real temporary directory.

### The level solvability gate

This is the test worth understanding. **Every file under `levels/` must be provably
completable, under five different seeds, or the build fails.**

`tests/Pathweaver.Core.Tests/Solving/LevelSolver.cs` is a depth-first search over
`PlaceTile` commands. It needs no undo logic, because `GameState` is immutable and the
parent state still exists after a branch. Two things make it tractable:

- **Hub-directed move ordering.** Legal placements are tried nearest-first by hex
  distance to a matching hub, tie-broken by the board's stable cell order. Unguided
  DFS could not prove a 37-cell level solvable inside its budget; with ordering it
  finds a route in a fraction of the states.
- **Transposition pruning.** Board signatures are hashed, so different orderings of
  the same placements collapse to one entry and the search explores *positions*
  rather than permutations.

The search is capped at 200,000 explored states so a pathological level cannot hang
CI, and the result distinguishes "not proven solvable within budget" from "proven
unsolvable" — those are different facts and the failure message says which.

`ShippedLevelsTests.cs` then asserts four things:

1. at least one level ships, which guards against every other case passing vacuously;
2. each file parses, and its `id` matches its filename;
3. each level is solvable under **all five** seeds (1, 2, 3, 7, 42) — several seeds,
   because the Daily Expedition derives its seed from the date, so levels get played
   under orderings nobody chose, and one seed would pass a level that only works when
   the tile order is kind;
4. the returned solution, replayed command-by-command through `GameEngine` from a
   fresh game, actually reaches the target score. Trusting the solver's verdict
   without replaying it would let a solver bug certify a level the game cannot clear.

The net effect is that an unsolvable level fails CI instead of reaching a player.

The solver deliberately lives in the test project rather than in `Pathweaver.Core`.
Its only job today is to fail the build, and it carries no weight in the shipped game.
It is expected to move into production when rewarded hints arrive, since a hint is one
move from a solution — paying that cost then is cheaper than shipping an unused search
now.

### CI

`.github/workflows/ci.yml` runs on pushes and pull requests against `main`: restore,
`dotnet format --verify-no-changes` (formatting is a hard gate), a Release build, then
tests with coverage collection uploaded as an artifact.

**CI runs the Core tests only.** There is no Unity job, because there is no Unity
licence on the runner. That is the direct payoff of keeping `UnityEngine` out of the
simulation, and it is also the limitation to remember: the 48 EditMode tests are yours
to run locally before pushing presentation changes.

---

## Project status

Stated plainly, because the gap between "the engine works" and "the game ships" is
where optimism usually hides.

**Done.**

- The simulation is feature-complete for the MVP scope and covered by 362 passing
  tests: hex grid, deterministic seeding, tiles and rotation, the tile bag, flow
  resolution, the exact-integer score curve, placement rules, deadlock detection,
  Pivot Tokens and skips, the command/state engine, versioned binary saves, and the
  level loader.
- A playable vertical slice runs on real Android hardware. You can place and rotate
  tiles with one thumb, complete routes, earn and spend both token types, skip a bad
  draw, restart with confirmation, watch progress against the target, and suspend and
  resume mid-run.
- Platform work is in place: the variable frame-rate governor (up to 120 Hz while
  something is moving, 30 Hz after 1.5 seconds idle, clamped to what the screen
  actually reports), haptics tuned on a real phone, and atomic save writes. The game
  saves after every move and on pause, focus loss, and quit; a corrupt save is
  quarantined and the player gets a fresh board rather than a crash loop.
- The build and deploy path is scripted end to end, and project configuration is
  reproducible through batch-mode Editor entry points rather than remembered clicks.
  The Android build is ARM64, IL2CPP, portrait-only, `minSdkVersion` 24 and
  `targetSdkVersion` 36.

**Placeholder.**

- **All art is procedural geometry generated at runtime.** No artist has touched this
  project and there are no art assets in the repository — no sprites, no textures, no
  models. Hexes, conduit spokes, springs, and hubs are generated meshes with flat
  colours from a palette in code. `Artifacts/board-preview.png` is representative of
  how the game currently looks.
- **There is no font, no UI canvas, and no localisation.** Every interface element —
  buttons, the progress bar, token pips, the level-complete notice — is drawn as a
  generated mesh, because there is no text rendering at all. Nothing in the project
  references `UnityEngine.UI`, TextMeshPro, or a font asset. This is a real
  constraint, not a style: anything that needs to say words cannot currently be built.
- There is no audio.
- The APK is built with `BuildOptions.Development | BuildOptions.AllowDebugging`. It is
  a development build for on-device iteration, not a release configuration, and it is
  not signed for distribution.

**Outstanding.**

- **Content: one level.** `levels/biome1-01.pwlevel` ("First Waters") is the only
  authored level. Phase 1 calls for 20; the full campaign is 120+.
- **Endless Wayfare** — procedural grid generation exists as a design and a reserved
  PCG stream, not as code.
- **The Atlas progression screen**, the constellation map, relic slots, and biome
  packs.
- **Menus** of any kind. The game boots straight into the one level.
- **The Daily Expedition** as a mode. The determinism machinery it depends on is
  built and tested; the mode wrapped around it is not.
- **The rest of the accessibility pass.** Resources now carry a distinct silhouette
  as well as a colour, and the palette is spread across brightness so hue is never the
  only channel — both enforced by tests. Text scaling and reduced motion are untouched,
  and text scaling is blocked on there being any text rendering at all.
- Cloud save delta sync, Google Play Achievements, and telemetry.

**Not published.** Nothing has been uploaded to Google Play. The Play Console account
is personal, which means Production and Open testing stay locked until a closed test
has run with 12 testers opted in continuously for 14 days, followed by a
production-access review. That is wall-clock time no amount of coding removes, so it
has to run in parallel with development rather than after it.

---

## Repository layout

```
PRD.md                            Product requirements — the source of truth for scope
CLAUDE.md                         Toolchain setup, commands, decision log, PRD constraints
Pathweaver.slnx                   .NET solution (the .NET 10 SDK emits .slnx, not .sln)

src/Pathweaver.Core/              The entire simulation. netstandard2.1, no UnityEngine
  Hex/                              HexCoord (axial, integer-only), HexGrid (immutable)
  Determinism/                      Pcg32, SeedSource (date to seed), PathweaverStream
  Tiles/                            EdgeMask, ConduitTile, ResourceKind, TileBag
  Flow/                             FlowResolver, Route, FlowEndpoint
  Scoring/                          ScoreTable — 1.35^(L-1) as exact scaled integers
  Rules/                            PlacementRules, TokenRules, TokenPool, DeadlockDetector
  State/                            GameState, GameCommand, GameEngine
  Save/                             SaveGame — versioned binary snapshot, no dependencies
  Levels/                           LevelDefinition, LevelLoader

tests/Pathweaver.Core.Tests/      362 xUnit tests. net10.0
  Solving/                          LevelSolver and the shipped-level solvability gate

levels/*.pwlevel                  Authored levels. Every one must pass the gate

Assets/Scripts/                   Pathweaver.Game — presentation and input only
  App/                              GameSession (owns state), LevelCatalogue, SaveService
  Presentation/                     BoardView, TileVisual, HexMetrics, HexMeshFactory,
                                    InputController, and the generated-mesh UI views
  Platform/                         FrameRateGovernor, FrameRatePlan, HapticsService
Assets/Tests/                     48 NUnit EditMode cases
Assets/Editor/                    ProjectBootstrap, AndroidBuild, AndroidManifestPatcher
Assets/Scenes/Game.unity          The only scene
Assets/Settings/                  URP asset, 2D renderer, tile material
Assets/Plugins/                   Pathweaver.Core.dll — build output, not committed
Assets/Resources/Levels/          Levels copied from levels/ — build output, not committed

scripts/build-core.sh             Builds the simulation into Assets/Plugins. Run first
scripts/deploy.sh                 Build, install, launch, and time cold boot on a device
.github/workflows/ci.yml          Core build, format check, and tests. No Unity
Packages/, ProjectSettings/       Unity configuration, committed
```

Rendering uses the Universal Render Pipeline with the **2D renderer** (URP 17.5.0).
The 2D renderer is the reason for the choice: it provides the cozy lighting the
product asks for later without reworking materials once real art replaces the
placeholder shapes.
