# Pathweaver: Pocket Realms

Offline-first hex route-building puzzle game for Android. Unity 2D (URP), C#.
See PRD.md for the full product spec.

## Status

The simulation is complete and covered by tests. A playable vertical slice runs on
hardware: a level renders, places tiles one-thumb, scores routes, and resumes after
a force-quit. Art is placeholder generated geometry, and there is no font or UI
canvas, so every interface element is a mesh.

Toolchain is ready: .NET SDK 10.0.400, and Unity **6000.5.9f1** (Unity 6.5) with
the Android modules. See Toolchain setup.

## First thing on a fresh clone

Unity cannot reference a `.csproj`, so the simulation is consumed as a compiled
plugin. That plugin is a build output and is not committed, so the Unity project
will not compile until it exists:

```bash
./scripts/build-core.sh
```

Run it again after changing anything under `src/`. Forgetting is the most likely
cause of Unity reporting that `Pathweaver.Core` cannot be found.

## Release identity

| | |
|---|---|
| Application ID | `es.borlafu.pathweaver` |
| Play Console account | `borlafu`, **personal** |
| Distribution | Free, no in-app purchases in the MVP |

### Signing

| | |
|---|---|
| Upload key | held locally, outside this repository; alias `pathweaver-upload` |
| App signing key | held by Google under Play App Signing, with post-quantum keys enrolled |
| App signing certificate SHA-256 | `FC:92:61:74:17:B8:46:7E:8F:D5:AE:9B:2B:4E:12:60:6C:92:0F:E5:C0:0C:BB:87:07:66:31:C8:6D:3F:C7:0E` |

The two keys are different on purpose, and that difference is what makes a mistake survivable: you
sign uploads with the upload key, Google re-signs with the app signing key before delivery, and a lost
or compromised upload key can be reset. The app signing certificate is public — fingerprints are
published in `.well-known` files by design — and Play Games Services or any API integration will ask
for the value above.

Release builds read the passwords from the macOS Keychain via `scripts/build-release.sh`. Never enter
them in the Editor: Unity writes keystore passwords into `ProjectSettings.asset`, which is committed.

There is no `assetlinks.json` and no need for one. Digital Asset Links proves domain ownership so that
`https://` links open in the app rather than a browser; this game is offline-first with no website and
no deep links, so there is nothing to verify and nowhere to host it. If a site ever exists, the file
goes at `https://<domain>/.well-known/assetlinks.json` with the app signing fingerprint above.

The application ID is **permanent**. It cannot be changed after the first upload
and cannot be reused even if the app is deleted, so it must match exactly in
Unity's Player Settings (Other Settings → Identification → Package Name).

Because the account is personal, Production and Open testing stay locked until a
closed test has run with 12 testers opted in continuously for 14 days, followed
by a production-access review. That is wall-clock time no amount of coding
removes, so it runs in parallel with development.

## Layout

The repository root is both the .NET solution and the Unity project.

```
Pathweaver.slnx                 solution (.NET 10 SDK emits .slnx, not .sln)
src/Pathweaver.Core/            netstandard2.1, no UnityEngine references
tests/Pathweaver.Core.Tests/    net10.0, xUnit, and the level solvability gate
levels/*.pwlevel                authored levels, verified solvable by CI
atlas/*.pwatlas                 World Atlas packs, verified reachable by CI
scripts/build-core.sh           builds the simulation into Assets/Plugins

Assets/Scripts/                 Pathweaver.Game assembly — presentation only
Assets/Plugins/                 Pathweaver.Core.dll, built, not committed
Assets/Settings/                URP asset and 2D renderer
Assets/Editor/                  command-line project setup
Packages/, ProjectSettings/     Unity configuration, committed
```

`Pathweaver.Core` holds the whole simulation: hex grid, deterministic seeding,
flow resolution, scoring, placement rules, save format, level loading. It must
stay free of `UnityEngine` references so it runs under `dotnet test` and in CI
without an Editor, a licence, or a device. Unity supplies presentation and input
only — no game rule belongs in `Assets/`.

Rendering is the Universal Render Pipeline with the **2D renderer** (URP 17.5.0),
per PRD section 5.1. The 2D renderer is the reason for the choice: it gives the
cozy lighting the PRD asks for later without reworking materials once real art
replaces the placeholder shapes.

## Toolchain setup (macOS, Apple Silicon)

Verified on macOS 26.5.2, arm64, Homebrew 6.0.17 at `/opt/homebrew`.

### Unity — pinned to 6000.5.9f1

This project builds on **Unity 6000.5.9f1** (Unity 6.5), installed at:

