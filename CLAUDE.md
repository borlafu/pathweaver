# Pathweaver: Pocket Realms

Offline-first hex route-building puzzle game for Android. Unity 2D (URP), C#.
See PRD.md for the full product spec.

## Status

`Pathweaver.Core` and its test project exist and build clean. No Unity project
has been scaffolded yet — no `Assets/` or `ProjectSettings/`, so the Unity
commands below do not work yet.

Toolchain: .NET SDK 10.0.400 installed. Unity Hub installed, but no Editor
version yet. See Toolchain setup.

## Layout

```
Pathweaver.slnx                 solution (.NET 10 SDK emits .slnx, not .sln)
src/Pathweaver.Core/            netstandard2.1, no UnityEngine references
tests/Pathweaver.Core.Tests/    net10.0, xUnit
```

`Pathweaver.Core` holds the whole simulation: hex grid, deterministic seeding,
flow resolution, scoring. It must stay free of `UnityEngine` references so it
runs under `dotnet test` and in CI without an Editor or a license. Unity will
consume it as a managed plugin and supply only presentation and input.

## Toolchain setup (macOS, Apple Silicon)

Verified on macOS 26.5.2, arm64, Homebrew 6.0.17 at `/opt/homebrew`.

### Unity

Unity is installed in two steps: the Hub, then an Editor version through the Hub.

```bash
brew install --cask unity-hub
```

Then sign in to the Hub once (a free Personal license is enough for this
project) and install an Editor. Prefer the current Unity 6 LTS release. List
what is available before picking a version:

```bash
alias unityhub='"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" --'
unityhub --headless editors --releases
```

Install the Editor with the Android build support modules. Without the
`android` module there is no Android build target, and without the two
sub-modules the Hub will not provide an SDK/NDK or JDK:

```bash
unityhub --headless install \
  --version <version-from-the-list-above> \
  --module android android-sdk-ndk-tools android-open-jdk \
  --childModules
```

The Editor lands at:

```
/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity
```

That binary is the `Unity` CLI referenced under Commands. Add it to PATH or
alias it — there is no `Unity` on PATH by default.

Choose the Apple Silicon Editor build, not Intel. The Intel build runs under
Rosetta and is markedly slower for iterative work.

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

Core library — works today, no Unity needed:

- Build: `dotnet build Pathweaver.slnx`
- Test: `dotnet test Pathweaver.slnx`
- Coverage: `dotnet test Pathweaver.slnx --collect:"XPlat Code Coverage"`
- Format: `dotnet format Pathweaver.slnx`

Unity generates its own `.sln` and `.csproj` files under the project root when
an `Assets/` folder exists. Those are gitignored; never hand-edit them. The
hand-authored `src/` and `tests/` projects are tracked, via negation rules in
`.gitignore`.

Unity — requires an installed Editor:

- EditMode tests: `Unity -runTests -batchmode -projectPath . -testPlatform EditMode`
- PlayMode tests: `Unity -runTests -batchmode -projectPath . -testPlatform PlayMode`
- Android build: `Unity -quit -batchmode -projectPath . -executeMethod BuildScript.BuildAndroid`

Unity holds an exclusive lock on the project while the Editor is open; batchmode
commands fail until it is closed.

`-runTests` writes results to an XML file rather than stdout. Pass
`-testResults <path>` and read that file to see which tests failed.

## Hard constraints from the PRD

- Offline-first: no gameplay path may require network access. Cloud save is an
  async delta sync on top of local SQLite, never a dependency.
- Cold boot under 1.5s to interactive main menu.
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
