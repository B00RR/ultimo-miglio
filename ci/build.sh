#!/usr/bin/env bash
# Ultimo Miglio - headless Unity tasks. Usage:
#   ci/build.sh setup|scene|test|dev|apk|aab|all|still|video [seconds] [name]
# Env: UNITY (editor binary), UM_KEYSTORE/UM_KEYSTORE_PASS/UM_KEY_ALIAS/UM_KEY_PASS (release signing)
set -euo pipefail
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/home/box/Unity/Hub/Editor/6000.3.26f1/Editor/Unity}"
mkdir -p Logs Builds Captures
run() { local log="Logs/$1.log"; shift; echo ">> $*"; "$UNITY" -batchmode -projectPath "$PWD" -buildTarget Android -logFile "$log" "$@" || { tail -50 "$log"; exit 1; }; echo "log: $log"; }
X="xvfb-run -a"
case "${1:-all}" in
  setup) run setup -nographics -quit -executeMethod UltimoMiglio.EditorTools.ProjectSetup.Apply ;;
  scene) run scene -nographics -quit -executeMethod UltimoMiglio.EditorTools.SliceSceneBuilder.Build ;;
  test)  run test -nographics -runTests -testPlatform EditMode -testResults "$PWD/Logs/editmode-results.xml"; echo "results: Logs/editmode-results.xml" ;;
  dev)   run build_dev -nographics -executeMethod UltimoMiglio.EditorTools.BuildScript.BuildDevApk ;;
  apk)   run build_apk -nographics -executeMethod UltimoMiglio.EditorTools.BuildScript.BuildApk ;;
  aab)   run build_aab -nographics -executeMethod UltimoMiglio.EditorTools.BuildScript.BuildAab ;;
  all)   run build_all -nographics -executeMethod UltimoMiglio.EditorTools.BuildScript.BuildAll ;;
  still) UNITY="$X $UNITY" run still -quit -executeMethod UltimoMiglio.EditorTools.CaptureScript.Still -captureName "${3:-still}" ;;
  video) UNITY="$X $UNITY" run video -executeMethod UltimoMiglio.EditorTools.CaptureScript.Video -captureSeconds "${2:-20}" -captureName "${3:-milestone}" ;;
  *) echo "unknown: $1"; exit 2 ;;
esac
ls -la Builds Captures 2>/dev/null | tail -n +1
