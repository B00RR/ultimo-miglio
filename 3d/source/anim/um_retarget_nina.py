# Ultimo Miglio - Nina: rig Humanoid adattato alla mesh finale (posa A) + re-bake delle 7 clip dal rig sorgente (T-pose).
# Uso: blender -b 3d/source/chr/chr_nina_vela_rig.blend -P um_retarget_nina.py [-- /path/chr_nina_vela_mesh.fbx]
# Metodo: ogni osso nuovo ha rest = rest sorgente ruotato sulla direzione misurata sulla mesh (stesso roll relativo),
# Copy Rotation (world) da sorgente -> bake visuale per action, Hips anche Copy Location (scala altezza).
import bpy, sys, os, math
from mathutils import Vector, Matrix
argv=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
ROOT=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),"..",".."))
MESH=argv[0] if argv else f"{ROOT}/export/chr/chr_nina_vela_mesh.fbx"
sc=bpy.context.scene; sc.render.fps=30; sc.render.fps_base=1.0   # FBX take a 30 fps
src=bpy.data.objects["chr_nina_vela_rig"]; src.name="SRC_rig"; src.data.name="SRC_rig"
for o in list(src.children): bpy.data.objects.remove(o)       # via la mesh blockout
CLIPS=[(a.name,int(a.frame_range[1]),a.use_cyclic) for a in bpy.data.actions]
for a in list(bpy.data.actions): a.name="SRC_"+a.name
if src.animation_data:
    for t in list(src.animation_data.nla_tracks): src.animation_data.nla_tracks.remove(t)
# --- mesh finale
before=set(bpy.data.objects)
if MESH.endswith(".blend"):   # v02+: .blend dell'artista con i vertex group dei pezzi rigidi
    with bpy.data.libraries.load(MESH) as (sf,df): df.objects=[n for n in sf.objects if n=="chr_nina_vela_mesh"]
    for o in df.objects: sc.collection.objects.link(o)
else: bpy.ops.import_scene.fbx(filepath=MESH)
new=[o for o in bpy.data.objects if o not in before]
for o in new:
    if o.type!='MESH': bpy.data.objects.remove(o)
mesh=[o for o in new if o.name in bpy.data.objects][0]; mesh.name="chr_nina_vela_LOD0"
bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); bpy.context.view_layer.objects.active=mesh
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
H=max((v.co.z for v in mesh.data.vertices)); K=H/1.72
ART={"jacket_edges":"Chest","scarf":"Chest","ponytail_01":"ponytail_01","ponytail_02":"ponytail_02"}   # 05_3D_PIPELINE "Nina v02"
ARTV={}
for g in mesh.vertex_groups:
    if g.name in ART:
        for v in mesh.data.vertices:
            if any(x.group==g.index and x.weight>0.5 for x in v.groups): ARTV[v.index]=ART[g.name]
PONY=any(b=="ponytail_01" for b in ARTV.values()); print("UM_ARTGROUPS",{k:sum(1 for b in ARTV.values() if b==v) for k,v in ART.items()})            # scala verticale rispetto al rig sorgente
# --- giunture misurate sulla mesh (posa A). x>0 = Left
def sym(s,v): return Vector((v[0] if s=="Left" else -v[0], v[1], v[2]))
# braccia MISURATE sulla mesh (la mesh 13:55 ha braccia piu' strette dei valori in §9): punta mano = vertice piu' basso con x>0.22
_av=[v.co for v in mesh.data.vertices if v.co.x>0.22 and 0.55<v.co.z<1.3]
_tip=min(_av,key=lambda c:c.z); _tipc=sum((c for c in _av if c.z<_tip.z+0.04),Vector())/len([c for c in _av if c.z<_tip.z+0.04])
_wr=Vector((_tipc.x-0.005,_tipc.y,_tip.z+0.09)); _sh=Vector((0.19,0,1.27)); _el=(_sh+_wr)/2+Vector((0.01,0.02,0))
ARMJ=(tuple(_el),tuple(_wr),(_tipc.x,_tipc.y,_tip.z+0.01)); print("UM_ARMJ elbow",[round(x,3) for x in _el],"wrist",[round(x,3) for x in _wr],"tip",round(_tip.z,3))
# giunture da 05_3D_PIPELINE.md §9 (posa braccia giu, ~10 gradi di abduzione)
J={"Root":((0,0,0),(0,0.25,0)),"Hips":((0,0,0.86),(0,0,0.98)),"Spine":((0,0,0.98),(0,0,1.12)),
   "Chest":((0,0,1.12),(0,0,1.34)),"Neck":((0,0,1.34),(0,0,1.42)),"Head":((0,0,1.42),(0,0,H)),
   "Backpack":((0,0.17,1.06),(0,0.17,1.26))}
