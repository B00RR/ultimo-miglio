# Anteprima clip robot (Workbench, atlas del 3D Artist). Uso:
# blender -b -P um_anim_preview_bots.py -- OUTDIR STEP bot_nonno bot_tico
import bpy, os, sys
from mathutils import Vector
a=sys.argv[sys.argv.index("--")+1:]; OUT=a[0]; STEP=int(a[1]); NAMES=a[2:]; os.makedirs(OUT,exist_ok=True)
ROOT=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),"..",".."))
ATL=os.environ.get("UM_ATLAS",os.path.join(ROOT,"..","wt","3d","Assets","_Game","Art","Textures","T_Palette_Atlas.png"))
bpy.ops.wm.read_factory_settings(use_empty=True); sc=bpy.context.scene
ORDER={"bot_nonno":["Idle","Drive","Lift","Drop","Happy"],"bot_tico":["Idle","Move","Stamp","Happy"]}
rigs={}
for n in NAMES:
    before=set(bpy.data.actions)
    with bpy.data.libraries.load(f"{ROOT}/source/bot/{n}_final_rig.blend") as (s,d): d.objects=s.objects; d.actions=s.actions
    for o in d.objects:
        sc.collection.objects.link(o)
        if o.type=='ARMATURE': rigs[n]=o
    rigs[n+"_acts"]={x.name.split(".")[0]:x for x in bpy.data.actions if x not in before}
img=bpy.data.images.load(ATL)
for m in bpy.data.materials:
    if m.use_nodes and "Principled BSDF" in m.node_tree.nodes:
        for x in [x for x in m.node_tree.nodes if x.type=='TEX_IMAGE']: m.node_tree.nodes.remove(x)
        t=m.node_tree.nodes.new("ShaderNodeTexImage"); t.image=img; t.interpolation='Closest'
        m.node_tree.links.new(t.outputs[0],m.node_tree.nodes["Principled BSDF"].inputs[0])
bpy.ops.mesh.primitive_plane_add(size=30); fl=bpy.context.object
fm=bpy.data.materials.new("floor"); fm.diffuse_color=(0.85,0.72,0.52,1); fl.data.materials.append(fm)
cam=bpy.data.cameras.new("cam"); cam.type='ORTHO'; cam.ortho_scale=2.2; co=bpy.data.objects.new("cam",cam); sc.collection.objects.link(co); sc.camera=co
CLOSE=os.environ.get("UM_CLOSE")=="1"   # primo piano baffi/viso di NONNO
if CLOSE: cam.ortho_scale=0.9
d=Vector((0.6,-1.6,0.45) if CLOSE else (0.9,-1.6,1.1)).normalized(); co.location=Vector((0,-0.2,0.88) if CLOSE else (0,0,0.85))+d*8; co.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
sc.render.engine='BLENDER_WORKBENCH'; s=sc.display.shading; s.light='STUDIO'; s.color_type='TEXTURE'
s.show_shadows=True; s.shadow_intensity=0.35; s.show_cavity=True; s.cavity_type='BOTH'; s.show_object_outline=True; s.object_outline_color=(0.23,0.18,0.16)
sc.display.light_direction=(0.4,-0.5,0.8); sc.world=bpy.data.worlds.new("w"); sc.world.color=(0.9,0.74,0.5)
sc.render.resolution_x=sc.render.resolution_y=540
idx=0
for n in NAMES:
    R=rigs[n]
    for o in sc.objects:
        if o.type in('MESH','ARMATURE') and o!=fl: o.hide_render=not(o==R or o.parent==R)
    for c in ORDER[n]:
        act=rigs[n+"_acts"][c]; R.animation_data.action=act; N=int(act.frame_range[1])
        fr=(list(range(N))*2 if act.use_cyclic else list(range(N+1))+[N]*6)[::STEP]
        for f in fr:
            sc.frame_set(f); sc.render.filepath=f"{OUT}/{idx:04d}_{n}{'_close' if CLOSE else ''}_{c}_{f}.png"; bpy.ops.render.render(write_still=True); idx+=1
print("UM_PREVIEW",idx)
