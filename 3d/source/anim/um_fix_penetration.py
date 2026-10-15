# Correzione automatica compenetrazioni: per ogni clip/frame, se due parti NON adiacenti (non padre-figlio)
# si compenetrano oltre il riposo, smorza (verso il riposo) le pose delle ossa coinvolte fino al contatto pulito.
# Lo smorzamento e' poi lisciato nel tempo (finestra +-3 frame) per non creare scatti; loop preservati.
# Uso: blender -b <rig>_final_rig.blend -P um_fix_penetration.py -- <fbx_out> <deform_only 0|1> [soglia=4] [skip_adjacent 0|1]
# skip_adjacent=1 per mesh skinnate continue (Nina: i giunti padre-figlio si deformano, non sono compenetrazioni)
import bpy, sys, collections, math
from mathutils import Quaternion, Vector
from mathutils.bvhtree import BVHTree
a=sys.argv[sys.argv.index("--")+1:]; FBX=a[0]; DEFONLY=a[1]=="1"; THR=int(a[2]) if len(a)>2 else 4; SKIPADJ=(a[3]=="1") if len(a)>3 else False
sc=bpy.context.scene
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
mesh=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==arm][0]
bones=arm.data.bones; names={g.index:g.name for g in mesh.vertex_groups}
part=[]
for p in mesh.data.polygons:
    w=collections.Counter()
    for vi in p.vertices:
        for g in mesh.data.vertices[vi].groups: w[names[g.group]]+=g.weight
    part.append(w.most_common(1)[0][0] if w else "?")
parts=sorted(set(part)-{"?"})
def adjacent(x,y):   # mesh skinnata: ossa a distanza <=2 nella gerarchia condividono la pelle (giunti, inguine, collo)
    if x not in bones or y not in bones: return False
    return len(chain(x,y))<=2
def chain(x,y):   # ossa da x e y fino all'antenato comune escluso
    ax=[];b=bones[x]
    while b: ax.append(b.name); b=b.parent
    ay=[];b=bones[y]
    while b: ay.append(b.name); b=b.parent
    common=next(n for n in ax if n in ay)
    return [n for n in ax[:ax.index(common)]]+[n for n in ay[:ay.index(common)]]
def counts():
    dg=bpy.context.evaluated_depsgraph_get(); ev=mesh.evaluated_get(dg); me=ev.to_mesh()
    V=[v.co.copy() for v in me.vertices]; T={}
    for pn in parts: T[pn]=BVHTree.FromPolygons(V,[list(p.vertices) for i,p in enumerate(me.polygons) if part[i]==pn])
    ev.to_mesh_clear(); c={}
    for i,x in enumerate(parts):
        for y in parts[i+1:]:
            if SKIPADJ and adjacent(x,y): continue
            n=len(T[x].overlap(T[y]))
            if n: c[(x,y)]=n
    return c
arm.data.pose_position='REST'; bpy.context.view_layer.update(); REST=counts(); arm.data.pose_position='POSE'
def bad(c): return [k for k,n in c.items() if n>REST.get(k,0)+THR]
def read_pose(f):
    sc.frame_set(f); return {pb.name:(pb.location.copy(),pb.rotation_quaternion.copy(),pb.scale.copy()) for pb in arm.pose.bones}
def apply(P,S):
    for n,(l,q,s) in P.items():
        k=S.get(n,1.0); pb=arm.pose.bones[n]
        pb.location=l*k; pb.rotation_quaternion=Quaternion().slerp(q,k); pb.scale=Vector((1,1,1)).lerp(s,k)
    bpy.context.view_layer.update()
log=[]
for act in sorted(bpy.data.actions,key=lambda x:x.name):
    arm.animation_data.action=act; N=int(act.frame_range[1]); loop=act.use_cyclic
    poses=[read_pose(f) for f in range(N+1)]; need=[{} for _ in range(N+1)]
    for f in range(N+1):
        S={}; apply(poses[f],S); it=0
        while True:
            bb=bad(counts())
            if not bb or it>14: break
            for k in bb:
                ch=chain(*k); sec=[n for n in ch if n.startswith("ponytail")]
                for n in (sec or ch):   # parti secondarie (coda) cedono per prime
                    if n.startswith('wheel'): continue   # le ruote girano su se stesse: non smorzare
                    if act.name.startswith("Carry") and any(t in n for t in ("Arm","Hand","Shoulder","Chest","Spine")): continue   # posa 'pila al petto' intenzionale
                    S[n]=S.get(n,1.0)*0.85   # le ruote girano su se stesse: non smorzare
            apply(poses[f],S); it+=1
        need[f]=S
        if S: log.append((act.name,f,bb if bb else "clean",{k:round(v,2) for k,v in S.items()}))
    # lisciatura temporale: per ogni osso minimo su finestra +-3 (ciclica se loop) poi media
    allb={n for S in need for n in S}; smooth=[{} for _ in range(N+1)]
    for n in allb:
        raw=[S.get(n,1.0) for S in need]
        idx=lambda i: (i%N) if loop else max(0,min(N,i))
        mn=[min(raw[idx(i+d)] for d in range(-3,4)) for i in range(N+1)]
        sm=[sum(mn[idx(i+d)] for d in range(-2,3))/5 for i in range(N+1)]
        for i in range(N+1): smooth[i][n]=min(sm[i],mn[i]+0.08)
    if allb:
        for f in range(N+1):
            sc.frame_set(f); apply(poses[f],smooth[f])
            for pb in arm.pose.bones:
                for kk in ("location","rotation_quaternion","scale"): pb.keyframe_insert(kk,frame=f)
    # verifica finale
    left=[]
    for f in range(N+1):
        sc.frame_set(f); b=bad(counts())
        if b: left.append((f,b))
    print("FIX",act.name,"bones damped:",sorted(allb),"| residual:",left[:6] if left else "none")
arm.animation_data.action=bpy.data.actions["Idle"]; sc.render.fps=30; sc.render.fps_base=1.0
bpy.ops.wm.save_mainfile()
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=FBX, object_types={'ARMATURE','MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z', axis_up='Y', use_armature_deform_only=DEFONLY, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE')
print("FIX_DONE",FBX)