for s_ in ("Left","Right"):
    sh=(0.19,0,1.27); el,wr,tip=ARMJ
    J[s_+"Shoulder"]=(sym(s_,(0.04,0,1.29)),sym(s_,sh)); J[s_+"Arm"]=(sym(s_,sh),sym(s_,el))
    J[s_+"ForeArm"]=(sym(s_,el),sym(s_,wr)); J[s_+"Hand"]=(sym(s_,wr),sym(s_,tip))
    J[s_+"UpLeg"]=(sym(s_,(0.11,0,0.84)),sym(s_,(0.11,0,0.47))); J[s_+"Leg"]=(sym(s_,(0.11,0,0.47)),sym(s_,(0.11,0.01,0.12)))
    J[s_+"Foot"]=(sym(s_,(0.11,0.01,0.12)),sym(s_,(0.11,-0.12,0.03))); J[s_+"ToeBase"]=(sym(s_,(0.11,-0.12,0.03)),sym(s_,(0.11,-0.21,0.03)))
J["Prop_R"]=(sym("Right",(0.53,-0.05,0.70)),sym("Right",(0.53,-0.05,0.82)))
# --- nuovo armature
ad=bpy.data.armatures.new("chr_nina_vela_rig"); arm=bpy.data.objects.new("chr_nina_vela_rig",ad); sc.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
for b in src.data.bones:
    h,t=Vector(J[b.name][0]),Vector(J[b.name][1]); eb=ad.edit_bones.new(b.name)
    Ms=b.matrix_local.to_3x3(); d_old=Ms.col[1].normalized(); d_new=(t-h).normalized()
    R=d_old.rotation_difference(d_new).to_matrix(); M=(R@Ms).to_4x4(); M.translation=h
    eb.head=h; eb.tail=t; eb.matrix=M; eb.length=(t-h).length; eb.use_deform=b.use_deform
for b in src.data.bones:
    if b.parent: ad.edit_bones[b.name].parent=ad.edit_bones[b.parent.name]
if PONY:   # coda: elastico (0,0.19,1.38) -> punta (0,0.205,1.30); ossa extra, il Humanoid le ignora
    e1=ad.edit_bones.new("ponytail_01"); e1.head=(0,0.125,1.445); e1.tail=(0,0.2,1.35); e1.parent=ad.edit_bones["Head"]
    e2=ad.edit_bones.new("ponytail_02"); e2.head=(0,0.2,1.35); e2.tail=(0,0.206,1.25); e2.parent=e1; e2.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
