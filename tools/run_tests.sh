#!/usr/bin/env bash
# Esegue i test Unity in batchmode e produce XML NUnit in Logs/.
# Uso: tools/run_tests.sh [editmode|playmode|all]   (default: all)
# Env: UNITY (binario editor)
# Output: Logs/editmode-results.xml, Logs/playmode-results.xml, Logs/test-summary.txt
# Exit code: 0 tutti verdi, 1 almeno un test fallito/errore, 2 errore di configurazione.
set -uo pipefail
cd "$(dirname "$0")/.."
UNITY="${UNITY:-/home/box/Unity/Hub/Editor/6000.3.26f1/Editor/Unity}"
[ -x "$UNITY" ] || { echo "ERRORE: editor Unity non trovato: $UNITY" >&2; exit 2; }
mkdir -p Logs
: > Logs/test-summary.txt
FAIL=0
run() { # $1 = EditMode|PlayMode
  local p="$1" lc; lc=$(echo "$p" | tr A-Z a-z)
  local xml="$PWD/Logs/${lc}-results.xml" log="Logs/${lc}.log"
  rm -f "$xml"
  echo ">> Test $p (log: $log)"
  local gfx=(-nographics); [ "$p" = PlayMode ] && gfx=()   # PlayMode: grafica richiesta da alcune scene
  "$UNITY" -batchmode "${gfx[@]}" -projectPath "$PWD" -runTests -testPlatform "$p" \
    -testResults "$xml" -logFile "$log"
  local rc=$?
  if [ ! -f "$xml" ]; then echo "$p: NESSUN RISULTATO (exit $rc)" | tee -a Logs/test-summary.txt; FAIL=1; return; fi
  python3 - "$xml" "$p" <<'PY' | tee -a Logs/test-summary.txt
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
g = lambda k: r.get(k, "0")
print(f"{sys.argv[2]}: esito={g('result')} totali={g('total')} superati={g('passed')} falliti={g('failed')} saltati={g('skipped')}")
PY
  [ $rc -ne 0 ] && FAIL=1
}
case "${1:-all}" in
  editmode) run EditMode ;;
  playmode) run PlayMode ;;
  all) run EditMode; run PlayMode ;;
  *) echo "uso: $0 [editmode|playmode|all]"; exit 2 ;;
esac
[ $FAIL -eq 0 ] && echo "TEST: TUTTI VERDI" || echo "TEST: CI SONO FALLIMENTI"
exit $FAIL
