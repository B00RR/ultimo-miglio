#!/usr/bin/env bash
# Cache-bust idempotente: inserisce o sostituisce ?v=<sha corto> sugli URL di Build/, TemplateData/ e StreamingAssets/
# di un index.html WebGL (loader, data, framework, wasm, css/immagini). Rilanciarlo non duplica ?v=.
# Uso: tools/cache_bust_index.sh <index.html> <sha>
set -euo pipefail
F="${1:?Uso: $0 <index.html> <sha>}"; SHA="${2:?Uso: $0 <index.html> <sha>}"; V="${SHA:0:7}"
[[ "$V" =~ ^[0-9a-fA-F]{7}$ ]] || { echo "ERRORE: sha non valido: $SHA" >&2; exit 2; }
[ -f "$F" ] || { echo "ERRORE: $F non trovato" >&2; exit 2; }
V="$V" perl -pi -e 's#((?:Build|TemplateData|StreamingAssets)/[^"\x27?\s)]+\.(?:js|data|wasm|unityweb|css|png|ico|json))(?:\?v=[0-9A-Za-z_]+)?#$1?v=$ENV{V}#g' "$F"
grep -qF "?v=$V" "$F" || { echo "ERRORE: cache-bust ?v=$V non applicato a $F" >&2; exit 1; }
