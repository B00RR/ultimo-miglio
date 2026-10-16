# Render anteprima di tutte le clip (Workbench) -> PNG in /tmp/um_prev/<idx>_<char>_<clip>_<frame>.png
# Uso: blender -b 3d/source/chr/chr_nina_vela_rig.blend -P um_anim_preview.py
import bpy, os, math
from mathutils import Vector
ROOT=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),"..",".."))
OUT="/tmp/um_prev"; os.makedirs(OUT,exist_ok=True)
sc=bpy.context.scene
nina=bpy.data.objects["chr_nina_vela_rig"]; nina_acts={a.name:a for a in bpy.data.actions}
with bpy.data.libraries.load(f"{ROOT}/source/bot/bot_nonno_rig.blend") as (src,dst): dst.objects=src.objects
nonno=None; bot_acts={}
for o in dst.objects:
    sc.collection.objects.link(o)
    if o.type=='ARMATURE': nonno=o
for a in bpy.data.actions:
    if a.library is None and a not in nina_acts.values(): bot_acts[a.name.split(".")[0]]=a
bpy.ops.mesh.primitive_plane_add(size=30); fl=bpy.context.object
m=bpy.data.materials.new("floor"); m.diffuse_color=(0.79,0.65,0.45,1); fl.data.materials.append(m)
cam=bpy.data.cameras.new("cam"); cam.type='ORTHO'; co=bpy.data.objects.new("cam",cam); sc.collection.objects.link(co); sc.camera=co
sc.render.engine='BLENDER_WORKBENCH'; s=sc.display.shading
s.light='STUDIO'; s.studio_light='paint.sl' if 'paint.sl' in [l.name for l in bpy.context.preferences.studio_lights] else s.studio_light; s.color_type='VERTEX'; s.show_shadows=True; s.shadow_intensity=0.35; s.show_cavity=True; s.cavity_type='BOTH'
s.show_object_outline=True; s.object_outline_color=(0.23,0.18,0.16)
sc.display.shadow_focus=0.2; sc.display.light_direction=(0.4,-0.5,0.8)
sc.world=bpy.data.worlds.new("w"); sc.world.color=(0.9,0.74,0.5); s.background_type='VIEWPORT'; s.background_color=(0.95,0.87,0.71)
sc.render.resolution_x=sc.render.resolution_y=540; sc.render.image_settings.file_format='PNG'
fl.active_material.diffuse_color=(0.88,0.78,0.6,1); s.color_type='VERTEX'
# floor non ha vertex color -> usa colore oggetto: aggiungo attributo
ca=fl.data.color_attributes.new("Col",'BYTE_COLOR','CORNER')
for d in ca.data: d.color=(0.8,0.66,0.48,1)
def frame_on(target, h):
    # vista 3/4 isometrica dal davanti-destra (personaggi guardano -Y)
    tgt=Vector((target.location.x,0,h*0.5)); d=Vector((0.9,-1.6,1.1)).normalized()
    co.location=tgt+d*8; co.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler(); cam.ortho_scale=h*1.25; print('CAM',cam.type,cam.ortho_scale,co.location)
idx=0
for rig,acts,order,h,other in ((nina,nina_acts,["Idle","Walk","Run","Carry_Idle","Carry_Walk","Deliver","Cheer"],1.9,nonno),
                               (nonno,bot_acts,["Idle","Drive","Lift","Drop","Happy"],1.9,nina)):
    for o in sc.objects: o.hide_render = (o!=fl and o.parent not in (rig,) and o!=rig)
    frame_on(rig,h)
    for name in order:
        a=acts[name]; rig.animation_data.action=a; n=int(a.frame_range[1])
        reps=2 if a.use_cyclic else 1
        for r in range(reps):
            for f in range(n if a.use_cyclic else n+1):
                sc.frame_set(f); sc.render.filepath=f"{OUT}/{idx:04d}_{rig.name.split('_rig')[0]}_{name}_{f}.png"
                bpy.ops.render.render(write_still=True); idx+=1
        # pausa di 6 frame sull'ultimo per clip non loop
        if not a.use_cyclic:
            for k in range(6):
                sc.frame_set(n); sc.render.filepath=f"{OUT}/{idx:04d}_{rig.name.split('_rig')[0]}_{name}_{n}.png"; bpy.ops.render.render(write_still=True); idx+=1
print("UM_PREVIEW frames", idx)
