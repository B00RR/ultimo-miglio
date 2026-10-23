#!/usr/bin/env bash
# Pubblica (o aggiorna) un commento con l'esito di test e build su una PR.
# Uso: tools/pr_comment.sh <numero_PR> [url_build] [url_run]
# Richiede: gh autenticato (o GH_TOKEN), python3. Legge Logs/*-results.xml e Logs/webgl_build.log.
set -euo pipefail
cd "$(dirname "$0")/.."
PR="${1:?numero PR mancante}"; URL="${2:-}"; export UM_RUN="${3:-}"
REPO="${UM_REPO:-B00RR/ultimo-miglio}"
BODY=$(python3 - "$URL" <<'PY'
import sys, os, glob, xml.etree.ElementTree as ET, datetime, subprocess
url = sys.argv[1]
ora = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
try: sha = subprocess.check_output(["git","rev-parse","--short","HEAD"], text=True, stderr=subprocess.DEVNULL).strip()
except Exception: sha = "?"
L = ["<!-- um-ci -->", f"## Esito pipeline — commit `{sha}` ({ora} Europe/Rome)", "",
     "| Suite | Esito | Superati | Falliti | Saltati |", "|---|---|---|---|---|"]
falliti = []
saltati = []
for p in ("editmode", "playmode"):
    f = f"Logs/{p}-results.xml"
    if not os.path.exists(f):
        L.append(f"| {p} | ⚪ non eseguito | – | – | – |"); continue
    r = ET.parse(f).getroot()
    # NUnit marca l'intera suite «Skipped:Ignored» se un test è saltato di proposito
    # (es. Cattura205 senza UM_CATTURA205), anche con zero falliti. Rosso solo se qualcosa fallisce.
    failed_n = int(r.get("failed") or 0)
    ok = failed_n == 0 and not r.get("result", "").startswith("Failed")
    L.append(f"| {p} | {'✅ verde' if ok else '❌ rosso'} | {r.get('passed')} | {r.get('failed')} | {r.get('skipped')} |")
    for tc in r.iter("test-case"):
        if tc.get("result") == "Failed":
            m = tc.find("failure/message")
            falliti.append(f"- `{tc.get('fullname')}`: {(m.text or '').strip()[:300] if m is not None else ''}")
        elif tc.get("result") not in ("Passed", None):
            msg = tc.findtext("reason/message") or tc.findtext("properties/property") or ""
            saltati.append(f"- `{tc.get('fullname')}`: {msg.strip()[:200]}")
log = "Logs/webgl_build.log"
if os.path.exists(log):
    riga = [l for l in open(log, errors="ignore") if "[WebGLBuild] esito=" in l or "[WebGLMin] " in l and "errors=" in l]
    esito = riga[-1].strip() if riga else "esito non trovato nel log"
    L += ["", f"**Build WebGL:** {'✅' if 'Succeeded' in esito else '❌'} `{esito}`"]
if url: L += ["", f"**Link giocabile:** {url}"]
if os.environ.get("UM_RUN"): L += ["", f"**Run:** {os.environ['UM_RUN']}"]
if os.environ.get("UM_NOTA"): L += ["", os.environ["UM_NOTA"]]
if falliti: L += ["", "### Test falliti", *falliti]
if saltati: L += ["", "### Test saltati", *saltati]
print("\n".join(L))
PY
)
# Aggiorna il commento precedente della pipeline se esiste, altrimenti ne crea uno.
ID=$(gh api "repos/$REPO/issues/$PR/comments" --paginate --jq '.[] | select(.body | startswith("<!-- um-ci -->")) | .id' | tail -1)
if [ -n "$ID" ]; then gh api -X PATCH "repos/$REPO/issues/comments/$ID" -f body="$BODY" >/dev/null; echo "Commento aggiornato su PR #$PR"
else gh pr comment "$PR" -R "$REPO" --body "$BODY"; fi