```
/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity
```

That is the `Unity` binary the commands below refer to; there is no `Unity` on
PATH by default.

The version is pinned rather than merely current, for two reasons:

- It offers **Target API Level 36**, which Google Play requires for new
  submissions from 31 August 2026. It also offers 37.0. Any Editor that caps
  below 36 cannot ship this game.
- Its splash screen is disableable on a Personal licence, which the 1.5 second
  cold-boot target depends on.

Do not upgrade the Editor mid-project without re-checking both, and without
re-running the determinism tests — an engine change must never alter simulation
output.

To reproduce the install from scratch:

```bash
brew install --cask unity-hub
```

Sign in to the Hub once (a free Personal licence suffices), then install the
pinned Editor with the Android build support modules. Without the `android`
module there is no Android build target, and without the two sub-modules the Hub
provides no SDK/NDK or JDK:

```bash
alias unityhub='"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" --'
unityhub --headless install \
  --version 6000.5.9f1 \
  --module android android-sdk-ndk-tools android-open-jdk \
  --childModules
```

A convenient alias for the pinned Editor:

```bash
alias unity='/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity'
```

Choose the Apple Silicon Editor build, not Intel. The Intel build runs under
Rosetta and is markedly slower for iterative work.

### Player settings that must not regress

Both were verified on 6000.5.9f1 and are load-bearing for PRD targets:

| Setting | Path | Required value |
|---|---|---|
| Target API Level | Player → Other Settings → Identification | 36 or higher |
| Show Splash Screen | Player → Splash Image | unchecked |

### .NET SDK

Unity compiles C# with its own bundled Roslyn toolchain, so the .NET SDK is
**not** required to build or run the game. It is required for `dotnet format`
and other CLI tooling that operates on the Unity-generated solution.

```bash
brew install --cask dotnet-sdk
dotnet --version
```

The Unity-generated `.csproj` files target a specific C#/framework version set
by the Editor. Do not raise `LangVersion` or `TargetFramework` in them — Unity
overwrites those files on every regeneration.

## Commands

Core library — no Unity needed, and what CI runs:

- Build: `dotnet build Pathweaver.slnx`
- Test: `dotnet test Pathweaver.slnx`
- Coverage: `dotnet test Pathweaver.slnx --collect:"XPlat Code Coverage"`
- Format: `dotnet format Pathweaver.slnx`
- Plugin: `./scripts/build-core.sh` — required before Unity will compile

The test suite includes the level solvability gate: every file under `levels/` must
load and be completable under several seeds, so an unsolvable level fails CI rather
than reaching a player.

Batch operations on the Unity project, useful because they need no GUI:

```bash
unity -batchmode -quit -projectPath . -logFile /tmp/unity.log            # compile only
unity -batchmode -quit -projectPath . -executeMethod <Type>.<Method> ...  # run setup code
```

`Assets/Editor/ProjectBootstrap.cs` holds the setup steps that were run through
that route, so project configuration is reproducible rather than a sequence of
remembered clicks.

Rendering can be reviewed without opening the Editor or a device:

```bash
unity -batchmode -quit -projectPath . \
  -executeMethod Pathweaver.EditorTools.ProjectBootstrap.CaptureBoardPreview \
  -levelId biome1-01 -output Artifacts/board-preview.png -logFile /tmp/unity.log
```

Worth using after any change to the presentation layer. It caught back-face culling
silently hiding every hexagon — which a test now would notice, because
`Assets/Tests/MeshWindingTests.cs` compares every generated mesh against the winding of
the one known-good hexagon.

Animation cannot be judged from one still, but it can be judged from four. Pass
`-pulsePhase` to freeze the endpoint pulses anywhere in their cycle:

```bash
for phase in 0 0.25 0.5 0.75; do
  unity -batchmode -quit -projectPath . \
    -executeMethod Pathweaver.EditorTools.ProjectBootstrap.CaptureBoardPreview \
    -levelId biome1-06 -pulsePhase $phase -output Artifacts/pulse-$phase.png \
    -logFile /tmp/unity.log
done
```

That the motion is a pure function of a phase — `EndpointPulse`, `FlowPulse` — is what makes
this possible, and is why those two are separate from the components that apply them.

The store icon and feature graphic come from the same route — the game's own cells and
palette, so the listing cannot advertise something the game does not look like:

```bash
unity -batchmode -quit -projectPath . \
  -executeMethod Pathweaver.EditorTools.StoreArt.Capture -logFile /tmp/unity.log
```

