#!/usr/bin/env bash
# Pubblica la build WebGL (file sciolti, niente zip) sul repo GitHub Pages dedicato.
# Uso: tools/deploy_pages.sh [cartella_build]   (default: Builds/WebGL)
# Env: UM_WEB_REPO (default B00RR/ultimo-miglio-web). Richiede gh autenticato con push.
set -euo pipefail
cd "$(dirname "$0")/.."
SRC="${1:-Builds/WebGL_min}"; REPO="${UM_WEB_REPO:-B00RR/ultimo-miglio-web}"
[ -f "$SRC/index.html" ] || { echo "ERRORE: $SRC/index.html non trovato: lancia prima tools/build_webgl.sh" >&2; exit 2; }
if ls "$SRC"/Build/*.br "$SRC"/Build/*.gz >/dev/null 2>&1; then
  echo "ERRORE: build compressa (.br/.gz): GitHub Pages non la serve correttamente. Rifare con WebGLBuild.BuildCI." >&2; exit 2; fi
SHA=$(git rev-parse --short HEAD); TMP=$(mktemp -d)
# Svuota la radice ma preserva dev/ (preview) e prova/
gh repo clone "$REPO" "$TMP" -- -q --depth 1
find "$TMP" -mindepth 1 -maxdepth 1 ! -name .git ! -name dev ! -name prova -exec rm -rf {} +
cp -r "$SRC"/. "$TMP"/; touch "$TMP/.nojekyll"
tools/patch_index_mobile.sh "$TMP/index.html"
tools/cache_bust_index.sh "$TMP/index.html" "$SHA"
echo "commit sorgente: $SHA — $(date '+%Y-%m-%d %H:%M') Europe/Rome" > "$TMP/VERSIONE.txt"
cd "$TMP"; git add -A
git commit -qm "Build WebGL da ultimo-miglio@$SHA" && git push -q origin HEAD:main
echo "Pubblicato. URL: https://$(echo "${REPO%%/*}" | tr A-Z a-z).github.io/${REPO#*/}/ (attendere 1-2 minuti)"
