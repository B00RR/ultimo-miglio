# #2: copia le UV dalla mesh nuova del 3D Artist (stessi vertici per numero/ordine/posizione) sul cliente gia' riggato,
# per indice (loop -> vertice), senza toccare scheletro, pesi e clip. Poi riesporta l'FBX riggato.
# Uso: blender -b --factory-startup -P um_uv_transfer_clienti.py -- <riggato.fbx> <nuova_uv.fbx> <out.fbx>
import bpy, sys
a = sys.argv[sys.argv.index("--")+1:]; RIG, SRC, OUT = a
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC); src = [o for o in bpy.context.selected_objects if o.type == 'MESH']
assert len(src) == 1, f"mesh sorgente: {len(src)}"; src = src[0]; sm = src.data
for o in list(bpy.context.selected_objects):
    if o != src: bpy.data.objects.remove(o)
src.name = "UVSRC"
bpy.ops.import_scene.fbx(filepath=RIG, automatic_bone_orientation=False, ignore_leaf_bones=False)
arm = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
dm = [o for o in bpy.data.objects if o.type == 'MESH' and o != src]; assert len(dm) == 1; dst = dm[0]; d = dst.data
assert len(sm.vertices) == len(d.vertices), f"vertici {len(sm.vertices)} != {len(d.vertices)}"
md = max((sm.vertices[i].co - d.vertices[i].co).length for i in range(len(d.vertices)))
mw = max(((src.matrix_world @ sm.vertices[i].co) - (dst.matrix_world @ d.vertices[i].co)).length for i in range(len(d.vertices)))
print("MAXDIST_VERT locale", md, "mondo", mw); assert mw < 1e-3, "vertici non coincidenti"
# UV per vertice+poligono: mappa (insieme vertici del poligono, vertice) -> uv sorgente
suv = sm.uv_layers.active.data; key = {}
for p in sm.polygons:
    fs = frozenset(p.vertices)
    for li in p.loop_indices: key[(fs, sm.loops[li].vertex_index)] = suv[li].uv.copy()
duv = d.uv_layers.active.data; miss = 0; ch = 0
for p in d.polygons:
    fs = frozenset(p.vertices)
    for li in p.loop_indices:
        uv = key.get((fs, d.loops[li].vertex_index))
        if uv is None: miss += 1; continue
        if (duv[li].uv - uv).length > 1e-5: ch += 1
        duv[li].uv = uv
print("UV_LOOPS", len(duv), "cambiate", ch, "mancanti", miss); assert miss == 0
bpy.data.objects.remove(src)
# l'import FBX antepone "<rig>|" ai nomi azione: ripristina i take originali "<rig>|Clip"
for act in bpy.data.actions:
    n = act.name.split("|")[-1]
    if n.startswith("Wait_Imp"): n = "Wait_Impatient"
    act.name = n
print("ARM", arm.name)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=OUT, object_types={'ARMATURE','MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', use_armature_deform_only=True, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE')
print("UV_DONE", OUT, [x.name for x in bpy.data.actions])
