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
silently hiding every hexagon, which no test would have noticed.

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
| Does clearing the quota end the board? | No. Extending routes is rewarded, so play continues and the completion notice is dismissable. |
| Is a retrieved conduit returned to hand? | No, it is discarded. The token buys back the space, not the tile. |

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
