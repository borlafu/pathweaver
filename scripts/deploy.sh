#!/usr/bin/env bash
# Builds, installs, launches, and times the game on a connected Android device.
#
# adb and the Unity Editor are used from the Unity install rather than from PATH, so
# the versions match what the game is built against.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

UNITY_VERSION="6000.5.9f1"
UNITY="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
ADB="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"

APPLICATION_ID="es.borlafu.pathweaver"
ACTIVITY="com.unity3d.player.UnityPlayerGameActivity"
APK="$root/Artifacts/pathweaver.apk"

skip_build=false
if [ "${1:-}" = "--no-build" ]; then
  skip_build=true
fi

if [ ! -x "$ADB" ]; then
  echo "adb not found at $ADB" >&2
  exit 1
fi

if ! "$ADB" devices | grep -qE "\sdevice$"; then
  echo "No authorised device. Plug in a phone, enable USB debugging, and accept the prompt." >&2
  "$ADB" devices >&2
  exit 1
fi

if [ "$skip_build" = false ]; then
  echo "== Building the simulation plugin"
  "$root/scripts/build-core.sh"

  echo "== Building the APK (several minutes on a cold cache)"
  "$UNITY" -batchmode -quit \
    -projectPath "$root" \
    -buildTarget Android \
    -executeMethod Pathweaver.EditorTools.AndroidBuild.BuildApk \
    -apkOutput "$APK" \
    -logFile /tmp/pathweaver-build.log

  echo "   $(du -h "$APK" | cut -f1) at $APK"
fi

echo "== Installing"
"$ADB" install -r "$APK" | tail -1

echo "== Launching"
# Force-stopping first makes the launch a cold start, which is the number the 1.5
# second budget in PRD section 1.2 refers to.
"$ADB" shell am force-stop "$APPLICATION_ID"
"$ADB" shell am start -W -n "$APPLICATION_ID/$ACTIVITY" | grep -E "Status|LaunchState|TotalTime"

echo
echo "Logs:        $ADB logcat -s Unity"
echo "Screenshot:  $ADB exec-out screencap -p > /tmp/pathweaver.png"
echo "Wipe save:   $ADB shell run-as $APPLICATION_ID rm -rf /data/data/$APPLICATION_ID/files"
