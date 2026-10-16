#!/usr/bin/env bash
# Build WebGL di Ultimo Miglio in batchmode (compatibile GitHub Pages).
# Uso: tools/build_webgl.sh [cartella_output]   (default: Builds/WebGL)
# Env: UNITY (binario editor), UM_DEV=1 (build development), UM_SKIP_SCENE=1 (non rigenera la scena)
# Exit code: 0 build riuscita, 1 build fallita, 2 errore di configurazione.
set -euo pipefail
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/home/box/Unity/Hub/Editor/6000.3.26f1/Editor/Unity}"
# Default: metodo ufficiale WebGLMinBuild.Build (output fisso Builds/WebGL_min).
# UM_METHOD=ci usa WebGLBuild.BuildCI con cartella personalizzabile.
if [ "${UM_METHOD:-min}" = ci ]; then OUT="${1:-Builds/WebGL}"; METHOD=UltimoMiglio.EditorTools.WebGLBuild.BuildCI; else OUT=Builds/WebGL_min; METHOD=UltimoMiglio.EditorTools.WebGLMinBuild.Build; fi
LOG="Logs/webgl_build.log"
[ -x "$UNITY" ] || { echo "ERRORE: editor Unity non trovato: $UNITY" >&2; exit 2; }
if pgrep -f "Unity.*-projectPath $PWD" >/dev/null 2>&1; then
  echo "ERRORE: c'è già un'istanza Unity aperta su questo progetto. Chiudila prima." >&2; exit 2; fi
mkdir -p Logs
EXTRA=()
[ "${UM_DEV:-0}" = 1 ] && EXTRA+=(-development)
[ "${UM_SKIP_SCENE:-0}" = 1 ] && EXTRA+=(-skipSceneRebuild)
echo ">> Build WebGL -> $OUT (log: $LOG)"
set +e
"$UNITY" -batchmode -nographics -quit -projectPath "$PWD" -buildTarget WebGL \
  -executeMethod "$METHOD" -outputPath "$OUT" \
  -logFile "$LOG" "${EXTRA[@]}"
RC=$?
set -e
grep -E "\[WebGL(Build|Min)\]" "$LOG" || true
if [ $RC -ne 0 ] || [ ! -f "$OUT/index.html" ]; then
  echo "BUILD FALLITA (exit $RC). Ultime righe del log:" >&2; tail -40 "$LOG" >&2; exit 1; fi
echo "BUILD OK: $(du -sh "$OUT" | cut -f1) in $OUT"