# --- skin: pesi automatici, max 4 influenze
bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active=arm
bpy.ops.object.parent_set(type='ARMATURE_AUTO')   # bone heat (fallisce sulle mesh a pezzi non-manifold)
unw=sum(1 for v in mesh.data.vertices if not v.groups)
if True:   # skinning deterministico (bone heat fallisce su questa mesh)
    # fallback: distanza dai segmenti ossei, 2 influenze pesate 1/d^4, lato sx/dx separato
    print("UM_SKIN bone heat failed on",unw,"verts -> distance skinning")
    for vg in list(mesh.vertex_groups): mesh.vertex_groups.remove(vg)
    from mathutils.geometry import intersect_point_line
    segs=[(b.name,b.head_local.copy(),b.tail_local.copy()) for b in ad.bones if b.use_deform and b.name not in ("Root","Prop_R")]
    ARMB=("Arm","ForeArm","Hand")
    def allowed(n,c):   # braccia giu vicino alle cosce: separa braccia/gambe per X
        isarm=n.endswith(ARMB)
        if abs(c.x)>0.225 and 0.55<c.z<1.1: return isarm
        if abs(c.x)<0.2 and c.z<0.95 and isarm: return False
        return True
    vgs={n:mesh.vertex_groups.new(name=n) for n,_,_ in segs}
    import bmesh
    bm=bmesh.new(); bm.from_mesh(mesh.data); bm.verts.ensure_lookup_table(); seen=set(); isl=[]
    for v0 in bm.verts:
        if v0.index in seen: continue
        st=[v0]; cur=[]; seen.add(v0.index)
        while st:
            x=st.pop(); cur.append(x.index)
            for e in x.link_edges:
                o=e.other_vert(x)
                if o.index not in seen: seen.add(o.index); st.append(o)
        isl.append(cur)
    isl.sort(key=len,reverse=True); rigid={}
    for I in isl[1:]:   # tutto tranne il corpo continuo (Skin modifier): pezzi rigidi
        if all(i in ARTV for i in I): continue
        c=sum((mesh.data.vertices[i].co for i in I),Vector())/len(I)
        if c.z>1.36: b_="Head"                        # testa, capelli, casco
        elif c.y>0.1 and c.z>0.95: b_="Chest"          # zaino
        else:                                          # altri pezzi (cintura, polsini, scarpe...): osso piu vicino
            from mathutils.geometry import intersect_point_line as ipl
            best=None
            for n,h,t in segs:
                if (n.startswith("Left") and c.x<-0.02) or (n.startswith("Right") and c.x>0.02) or n=="Backpack" or not allowed(n,c): continue
                p,f=ipl(c,h,t); f=min(1,max(0,f)); d=(c-(h+(t-h)*f)).length
                if best is None or d<best[0]: best=(d,n)
            b_=best[1]
        for i in I: rigid[i]=b_
    print("UM_SKIN islands",len(isl),"rigid verts",len(rigid))
    for v in mesh.data.vertices:
        if v.index in ARTV and ARTV[v.index]=="Chest" and v.co.z<1.12 and v.co.y<-0.12:   # bordi giacca aperta: flap sull'orlo
            z=v.co.z; side="Left" if v.co.x>0 else "Right"
            if z<1.0:
                a_=min(0.5,(1.0-z)/0.09*0.5); vgs["Hips"].add([v.index],1-a_,'REPLACE'); vgs[side+"UpLeg"].add([v.index],a_,'REPLACE')
            else:
                a_=(z-1.0)/0.12; vgs["Spine"].add([v.index],1-a_,'REPLACE'); vgs["Chest"].add([v.index],a_,'REPLACE')
            continue
        if v.index in ARTV: vgs.setdefault(ARTV[v.index],mesh.vertex_groups.get(ARTV[v.index]) or mesh.vertex_groups.new(name=ARTV[v.index])).add([v.index],1.0,'REPLACE'); continue
        if v.index in rigid: vgs[rigid[v.index]].add([v.index],1.0,'REPLACE'); continue
        co=v.co; ds=[]
        for n,h,t in segs:
            if (n.startswith("Left") and co.x<-0.02) or (n.startswith("Right") and co.x>0.02): continue
            if n=="Backpack" and co.y<0.08: continue
            if not allowed(n,co): continue
            p,f=intersect_point_line(co,h,t); f=min(1,max(0,f)); d=(co-(h+(t-h)*f)).length
            ds.append((d,n))
        ds.sort(); ds=ds[:2]; w=[1/max(d,0.01)**4 for d,_ in ds]
        if len(ds)>1 and ds[1][0]>ds[0][0]*1.6: w[1]=0
        tot=sum(w)
        for (d,n),wi in zip(ds,w):
            if wi>0: vgs[n].add([v.index],wi/tot,'REPLACE')
