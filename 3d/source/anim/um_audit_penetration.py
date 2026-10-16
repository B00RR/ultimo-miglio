# Audit compenetrazioni parte-parte per ogni clip/frame (BVH overlap tra le parti della mesh deformata).
# Parti = gruppo osseo dominante di ogni faccia. Le coppie che si toccano gia' a riposo (giunti, pezzi incastrati)
# vengono segnalate solo se le facce in intersezione crescono oltre il riposo (soglia).
# Uso: blender -b <rig>.blend -P um_audit_penetration.py -- [REPORT.json] [soglia_extra=6]
import bpy, sys, json, collections
from mathutils.bvhtree import BVHTree
a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
OUT=a[0] if a else None; THR=int(a[1]) if len(a)>1 else 6
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
mesh=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==arm][0]
names={g.index:g.name for g in mesh.vertex_groups}
part=[]
for p in mesh.data.polygons:
    w=collections.Counter()
    for vi in p.vertices:
        for g in mesh.data.vertices[vi].groups: w[names[g.group]]+=g.weight
    part.append(w.most_common(1)[0][0] if w else "?")
parts=sorted(set(part))
def trees():
    dg=bpy.context.evaluated_depsgraph_get(); ev=mesh.evaluated_get(dg); me=ev.to_mesh()
    V=[mesh.matrix_world@v.co for v in me.vertices]; T={}
    for pn in parts:
        polys=[list(p.vertices) for i,p in enumerate(me.polygons) if part[i]==pn]
        T[pn]=BVHTree.FromPolygons(V,polys,all_triangles=False,epsilon=0.0)
    ev.to_mesh_clear(); return T
def counts(T):
    c={}
    for i,x in enumerate(parts):
        for y in parts[i+1:]:
            n=len(T[x].overlap(T[y]))
            if n: c[(x,y)]=n
    return c
# riposo
arm.data.pose_position='REST'; bpy.context.view_layer.update(); rest=counts(trees()); arm.data.pose_position='POSE'
res={}
for act in sorted(bpy.data.actions,key=lambda a:a.name):
    arm.animation_data.action=act; N=int(act.frame_range[1]); bad={}
    for f in range(N+1):
        bpy.context.scene.frame_set(f)
        for k,n in counts(trees()).items():
            if n>rest.get(k,0)+THR:
                key=f"{k[0]}|{k[1]}"; e=bad.setdefault(key,{"frames":[],"max":0,"rest":rest.get(k,0)})
                e["frames"].append(f); e["max"]=max(e["max"],n)
    res[act.name]=bad
    print("AUDIT",mesh.name,act.name,"OK" if not bad else {k:(v["frames"][0],v["frames"][-1],len(v["frames"]),v["max"],v["rest"]) for k,v in bad.items()})
if OUT: json.dump(res,open(OUT,"w"),indent=1)
