#!/usr/bin/env bash
# Prepara la preview /dev/ (issue #179) dentro un clone del repo del sito (B00RR/ultimo-miglio-web).
# Copia la build SOLO in <clone_sito>/dev/ (la radice e prova/ non vengono mai toccate), poi sull'index di dev/:
# patch mobile (se manca), cap DPR a 1.25 ed etichetta fissa "dev <sha corto> - gg/mm HH:MM" (ora Europe/Rome).
# Uso: tools/deploy_dev.sh <cartella_build> <clone_sito> [sha]
# Non fa commit ne' push: li fa il workflow (o chi lo lancia) dopo aver controllato che cambi solo dev/.
set -euo pipefail
QUI="$(cd "$(dirname "$0")" && pwd)"
SRC="${1:?Uso: $0 <cartella_build> <clone_sito> [sha]}"; SITO="${2:?Uso: $0 <cartella_build> <clone_sito> [sha]}"
SHA="${3:-$(git rev-parse HEAD)}"; CORTO="${SHA:0:7}"
[ -f "$SRC/index.html" ] || { echo "ERRORE: $SRC/index.html non trovato" >&2; exit 2; }
[ -d "$SITO/.git" ] || { echo "ERRORE: $SITO non e' un clone git del sito" >&2; exit 2; }
DEV="$SITO/dev"
# Copia mirata: si svuota e si riscrive solo dev/
rm -rf "$DEV"; mkdir -p "$DEV"; cp -r "$SRC"/. "$DEV"/
F="$DEV/index.html"
grep -qF 'user-scalable=no' "$F" || "$QUI/patch_index_mobile.sh" "$F"
CAP='Math.min(window.devicePixelRatio||1,1.25)'
if ! grep -qF "devicePixelRatio:$CAP" "$F"; then
  if grep -q 'devicePixelRatio:' "$F"; then
    perl -0pi -e 's/devicePixelRatio:\s*(Math\.min\([^)]*\)|[^,}]+)/devicePixelRatio:Math.min(window.devicePixelRatio||1,1.25)/' "$F"
  else
    perl -0pi -e 's/createUnityInstance\(([A-Za-z_\$]+),\{/createUnityInstance($1,{devicePixelRatio:Math.min(window.devicePixelRatio||1,1.25),/' "$F"
  fi
fi
grep -qF "devicePixelRatio:$CAP" "$F" || { echo "ERRORE: cap DPR non applicato" >&2; exit 1; }
QUANDO="$(TZ=Europe/Rome date '+%d/%m %H:%M')"
ETI="dev $CORTO - $QUANDO"
DIV="<div id=\"um-dev\" style=\"position:fixed;right:4px;bottom:4px;z-index:9999;padding:2px 6px;font:11px/1.3 monospace;color:#fff;background:rgba(0,0,0,.55);border-radius:3px;pointer-events:none\">$ETI</div>"
DIV="$DIV" perl -0pi -e 's/<div id="um-dev"[^<]*<\/div>//g; s/<\/body>/$ENV{DIV}<\/body>/' "$F"
grep -qF "$ETI" "$F" || { echo "ERRORE: etichetta non applicata" >&2; exit 1; }
echo "commit sorgente: $SHA - $(TZ=Europe/Rome date '+%Y-%m-%d %H:%M') Europe/Rome" > "$DEV/VERSIONE.txt"
echo "OK: preview pronta in $DEV ($ETI)"