bpy.context.view_layer.objects.active=mesh
bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL',limit=4)
# Torso/giacca: nessuna influenza delle ossa del braccio (Cheer stirava la giacca dai polsi ai fianchi).
# Un vertice e' "torso" se sta all'interno della linea spalla->gomito meno il raggio del braccio, oppure e' jacket_edges/scarf.
_sh=Vector((0.17,0,1.28)); _el=Vector(ARMJ[0])
def _armx(z):
    t=max(0.0,min(1.0,(_sh.z-z)/(_sh.z-_el.z))); return _sh.x+(_el.x-_sh.x)*t
ARMB=[s+n for s in ("Left","Right") for n in ("Arm","ForeArm","Hand")]
_nt=0
for v in mesh.data.vertices:
    c=v.co
    torso=(ARTV.get(v.index) in ("Chest",) ) or (0.86<c.z<1.24 and abs(c.x)<_armx(c.z)-0.035)
    if not torso: continue
    wa=0.0
    for b in ARMB+(["LeftShoulder","RightShoulder"] if c.z<1.2 else []):
        g=vgs.get(b) or mesh.vertex_groups.get(b)
        try: w=g.weight(v.index)
        except RuntimeError: continue
        wa+=w; g.remove([v.index])
    if wa>0:
        _nt+=1; a_=max(0.0,min(1.0,(c.z-1.0)/0.15))
        if c.z<0.95: mesh.vertex_groups["Hips"].add([v.index],wa,'ADD')
        else:
            mesh.vertex_groups["Spine"].add([v.index],wa*(1-a_),'ADD'); mesh.vertex_groups["Chest"].add([v.index],wa*a_,'ADD')
print("UM_TORSO arm influence removed from",_nt,"verts")
# Mesh v02: i pannelli laterali della giacca sono saldati all'avambraccio (spigoli di ~4 cm torso<->braccio).
# Qualsiasi pesatura li stira quando il braccio si alza: stacchiamo le facce miste duplicando i vertici di confine
# (stessa posizione/UV, nessun triangolo in piu'); ogni faccia segue solo il lato maggioritario.
import bmesh
def _split_arm_torso():
    gi={g.name:g.index for g in mesh.vertex_groups}
    AR={gi[n] for n in ARMB if n in gi}; TO={gi[n] for n in ("Hips","Spine","Chest") if n in gi}
    bm=bmesh.new(); bm.from_mesh(mesh.data); dl=bm.verts.layers.deform.verify(); bm.verts.ensure_lookup_table()
    def cls(v):
        d=v[dl]
        if not d: return None
        g=max(d.items(),key=lambda kv:kv[1])[0]; return "A" if g in AR else ("T" if g in TO else None)
    C={v.index:cls(v) for v in bm.verts}; dup={}; nf=0
    for f in list(bm.faces):
        cs=[C[v.index] for v in f.verts]
        if "A" not in cs or "T" not in cs: continue
        side="A" if cs.count("A")>cs.count("T") else "T"
        donors=[v for v,c in zip(f.verts,cs) if c==side]
        avg={}
        for v in donors:
            for g,w in v[dl].items(): avg[g]=avg.get(g,0)+w/len(donors)
        for lp in f.loops:
            v=lp.vert
            if C[v.index] in (side,None) and C[v.index]==side: continue
            if C[v.index]==side: continue
            key=(v.index,side)
            if key not in dup:
                nv=bm.verts.new(v.co); nv[dl].clear()
                for g,w in avg.items(): nv[dl][g]=w
                dup[key]=nv
        verts=[dup.get((v.index,side),v) if C[v.index]!=side else v for v in f.verts]
        uvs=[[lp[l].uv.copy() for l in bm.loops.layers.uv.values()] for lp in f.loops]
        nfc=bm.faces.new(verts,f); 
        for lp,uv in zip(nfc.loops,uvs):
            for l,u in zip(bm.loops.layers.uv.values(),uv): lp[l].uv=u
        bm.faces.remove(f); nf+=1
    loose=[e for e in bm.edges if not e.link_faces]; [bm.edges.remove(e) for e in loose]   # spigoli orfani delle facce rifatte
    bm.to_mesh(mesh.data); bm.free(); mesh.data.update()
    print("UM_SPLIT arm/torso faces",nf,"dup verts",len(dup))
