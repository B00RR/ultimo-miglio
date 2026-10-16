# Layout stanza Bancone (riferimento beauty v03)

`bancone_layout.json` contiene la disposizione della stanza Bancone presa dalla scena beauty approvata (`3d/final/vela_figli_final_v04.blend`, `build_final.py`), con in più il set dressing premium (#47). È un dato di riferimento: la scena di gioco la costruiscono `SliceSceneBuilder` e `DepotKit`.

Rigenerazione: `blender -b vela_figli_final_v04.blend -P 3d/scripts/dump_bancone_blend.py` (scrive `/tmp/dump.json`), poi `python3 3d/scripts/export_bancone_layout.py Assets/_Game/Art/Environment/Layouts/bancone_layout.json`.

## Conversione Blender → Unity
- Blender: sistema destrorso, Z verso l'alto. Unity: sistema sinistrorso, Y verso l'alto. 1 unità = 1 m.
- FBX esportati con `-Z forward`, `Y up`, `bake_space_transform`; l'importer di Unity inverte la X.
- Posizione: `(x_u, y_u, z_u) = (-x_b, z_b, -y_b)`. Rotazione: `rotY_u = -rotZ_b`. Scala invariata.
- Muri chiusi su 4 lati (nel beauty erano tagliati per la camera). Le voci `primitive: "cube"` sono la nicchia e il pad, da colorare con lo swatch indicato.
- Il verso della conversione è calcolato e non ancora verificato in Unity.
