#!/usr/bin/env bash
# Builds Pathweaver.Core and drops the assembly where Unity can see it.
#
# Unity cannot reference a .csproj, so the simulation is consumed as a managed
# plugin. The DLL is a build output and is not committed: run this after changing
# anything under src/, and before opening the Unity project on a fresh clone.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
destination="$root/Assets/Plugins"

echo "Building Pathweaver.Core (Release)"
dotnet build "$root/src/Pathweaver.Core/Pathweaver.Core.csproj" \
  --configuration Release \
  --nologo \
  --verbosity quiet

mkdir -p "$destination"
cp "$root/src/Pathweaver.Core/bin/Release/netstandard2.1/Pathweaver.Core.dll" "$destination/"

echo "Copied Pathweaver.Core.dll to Assets/Plugins"

# Levels live under levels/ as the authoring source of truth. Unity can only load
# assets under Assets/, and they have to be readable synchronously on Android, so
# they are copied into Resources as .txt for TextAsset import.
levels_source="$root/levels"
levels_destination="$root/Assets/Resources/Levels"

mkdir -p "$levels_destination"
rm -f "$levels_destination"/*.txt

copied=0
for level in "$levels_source"/*.pwlevel; do
  [ -e "$level" ] || continue
  cp "$level" "$levels_destination/$(basename "${level%.pwlevel}").txt"
  copied=$((copied + 1))
done

echo "Copied $copied level(s) to Assets/Resources/Levels"

# Atlas packs follow the same route as levels, and for the same reason: authored under atlas/,
# copied into Resources as .txt so Unity can import them as TextAssets and read them synchronously
# on a device. A new biome pack is a new file here and nothing else.
atlas_source="$root/atlas"
atlas_destination="$root/Assets/Resources/Atlas"

mkdir -p "$atlas_destination"
rm -f "$atlas_destination"/*.txt

packs=0
for pack in "$atlas_source"/*.pwatlas; do
  [ -e "$pack" ] || continue
  cp "$pack" "$atlas_destination/$(basename "${pack%.pwatlas}").txt"
  packs=$((packs + 1))
done

echo "Copied $packs atlas pack(s) to Assets/Resources/Atlas"