_split_arm_torso()
# cavallo: i vertici sull'asse (x~0) seguivano una sola coscia -> stiramento x3 in Run; li rendiamo simmetrici
for v in mesh.data.vertices:
    if abs(v.co.x)<0.025 and 0.68<v.co.z<0.86:
        for n in ("LeftUpLeg","RightUpLeg","Hips"):
            mesh.vertex_groups[n].add([v.index],{"Hips":0.6}.get(n,0.2),'REPLACE')
bpy.ops.object.vertex_group_normalize_all(lock_active=False)
unw=sum(1 for v in mesh.data.vertices if not v.groups); print("UM_SKIN unweighted verts",unw,"of",len(mesh.data.vertices))
# --- re-bake: orientamento assoluto (pose space) copiato osso per osso dal rig sorgente, posizioni dalla gerarchia nuova.
# Calcolo diretto in matrici (niente vincoli Copy Rotation: con le braccia sopra la testa la conversione euler ribalta gli avambracci).
src.animation_data_create(); arm.animation_data_create()
order=[]; 
def walk(b):
    order.append(b.name); [walk(c) for c in b.children]
for b in ad.bones:
    if not b.parent: walk(b)
for name,N,cyc in CLIPS:
    src.animation_data.action=bpy.data.actions["SRC_"+name]
    act=bpy.data.actions.new(name); arm.animation_data.action=act
    for f in range(N+1):
        sc.frame_set(f); Mn={}
        for n in order:
            b=ad.bones[n]; sp=src.pose.bones.get(n)
            if sp is None:   # ossa extra (coda): pose a riposo qui, simulazione secondaria dopo
                Mn[n]=Mn[b.parent.name]@(b.parent.matrix_local.inverted()@b.matrix_local); pb=arm.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=Matrix()
                for k in ("location","rotation_quaternion","scale"): pb.keyframe_insert(k,frame=f)
                continue
            rot=sp.matrix.to_quaternion()
            if b.parent is None or n=="Hips": head=sp.matrix.translation.copy()
            else:
                rel=b.parent.matrix_local.inverted()@b.matrix_local
                head=(Mn[b.parent.name]@rel).translation
                if n in ("Backpack","Prop_R"): head=head+(sp.matrix.translation-sp.head) if False else head
            M=Matrix.LocRotScale(head,rot,None); Mn[n]=M
            if b.parent is None: basis=b.matrix_local.inverted()@M
            else: basis=(b.parent.matrix_local.inverted()@b.matrix_local).inverted()@Mn[b.parent.name].inverted()@M
            if n in ("Backpack","Prop_R"): basis.translation=basis.translation+src.pose.bones[n].location   # rimbalzo pila / pacco
            pb=arm.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=basis
            pb.keyframe_insert("location",frame=f); pb.keyframe_insert("rotation_quaternion",frame=f); pb.keyframe_insert("scale",frame=f)
    act.use_fake_user=True; act.frame_range=(0,N); act.use_frame_range=True; act.use_cyclic=cyc
