# Anteprima clip Nina + NONNO (mesh finali). Uso: blender -b 3d/source/chr/chr_nina_vela_final_rig.blend -P um_anim_preview_v02.py -- OUTDIR [step]
import bpy, os, sys, math
from mathutils import Vector
a=sys.argv[sys.argv.index("--")+1:]; OUT=a[0]; STEP=int(a[1]) if len(a)>1 else 1; os.makedirs(OUT,exist_ok=True)
sc=bpy.context.scene; rig=bpy.data.objects["chr_nina_vela_rig"]
nacts={a.name:a for a in bpy.data.actions}
BOT=os.path.join(os.path.dirname(bpy.data.filepath),"..","bot","bot_nonno_final_rig.blend")
bot=None; objs=[]
if os.environ.get("UM_PREVIEW_NONNO")=="1":   # NONNO solo quando esiste la sua mesh vera
    with bpy.data.libraries.load(BOT) as (src,dst): dst.objects=src.objects
    objs=dst.objects
for o in objs:
    sc.collection.objects.link(o)
    if o.type=='ARMATURE': bot=o
bacts={a.name.split(".")[0]:a for a in bpy.data.actions if a not in nacts.values()}
bpy.ops.mesh.primitive_plane_add(size=30); fl=bpy.context.object
m=bpy.data.materials.new("floor"); m.diffuse_color=(0.85,0.72,0.52,1); fl.data.materials.append(m)
cam=bpy.data.cameras.new("cam"); cam.type='ORTHO'; cam.ortho_scale=2.3; co=bpy.data.objects.new("cam",cam); sc.collection.objects.link(co); sc.camera=co
d=Vector((0.9,-1.6,1.1)).normalized(); co.location=Vector((0,0,0.9))+d*8; co.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
sc.render.engine='BLENDER_WORKBENCH'; s=sc.display.shading; s.light='STUDIO'; s.color_type='TEXTURE'
s.show_shadows=True; s.shadow_intensity=0.35; s.show_cavity=True; s.cavity_type='BOTH'; s.show_object_outline=True; s.object_outline_color=(0.23,0.18,0.16)
sc.display.light_direction=(0.4,-0.5,0.8); sc.world=bpy.data.worlds.new("w"); sc.world.color=(0.9,0.74,0.5)
sc.render.resolution_x=sc.render.resolution_y=540
# solo anteprima: collega l'atlas del 3D Artist al Base Color (il .blend/FBX non viene modificato)
ATL=os.path.join(os.path.dirname(bpy.data.filepath),"..","..","export","T_Diorama_Atlas.png")
ATL=os.environ.get("UM_ATLAS",ATL if os.path.exists(ATL) else os.path.join(os.path.dirname(bpy.data.filepath),"..","..","..","wt","3d","Assets","_Game","Art","Textures","T_Palette_Atlas.png"))
for mat in bpy.data.materials:
    if mat.use_nodes and "Principled BSDF" in mat.node_tree.nodes and os.path.exists(ATL):
        for n_ in [n_ for n_ in mat.node_tree.nodes if n_.type=='TEX_IMAGE']: mat.node_tree.nodes.remove(n_)   # texture FBX mancanti -> magenta
        t=mat.node_tree.nodes.new("ShaderNodeTexImage"); t.image=bpy.data.images.load(ATL,check_existing=True); t.interpolation='Closest'
        mat.node_tree.links.new(t.outputs[0],mat.node_tree.nodes["Principled BSDF"].inputs[0])
idx=0
CLOSE=os.environ.get("UM_CLOSE")=="1"   # primo piano busto/giacca per Cheer
if CLOSE:
    cam.ortho_scale=0.95; d=Vector((0.35,-1.6,0.35)).normalized(); co.location=Vector((0,0,1.12))+d*8; co.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
JOBS=[(rig,nacts,["Cheer"] if CLOSE else ["Idle","Walk","Run","Carry_Idle","Carry_Walk","Deliver","Cheer"])]+([(bot,bacts,["Idle","Drive","Lift","Drop","Happy"])] if bot and not os.environ.get("UM_NOBOT") else [])
for R,acts,order in JOBS:
  for o in sc.objects:
      if o.type in ('MESH','ARMATURE') and o!=fl: o.hide_render=not (o==R or o.parent==R)
  tag="chr_nina_vela" if R==rig else "bot_nonno"
  for name in order:
    act=acts[name]; R.animation_data.action=act; n=int(act.frame_range[1])
    fr=(list(range(n))*2 if act.use_cyclic else list(range(n+1))+[n]*6)[::STEP]
    for f in fr:
        sc.frame_set(f); sc.render.filepath=f"{OUT}/{idx:04d}_{tag}_{name}_{f}.png"; bpy.ops.render.render(write_still=True); idx+=1
print("UM_PREVIEW",idx)
