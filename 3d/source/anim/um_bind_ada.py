# Ultimo Miglio - Nonna Ada: scheletro Humanoid di Nina (stessi giunti, 05_3D_PIPELINE §9) sulla mesh del 3D Artist.
# Corpo: pesi automatici (bone heat). Accessori: pesi RIGIDI 100% dai vertex group del .blend:
#   Head  <- hair, hair_bun, pencil, glasses | Chest <- blouse_collar, pin_faro, grembiule sopra z=APRON_Z
#   Hips  <- cardigan_skirt, ledger, grembiule sotto. Clip: Idle, Walk (da Nina, passo gia' nel verso giusto), Talk (nuova).
# Scala: nessuna nel rig (alta come Nina); lo 0.88 va SOLO sul root del prefab.
# Uso: blender -b <chr_nina_vela_final_rig.blend> -P um_bind_ada.py -- <chr_nonna_ada.blend> <out_final_rig.blend>
import bpy, sys, math
from mathutils import Quaternion, Vector
a=sys.argv[sys.argv.index("--")+1:]; ADA, OUT = a
APRON_Z=1.0
TALK_ARM=float(a[2]) if len(a)>2 else -55.0
TALK_FORE=float(a[3]) if len(a)>3 else -35.0
nina=bpy.data.objects["chr_nina_vela_rig"]
for o in list(bpy.data.objects):
    if o.type=='MESH': bpy.data.objects.remove(o)
nina.name="chr_nonna_ada_rig"; nina.data.name="chr_nonna_ada_rig"; arm=nina
for act in list(bpy.data.actions):
    if act.name not in ("Idle","Walk"): bpy.data.actions.remove(act)
with bpy.data.libraries.load(ADA) as (src, dst): dst.objects=["chr_nonna_ada"]
m=dst.objects[0]; bpy.context.scene.collection.objects.link(m); m.name="chr_nonna_ada_LOD0"
bpy.ops.object.select_all(action='DESELECT'); m.select_set(True); bpy.context.view_layer.objects.active=m
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
W=[v.co.copy() for v in m.data.vertices]
rig={}
def grp(n): g=m.vertex_groups[n]; return [v.index for v in m.data.vertices if any(x.group==g.index and x.weight>0.5 for x in v.groups)]
for n in ("hair","hair_bun","pencil","glasses"): rig.update({i:"Head" for i in grp(n)})
for n in ("blouse_collar","pin_faro"): rig.update({i:"Chest" for i in grp(n)})
for n in ("cardigan_skirt","ledger"): rig.update({i:"Hips" for i in grp(n)})
for i in grp("apron"): rig[i]="Chest" if W[i].z>APRON_Z else "Hips"
for g in list(m.vertex_groups): m.vertex_groups.remove(g)
# pesi automatici sul corpo (solo ossa deform del rig di Nina)
bpy.ops.object.select_all(action='DESELECT'); m.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
arm.animation_data.action=None
for pb in arm.pose.bones: pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0); pb.scale=(1,1,1)
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
skip={"ponytail_01","ponytail_02","Backpack","Prop_R"}   # ossa di Nina senza geometria su Ada: restano (stessi giunti) ma senza pesi
for n in skip:
    if n in m.vertex_groups: m.vertex_groups.remove(m.vertex_groups[n])
for i,b in rig.items():
    for g in m.vertex_groups: g.remove([i])
    (m.vertex_groups.get(b) or m.vertex_groups.new(name=b)).add([i],1.0,'REPLACE')
nz=[v.index for v in m.data.vertices if not any(x.weight>0 for x in v.groups)]
print("UM_ADA rigidi",len(rig),"vertici",len(m.data.vertices),"senza pesi",len(nz))
def seg(p,h,t):
    d=t-h; u=max(0,min(1,(p-h).dot(d)/max(d.length_squared,1e-9))); return (p-(h+d*u)).length
for i in nz:   # ripiego (isole dove il bone heat fallisce): osso piu' vicino come segmento, peso rigido
    best=min((b for b in arm.data.bones if b.use_deform and b.name not in skip), key=lambda b:seg(W[i],b.head_local,b.tail_local))
    (m.vertex_groups.get(best.name) or m.vertex_groups.new(name=best.name)).add([i],1.0,'REPLACE')
# ---- Talk: Idle + mano destra che gesticola, testa che annuisce (loop 48 frame)
idle=bpy.data.actions["Idle"]; sc=bpy.context.scene; arm.animation_data.action=idle
talk=bpy.data.actions.new("Talk"); N=48; poses=[]
for f in range(N+1):
    sc.frame_set(f % 60); poses.append({pb.name:pb.rotation_quaternion.copy() for pb in arm.pose.bones})
arm.animation_data.action=talk
def q(axis,ang): return Quaternion(Vector(axis),ang)
for f in range(N+1):
    t=f/N; p=2*math.pi*t
    for pb in arm.pose.bones: pb.rotation_quaternion=poses[f][pb.name]
    pb=arm.pose.bones
    pb["RightArm"].rotation_quaternion = pb["RightArm"].rotation_quaternion @ q((1,0,0), math.radians(TALK_ARM)) @ q((0,0,1), math.radians(8*math.sin(p)))
    pb["RightForeArm"].rotation_quaternion = pb["RightForeArm"].rotation_quaternion @ q((1,0,0), math.radians(TALK_FORE + 15*math.sin(2*p)))
    pb["RightHand"].rotation_quaternion = pb["RightHand"].rotation_quaternion @ q((0,0,1), math.radians(20*math.sin(2*p)))
    pb["Head"].rotation_quaternion = pb["Head"].rotation_quaternion @ q((1,0,0), math.radians(6*max(0,math.sin(4*p))))
    pb["Chest"].rotation_quaternion = pb["Chest"].rotation_quaternion @ q((0,1,0), math.radians(3*math.sin(p)))
    for b in arm.pose.bones: b.keyframe_insert("rotation_quaternion", frame=f); b.keyframe_insert("location", frame=f)
for act in bpy.data.actions: act.use_fake_user=True
arm.animation_data.action=bpy.data.actions["Idle"]
bpy.ops.wm.save_as_mainfile(filepath=OUT)
print("UM_ADA_DONE", [x.name for x in bpy.data.actions], "tris", sum(len(p.vertices)-2 for p in m.data.polygons))