# --- coda: follow-through/overlap simulato (molla smorzata in world space, 2 segmenti), poi baked.
# Vincoli: resta dietro la testa (y locale testa >= rest*0.6), max 0.7 rad dal riposo, non scende sotto il bordo zaino.
def sim_pony(act,N,cyc):
    arm.animation_data.action=act; dt=1/30
    segs=[("ponytail_01","Head"),("ponytail_02","ponytail_01")]
    st={n:None for n,_ in segs}; vel={n:Vector() for n,_ in segs}; prev_root={n:None for n,_ in segs}; prev_v={n:Vector() for n,_ in segs}
    frames=list(range(N))*3+[N] if cyc else [0]*20+list(range(N+1))   # loop: 3 giri per regime; non-loop: assestamento iniziale
    out={}
    for k,f in enumerate(frames):
        sc.frame_set(f)
        for n,par in segs:
            pb=arm.pose.bones[n]; b=ad.bones[n]
            Mp=arm.pose.bones[par].matrix if par=="Head" else Mcur[par]
            rel=b.parent.matrix_local.inverted()@b.matrix_local
            M0=Mp@rel; root=M0.translation.copy(); rest_dir=M0.to_3x3().col[1].normalized()
            Rh=arm.pose.bones["Head"].matrix.to_3x3()@ad.bones["Head"].matrix_local.to_3x3().inverted()
            back=(Rh@Vector((0,1,0))).normalized()          # "dietro" della testa in world
            target=(rest_dir*0.7+back*0.25+Vector((0,0,-1))*0.12).normalized()
            if st[n] is None: st[n]=rest_dir.copy()
            rv=(root-prev_root[n])/dt if prev_root[n] is not None else Vector(); acc=(rv-prev_v[n])/dt if prev_root[n] is not None else Vector()
            prev_root[n]=root; prev_v[n]=rv
            K,Cd,G=(140,13,0.022) if n=="ponytail_01" else (110,10,0.03)
            a=(target-st[n])*K-vel[n]*Cd-acc*G
            vel[n]=vel[n]+a*dt; d=(st[n]+vel[n]*dt).normalized()
            # vincoli anti-compenetrazione: angolo max e dietro la testa
            Hm=arm.pose.bones["Head"].matrix.to_3x3()
            ang=rest_dir.angle(d)
            if ang>0.7: d=rest_dir.slerp(d,0.7/ang) if hasattr(rest_dir,'slerp') else (rest_dir.lerp(d,0.7/ang)).normalized()
            mind=rest_dir.dot(back)-0.05                 # mai piu' "avanti" del riposo: niente coda dentro sciarpa/nuca
            if d.dot(back)<mind: d=(d+back*(mind-d.dot(back))*1.2).normalized(); vel[n]=vel[n]*0.3
            st[n]=d
            q=rest_dir.rotation_difference(d); R=q.to_matrix()@M0.to_3x3()
            M=Matrix.LocRotScale(root,R.to_quaternion(),None)
            if 'Mcur' not in globals(): globals()['Mcur']={}
            Mcur[n]=M
            if k>=len(frames)-(N+1 if not cyc else N+1) :
                out.setdefault(f,{})[n]=(rel.inverted()@Mp.inverted()@M)
    for f,d in out.items():
        for n,basis in d.items():
            pb=arm.pose.bones[n]; pb.matrix_basis=basis
            for kk in ("location","rotation_quaternion","scale"): pb.keyframe_insert(kk,frame=f)
if PONY:
    for name,N,cyc in CLIPS: sim_pony(bpy.data.actions[name],N,cyc)
    print("UM_PONY simulated",len(CLIPS),"clips")
arm.animation_data.action=bpy.data.actions["Idle"]
for a in [a for a in bpy.data.actions if a.name.startswith("SRC_")]: bpy.data.actions.remove(a)
bpy.data.objects.remove(src)
for a in bpy.data.actions: print("UM_CLIP",a.name,tuple(a.frame_range),a.use_cyclic)
bpy.ops.wm.save_as_mainfile(filepath=f"{ROOT}/source/chr/chr_nina_vela_final_rig.blend")
bpy.ops.object.select_all(action='SELECT')
sc.render.fps=30; sc.render.fps_base=1.0   # l import FBX della mesh imposta i fps del file (24)
bpy.ops.export_scene.fbx(filepath=f"{ROOT}/export/chr/chr_nina_vela.fbx", object_types={'ARMATURE','MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', use_armature_deform_only=True,
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE', path_mode='AUTO')
print("UM_DONE tris",sum(len(p.vertices)-2 for p in mesh.data.polygons))
