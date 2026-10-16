# Ultimo Miglio - NONNO: sposta le ossa del rig sulla mesh finale ricentrata e la skinna rigidamente per pezzo (loose parts).
# Le direzioni delle ossa non cambiano -> le 5 action (delta) restano valide senza re-bake.
# Uso: blender -b 3d/source/bot/bot_nonno_rig.blend -P um_bind_nonno.py [-- /path/bot_nonno_mesh.fbx]
import bpy, sys, os, bmesh
from mathutils import Vector
argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
ROOT=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),"..",".."))
MESH=argv[0] if argv else f"{ROOT}/export/bot/bot_nonno_mesh.fbx"
arm=bpy.data.objects["bot_nonno_rig"]
for o in list(arm.children): bpy.data.objects.remove(o)
J={"Root":((0,0,0),(0,0.3,0)),"Base":((0,0.17,0.2),(0,0.17,0.6)),"Mast":((0,-0.14,0.06),(0,-0.14,0.8)),
   "Forks":((0,-0.14,0.08),(0,-0.48,0.08)),"Neck":((0,0.2,0.86),(0,0.2,0.95)),"Head":((0,0.2,0.95),(0,0.2,1.25)),
   "Wheel_FL":((0.32,-0.02,0.11),(0.42,-0.02,0.11)),"Wheel_FR":((-0.32,-0.02,0.11),(-0.42,-0.02,0.11)),
   "Wheel_BL":((0.32,0.27,0.11),(0.42,0.27,0.11)),"Wheel_BR":((-0.32,0.27,0.11),(-0.42,0.27,0.11))}
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
for n,(h,t) in J.items(): eb=arm.data.edit_bones[n]; eb.head=h; eb.tail=t; eb.roll=0
bpy.ops.object.mode_set(mode='OBJECT')
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=MESH)
new=[o for o in bpy.data.objects if o not in before]
for o in new:
    if o.type!='MESH': bpy.data.objects.remove(o)
m=[o for o in new if o.name in bpy.data.objects][0]; m.name="bot_nonno_LOD0"
bpy.ops.object.select_all(action='DESELECT'); m.select_set(True); bpy.context.view_layer.objects.active=m
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
for vg in list(m.vertex_groups): m.vertex_groups.remove(vg)
def bone_for(c, mn, mx):
    if mx.z<0.25 and abs(c.x)>0.3 and (mx.x-mn.x)<0.05:                       # ruote
        return ("Wheel_F" if c.y<0.1 else "Wheel_B")+("L" if c.x>0 else "R")
    if c.y<-0.11 and mx.z<0.2: return "Forks"                                  # forche + piastra
    if c.y<-0.09 and abs(c.x)<0.2 and mx.z>0.7: return "Mast"                  # montanti + traversa
    if mn.z>0.88 or (mn.z>0.84 and c.y>0.15 and abs(c.x)<0.1): return "Head"   # testa, viso, antenna, orecchie
    return "Base"
bm=bmesh.new(); bm.from_mesh(m.data); bm.verts.ensure_lookup_table()
seen=set(); groups={}
for v in bm.verts:
    if v.index in seen: continue
    stack=[v]; isl=[]; seen.add(v.index)
    while stack:
        x=stack.pop(); isl.append(x.index)
        for e in x.link_edges:
            o=e.other_vert(x)
            if o.index not in seen: seen.add(o.index); stack.append(o)
    cs=[bm.verts[i].co for i in isl]; mn=Vector([min(c[k] for c in cs) for k in range(3)]); mx=Vector([max(c[k] for c in cs) for k in range(3)])
    groups.setdefault(bone_for((mn+mx)/2,mn,mx),[]).extend(isl)
for b,idx in groups.items(): m.vertex_groups.new(name=b).add(idx,1.0,'REPLACE')
print("UM_NONNO parts",{b:len(i) for b,i in groups.items()})
m.parent=arm; md=m.modifiers.new("Armature",'ARMATURE'); md.object=arm
bpy.ops.wm.save_as_mainfile(filepath=f"{ROOT}/source/bot/bot_nonno_final_rig.blend")
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=f"{ROOT}/export/bot/bot_nonno.fbx", object_types={'ARMATURE','MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', use_armature_deform_only=True,
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE')
print("UM_DONE", [a.name for a in bpy.data.actions])
