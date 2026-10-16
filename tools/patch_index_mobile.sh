#!/usr/bin/env bash
# Applica all'index.html della build WebGL la stessa patch mobile di um-web/pubblica.sh (USE_BUILD_INDEX=1):
# viewport senza zoom, touch-action/overscroll none, showBanner -> console.warn, errorHandler che ritorna true, nessun alert.
# Uso: tools/patch_index_mobile.sh <index.html>   (modifica il file sul posto)
# Exit code: 0 patch applicata e verificata, 1 patch non applicata.
set -euo pipefail
F="${1:?Uso: $0 <index.html>}"
[ -f "$F" ] || { echo "ERRORE: $F non trovato"; exit 1; }
T="$(mktemp)"
sed -e 's|<meta name="viewport"[^>]*>|<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover">|' \
    -e 's|</style>|html,body,canvas{touch-action:none;overscroll-behavior:none;-webkit-user-select:none;user-select:none}</style>|' \
    -e 's|createUnityInstance(\([a-zA-Z_$]*\),{|createUnityInstance(\1,{showBanner:function(m,t){console.warn("[Unity "+(t\|\|"info")+"] "+m);},errorHandler:function(e,f,l){console.warn("[Unity error]",e,f,l);return true;},|' \
    -e 's|\.catch(function(e){alert(e);})|.catch(function(e){console.warn("[Unity init]",e);})|' \
    "$F" > "$T"
err=0
chk(){ grep -qF -- "$1" "$T" || { echo "ERRORE: patch non applicata: $2"; err=1; }; }
chk 'user-scalable=no' 'meta viewport senza zoom'
chk 'touch-action:none;overscroll-behavior:none' 'touch-action/overscroll'
chk 'showBanner:function(m,t){console.warn(' 'showBanner con console.warn'
chk 'return true;}' 'errorHandler che ritorna true'
if grep -q 'alert(' "$T"; then echo "ERRORE: è rimasto un alert(...) nell'index"; err=1; fi
[ $err = 0 ] || { rm -f "$T"; exit 1; }
mv "$T" "$F"
echo "OK: patch mobile applicata a $F"
