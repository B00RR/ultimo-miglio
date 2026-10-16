#!/usr/bin/env bash
# CI self-hosted: avvia l'Editor Unity del box in batchmode con lock e attesa.
# Sul box può girare una sola istanza Unity: flock su /tmp/unity-ci.lock (tra job CI)
# + attesa che non ci siano altri processi Unity Editor (anche di altri agenti), timeout 20 min.
# Uso: tools/ci_unity.sh <argomenti Unity...>   (projectPath e batchmode aggiunti qui)
set -uo pipefail
UNITY="${UNITY_EDITOR:-$HOME/Unity/Hub/Editor/6000.3.26f1/Editor/Unity}"
PROJ="$(cd "$(dirname "$0")/.." && pwd)"
exec 9>/tmp/unity-ci.lock
echo "Attendo il lock /tmp/unity-ci.lock..."; flock -w 1200 9 || { echo "::error::lock non ottenuto in 20 min"; exit 1; }
fine=$((SECONDS+1200))
while pgrep -f 'Editor/Unity( |$)' >/dev/null; do
  [ $SECONDS -ge $fine ] && { echo "::error::Altro Unity Editor attivo da 20 min:"; pgrep -af 'Editor/Unity( |$)'; exit 1; }
  echo "Altro Unity Editor attivo, attendo..."; sleep 15
done
"$UNITY" -batchmode -nographics -projectPath "$PROJ" "$@"
rc=$?; echo "Unity exit code: $rc"; exit $rc
