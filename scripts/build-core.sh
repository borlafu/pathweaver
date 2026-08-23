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