Writes `Artifacts/store/icon-512.png` and `Artifacts/store/feature-1024x500.png`. Both
are build outputs rather than committed assets. Listing text lives in
`docs/store/listing.md`, and the store screenshots — which come from a device session
rather than a command — are committed under `docs/store/screenshots/`.

Screenshots need a build without Unity's "Development Build" watermark, which
`./scripts/deploy.sh` cannot give. Pass `-development false` to `AndroidBuild.BuildApk`
for one: it drops the profiler and the watermark but keeps Unity's debug key, so it
installs over the everyday build without uninstalling and without touching saves. The
resizing the Play Console demands is recorded in `docs/store/listing.md`.

Unity tests, which CI cannot run because it has no licence:

```bash
unity -batchmode -runTests -projectPath . -testPlatform EditMode \
  -testResults /tmp/unity-tests.xml -logFile /tmp/unity.log
```

Results go to the XML file rather than stdout.

Unity generates its own `.sln` and `.csproj` files under the project root when
an `Assets/` folder exists. Those are gitignored; never hand-edit them. The
hand-authored `src/` and `tests/` projects are tracked, via negation rules in
`.gitignore`.

Unity — these work today:

- EditMode tests: `Unity -runTests -batchmode -projectPath . -testPlatform EditMode`
- PlayMode tests: `Unity -runTests -batchmode -projectPath . -testPlatform PlayMode`
- Android build: `./scripts/deploy.sh`, or directly
  `Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod Pathweaver.EditorTools.AndroidBuild.BuildApk -apkOutput Artifacts/pathweaver.apk`

On-device checks once a build exists:

```bash
./scripts/deploy.sh              # build, install, launch, and report cold boot
./scripts/deploy.sh --no-build   # reinstall the existing APK
```

The launch activity is `com.unity3d.player.UnityPlayerGameActivity`. Unity 6 renamed
it from `UnityPlayerActivity`; the old name silently fails to start.

`adb` is not on PATH. Use the one from the Editor install, which matches the SDK the
game is built against:

```
/Applications/Unity/Hub/Editor/6000.5.9f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
```

The `am start -W` output carries the cold-boot timing the 1.5 second budget is
measured against, and `deploy.sh` force-stops first so the measurement is a genuine
cold start.

Unity holds an exclusive lock on the project while the Editor is open; batchmode
commands fail until it is closed.

`-runTests` writes results to an XML file rather than stdout. Pass
`-testResults <path>` and read that file to see which tests failed.

## Design decisions the PRD leaves open

Recorded here because they are choices rather than deductions, and the reasoning is easy to
lose.

