#!/usr/bin/env bash
# Ri-esegue tutto il binding sulle mesh finali (da rilanciare a ogni re-export del 3D Artist, es. UV su T_Palette_Atlas).
# Uso: 3d/source/anim/um_bind_all.sh [--no-unity-copy]
set -euo pipefail
cd "$(dirname "$0")/../.."     # -> 3d/
B="${BLENDER:-/workspace/tools/blender-4.2.3-linux-x64/blender}"
WT="${UM_WT:-/workspace/ultimo-miglio/wt/animator}/Assets/_Game/Art/Characters"
# sorgenti animazione (rig blockout in T-pose + action): rigenerati solo se mancano
[ -f source/chr/chr_nina_vela_rig.blend ] || "$B" -b -P source/anim/um_rig_anim.py -- nina
[ -f source/bot/bot_nonno_rig.blend ]     || "$B" -b -P source/anim/um_rig_anim.py -- nonno
"$B" -b source/chr/chr_nina_vela_rig.blend -P source/anim/um_retarget_nina.py -- "$(ls "$PWD"/export/chr/chr_nina_vela_v0*.blend 2>/dev/null | sort | tail -1 || echo "$PWD/export/chr/chr_nina_vela_mesh.fbx")" | grep UM_
"$B" -b source/chr/chr_nina_vela_final_rig.blend -P source/anim/um_fix_penetration.py -- "$PWD/export/chr/chr_nina_vela.fbx" 1 4 1 | grep -E "^FIX"
# robot v02 (NONNO muletto, TICO): rig dai vertex group + anti-compenetrazione
if [ "${UM_BIND_BOTS:-1}" = "1" ]; then
  for c in nonno tico; do
    "$B" -b export/bot/bot_${c}_v02.blend -P source/anim/um_bind_bot.py -- $c | grep UM_DONE
    "$B" -b source/bot/bot_${c}_final_rig.blend -P source/anim/um_fix_penetration.py -- "$PWD/export/bot/bot_${c}.fbx" 0 4 0 | grep -E "^FIX"
  done
fi
rm -f source/*/*.blend1
if [ "${1:-}" != "--no-unity-copy" ]; then cp export/chr/chr_nina_vela.fbx "$WT/"; [ "${UM_BIND_BOTS:-1}" = "1" ] && cp export/bot/bot_nonno.fbx export/bot/bot_tico.fbx "$WT/" || true; echo "copiati in $WT (meta/GUID invariati)"; fi
