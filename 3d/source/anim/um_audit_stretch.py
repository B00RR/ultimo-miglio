# Controllo stiramento: per ogni clip/frame confronta la lunghezza degli spigoli deformati con il riposo.
# Uso: blender -b <rig>.blend -P um_audit_stretch.py -- [soglia=1.6] [report.json]
import bpy,sys,json
a=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
THR=float(a[0]) if a else 1.6; OUT=a[1] if len(a)>1 else None
sc=bpy.context.scene; rig=next(o for o in bpy.data.objects if o.type=='ARMATURE'); me=next(o for o in bpy.data.objects if o.type=='MESH')
_fe={k for p in me.data.polygons for k in p.edge_keys}   # solo spigoli di facce (visibili)
E=[tuple(e.vertices) for e in me.data.edges if e.key in _fe]; V0=[v.co.copy() for v in me.data.vertices]
L0=[(V0[i]-V0[j]).length for i,j in E]
gn={g.index:g.name for g in me.vertex_groups}
def top(vi):
    w=sorted(((g.weight,gn[g.group]) for g in me.data.vertices[vi].groups),reverse=True); return [(n,round(x,2)) for x,n in w[:3]]
rep={}; bad=set()
for act in sorted(bpy.data.actions,key=lambda x:x.name):
    rig.animation_data.action=act; f0,f1=map(int,act.frame_range); worst=(1.0,None,None); nf=0
    for f in range(f0,f1+1):
        sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get(); m=me.evaluated_get(dg).to_mesh()
        n=0
        for k,(i,j) in enumerate(E):
            if L0[k]<1e-4: continue
            r=(m.vertices[i].co-m.vertices[j].co).length/L0[k]
            if r>THR or r<1/THR:
                n+=1; bad.update((i,j))
                if abs(r-1)>abs(worst[0]-1): worst=(r,f,(i,j))
        nf+=n>0; me.evaluated_get(dg).to_mesh_clear()
    rep[act.name]=dict(frames_bad=nf,worst_ratio=round(worst[0],2),frame=worst[1],edge=worst[2])
    print("STRETCH",act.name,rep[act.name])
bv=sorted(bad); print("STRETCH_VERTS",len(bv))
for vi in bv[:40]: print("  v",vi,[round(c,3) for c in V0[vi]],top(vi))
if OUT: json.dump(dict(thr=THR,clips=rep,verts=len(bv)),open(OUT,'w'),indent=1)