| Question | Decision |
|---|---|
| Where may a tile be placed? | It must join a conduit or endpoint of its own kind with facing edges. Free placement would reduce deadlock to "board full" and leave Pivot Tokens nothing to rescue. |
| What earns a Pivot Token? | Completing a route of 4 or more conduits, rewarding the extended routing the score curve already pushes toward. |
| What happens on a deadlock with no tokens? | The run is lost, and restart is the only way out. A rewarded video granting a token is the intended softener later, alongside the rewarded hints in PRD section 6.2. |
| Does clearing the quota end the board? | Yes. Every control is withdrawn and a single button, centred, moves on to the next level or the next endless round. This reverses the earlier answer — play used to continue so routes could be extended for a bigger score — because a finished board with a full quota bar and a live tray reads as unfinished. A token earned by the clearing route is not lost: campaign levels carry Pivot Tokens between them, as endless rounds do. |
| Is a retrieved conduit returned to hand? | No, it is discarded. The token buys back the space, not the tile. |
| Does a pair pay more than once? | It pays for the best route it has managed, and only the difference. The original rule — one payout per pair at whatever length it first completed — made `biome1-17` a trap: the one-conduit short cut across the ring took 100 and put the 800 target out of reach for good, and retrieving the short cut did not clear the record, so the Pivot Token could not rescue it either. Reported from a device. Tokens are still granted on a first completion only, or extending a route a cell at a time would farm them. |
| Why do springs and hubs breathe? | A spring's ring grows from its centre to its rim and a hub's collapses inward, which is the "radiating versus converging silhouette" the art guide asks for in section 9. The motion is therefore an accessibility channel that happens to look alive, not decoration that happens to be accessible. The old edge marks — a star on a spring, a bar across a hub — were removed with it: two signals for one fact left the cell cluttered, and the marks were the weaker one. Reduced motion rests the ring open on a spring and closed on a hub rather than hiding it, so the role survives without either colour or movement. |
| Why does animation keep running when nothing is happening? | Because a board with a dead spring reads as frozen. It runs at the 30 Hz the frame governor already drops to after 1.5 seconds idle, and no animator calls `NotifyActivity` — doing so would pin the active rate for as long as the game is open and spend battery on a board nobody is touching. Pulse periods are kept slow enough (1.8 s, 54 frames) that 30 Hz does not look stepped; the fix for a stepped pulse is never to raise the idle rate. |
| What does a Pivot Token do? | It takes a conduit off the board, and nothing else. PRD section 3.2B also allows turning a placed conduit; that half is dropped, because a conduit was placed connected to something and turning it in place usually only disconnects it. `PivotRotate` was implemented, tested, and then deleted rather than left as dead code. |
| How is a Pivot Token spent? | Tap the remove button under the pip column to arm it, then tap a conduit to retrieve it. Arming is a mode rather than a bare tap on a conduit, because the board is the one thing a thumb touches constantly and a token is the scarcest thing the player holds. Arming spends nothing, and a tap anywhere else cancels. |
| What does the World Atlas cost and pay? | Star Essence, one per base score harvested on any cleared board, in both modes. Nodes are relics — extra skips, extra Pivot Tokens, extra essence per clear — and are additive on top of a board's own allowance, because an upgrade that replaced it would make a generous level worse than a mean one. The whole first region costs 51 and clearing the twenty levels once pays at least 77, a relationship CI checks. |
| How does a future biome pack dock on? | It adds a file under `atlas/` with its own `pack:` line and a `docks:` line naming the nodes it attaches to. Nothing already shipped changes. The `docks:` declaration exists because otherwise a prerequisite living in another file is indistinguishable from a typo, and one of the two has to be an error. |
| Do tokens survive the end of a board? | Yes. Endless rounds carry both Pivot Tokens and skips; campaign levels carry Pivot Tokens, in `CampaignProgress`. In both cases the board's own allowance is a floor rather than a replacement. They are earned, so taking them back at a boundary is taking back a reward — and since clearing a board ends it, a token earned by the clearing route would otherwise be unspendable. A fresh endless run keeps none. Campaign skips are not carried: three per level is an allowance rather than a reward, and every authored level is solvable within its own. |
| How does a player start over? | Settings holds one destructive control, below the two switches and smaller than them. It arms on the first tap and erases on the second, because there is no font to write a confirmation question in; armed, it turns red **and** gains a ring, since colour may never carry a fact on its own here. Every other tap on the screen — including a tap on nothing — disarms it. `ProgressReset.Wipe` is the single place that knows what "all progress" means: cleared levels and carried tokens, the World Atlas, the endless run, and every board save including generated endless rounds and quarantined files. Files are deleted rather than blanked, so the game reads its own storage exactly as it does on a first launch. |
| Why is the World Atlas not on the main menu? | Because it works and explains nothing. Star Essence, a node's cost, and what a relic actually does are all numbers the game has no font to write, so a player meets a constellation of coloured hexagons and guesses. `MainMenuView.IsAtlasVisible` withholds it for the closed test; `AwardEssence` keeps paying on every clear while it is false, so essence banks up and the wait costs a player nothing. It returns once there is text to explain it with, which is why the flag is one property rather than a deletion. |
| How many tokens may a player hold? | Three of each, rising to at most five as atlas relics are unlocked — `TokenRules.BaseCapacity` and `MaximumCapacity`, with `CapacityWith` doing the arithmetic. Earning into a full pool pays nothing rather than being refused, so a full pool cannot make a board unplayable; it is simply the pressure to spend. Both counts used to accumulate without limit while the pip column showed three, so a player reported holding six of something the game claimed a maximum of three of. The ceiling travels with the pool because it is not one number for the game: a relic raises the hand *and* the room to hold it, or the extra token it deals would vanish. An authored level may not deal more than the base ceiling, since a level file cannot assume relics the player may not have. |

## Hard constraints from the PRD

- Offline-first: no gameplay path may require network access. Cloud save is an
  async delta sync on top of local SQLite, never a dependency.
- Cold boot under 1.5s to interactive main menu. Achievable: the engine splash
  screen is disableable on this Editor and licence.
- APK/AAB under 85 MB.
- Deterministic generation: the Daily Expedition seed derives from device date
  only, and must produce an identical grid on every device with no server call.
- Variable frame rate: 120/90 Hz during drag and animation, 30 Hz when idle.
- No energy gates, no gacha, no forced or unskippable ads.

## Architecture notes

- Persistence: local SQLite with encrypted binary serialization for run state.
- Progression is a non-linear constellation map. Biome packs dock onto the outer
  edge and must not require rebalancing earlier biomes.
- Score formula: S = S_base * 1.35^(L-1), where L is route tile length.
