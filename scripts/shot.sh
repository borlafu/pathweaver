#!/usr/bin/env bash
# Captures the phone's screen, but only if the game is the focused window.
#
# This exists because it is a mistake worth making impossible. Driving the device with
# `adb shell input tap` and capturing the result assumes the game is in front; when it is not, the taps
# go to whatever is, and the capture is a picture of somebody's phone rather than of the board. That
# happened twice while taking store screenshots, once because a stray swipe opened the recents view and
# once because a launch silently failed while another app was open.
#
# So the check is the tool rather than a habit: no capture without focus, and a clear refusal otherwise.
#
# Usage:
#   ./scripts/shot.sh Artifacts/shots/whatever.png
set -euo pipefail

UNITY_VERSION="6000.5.9f1"
ADB="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
PACKAGE="es.borlafu.pathweaver"

output="${1:-}"
if [ -z "$output" ]; then
  echo "Usage: $0 <output.png>" >&2
  exit 2
fi

if [ ! -x "$ADB" ]; then
  echo "adb not found at $ADB" >&2
  exit 1
fi

focus="$("$ADB" shell dumpsys window 2>/dev/null | grep -m1 mCurrentFocus || true)"

if ! printf '%s' "$focus" | grep -q "$PACKAGE"; then
  echo "Refusing to capture: the game is not the focused window." >&2
  echo "  focus: ${focus:-unknown}" >&2
  echo "Launch it first, and check the phone is not in somebody's hand." >&2
  exit 1
fi

mkdir -p "$(dirname "$output")"
"$ADB" exec-out screencap -p > "$output"

echo "wrote $output"
