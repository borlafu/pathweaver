#!/usr/bin/env bash
# Builds a signed Android App Bundle for upload to Google Play.
#
# Passwords are read from the macOS Keychain rather than from arguments, a file in the
# repository, or the Editor's saved settings. A password passed on a command line ends up
# in shell history and in any transcript of the session; one entered into Unity's
# Publishing Settings ends up in ProjectSettings.asset, which is committed.
#
# One-time setup, which prompts rather than echoing what you type:
#
#   security add-generic-password -a "$USER" -s pathweaver-keystore-pass -W
#   security add-generic-password -a "$USER" -s pathweaver-key-pass -W
#
# If the two passwords are the same, store the same value under both names.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

UNITY_VERSION="6000.5.9f1"
UNITY="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"

# Overridable so a different machine, or a CI runner, need not match this layout.
KEYSTORE="${PATHWEAVER_KEYSTORE:-$HOME/Library/CloudStorage/Dropbox/DEV/com.borlafu.pathweaver/pathweaver-upload.keystore}"
KEY_ALIAS="${PATHWEAVER_KEY_ALIAS:-}"
OUTPUT="${1:-$root/Artifacts/pathweaver.aab}"

if [ -z "$KEY_ALIAS" ]; then
  echo "Set PATHWEAVER_KEY_ALIAS to the key alias inside the keystore." >&2
  echo "List it with:" >&2
  echo "  /Applications/Unity/Hub/Editor/${UNITY_VERSION}/PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool -list -keystore \"$KEYSTORE\"" >&2
  exit 1
fi

if [ ! -f "$KEYSTORE" ]; then
  echo "No keystore at $KEYSTORE" >&2
  exit 1
fi

read_secret() {
  local service="$1"
  if ! security find-generic-password -a "$USER" -s "$service" -w 2>/dev/null; then
    echo "No Keychain entry \"$service\". Create it with:" >&2
    echo "  security add-generic-password -a \"\$USER\" -s $service -W" >&2
    return 1
  fi
}

KEYSTORE_PASS="$(read_secret pathweaver-keystore-pass)"
KEY_PASS="$(read_secret pathweaver-key-pass)"

echo "== Building the simulation plugin"
"$root/scripts/build-core.sh"

echo "== Building a signed bundle (several minutes)"
mkdir -p "$(dirname "$OUTPUT")"

# Exported rather than passed as arguments, so they do not appear in the process list.
export PATHWEAVER_KEYSTORE="$KEYSTORE"
export PATHWEAVER_KEYSTORE_PASS="$KEYSTORE_PASS"
export PATHWEAVER_KEY_ALIAS="$KEY_ALIAS"
export PATHWEAVER_KEY_PASS="$KEY_PASS"

"$UNITY" -batchmode -quit \
  -projectPath "$root" \
  -buildTarget Android \
  -executeMethod Pathweaver.EditorTools.AndroidBuild.BuildAab \
  -aabOutput "$OUTPUT" \
  -logFile /tmp/pathweaver-release.log

echo "   $(du -h "$OUTPUT" | cut -f1) at $OUTPUT"

echo "== Checking 16 KB page alignment"
# Google Play requires native libraries aligned to 16 KB for new apps. No Unity setting
# controls it: the NDK and Gradle plugin produce it, which means a future toolchain change
# could silently take it away. Play would then reject the upload with a message that says
# nothing about why, so it is verified here instead.
readelf="$(find "/Applications/Unity/Hub/Editor/${UNITY_VERSION}/PlaybackEngines/AndroidPlayer/NDK" -name llvm-readelf 2>/dev/null | head -1)"
if [ -z "$readelf" ]; then
  echo "   WARNING: llvm-readelf not found; alignment unverified" >&2
else
  workdir="$(mktemp -d)"
  unzip -o -q "$OUTPUT" 'base/lib/*/*.so' -d "$workdir"

  misaligned=0
  for lib in "$workdir"/base/lib/*/*.so; do
    [ -e "$lib" ] || continue
    align="$("$readelf" -lW "$lib" | awk '/LOAD/ {print $NF; exit}')"
    if [ "$align" != "0x4000" ]; then
      echo "   $(basename "$lib") is aligned $align, not 0x4000" >&2
      misaligned=$((misaligned + 1))
    fi
  done

  rm -rf "$workdir"

  if [ "$misaligned" -gt 0 ]; then
    echo "   FAILED: $misaligned library/libraries are not 16 KB aligned; Play will reject this" >&2
    exit 1
  fi

  echo "   every native library is 16 KB aligned"
fi

echo "== Checking nothing secret leaked into the project"
if git -C "$root" diff --quiet -- ProjectSettings/ProjectSettings.asset; then
  echo "   ProjectSettings unchanged"
else
  echo "   WARNING: ProjectSettings.asset changed. Inspect it before committing:" >&2
  echo "     git diff ProjectSettings/ProjectSettings.asset | grep -i pass" >&2
fi

echo
echo "Verify the signature and compare the fingerprint against Play Console:"
echo "  # apksigner reads APKs, not bundles, so a bundle is checked with jarsigner"
echo "  /Applications/Unity/Hub/Editor/${UNITY_VERSION}/PlaybackEngines/AndroidPlayer/OpenJDK/bin/jarsigner -verify -verbose:summary -certs \"$OUTPUT\" | head -20"
