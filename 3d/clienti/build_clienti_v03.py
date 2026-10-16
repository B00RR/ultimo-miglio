# Ultimo Miglio - Clienti modulari (sheet art/sheet_clienti_v01.jpg): base + 5 varianti.
# Proporzioni/scheletro = Nina Vela (stessi giunti, braccia giù 22°, pivot tra i piedi, min Z 0) -> l'Animator riusa il rig di Nina.
# Più tozzi (torso x1.18) e testa più grande (r 0.185). Accessori rigidi = isole separate con vertex group col nome del pezzo.
# blender -b -P build_clienti.py
# v03 (#2): budget ~1.5k tri (decimate corpo 0.62, meno segmenti su testa/capelli/occhi), + Nonna Ada (sheet art/sheet_nonna_ada_v01.jpg).
# Env: UM_ATLAS (atlas), UM_OUT (cartella export), UM_ONLY=nome1,nome2
import os,sys,math,random
from mathutils import Vector, Matrix
SRC=open("/workspace/ultimo-miglio/3d/final/build_final.py").read()
exec(SRC[:SRC.index("def build_floor")])          # palette, atlas, helper (cube/cyl/uvs/join/to_atlas), materiali toon
from mathutils.geometry import intersect_point_line
OUT=os.environ.get("UM_OUT",os.path.join(EXP,"chr")); os.makedirs(OUT,exist_ok=True)
ONLY=os.environ.get("UM_ONLY","ada").split(",")   # #2: di default SOLO Nonna Ada (i clienti v02 sono già riggati, commit 0c05198)
for n,h in (("arancio","E8913A"),): PAL.append((n,h)); IDX[n]=len(PAL)-1
_AJ.setdefault('arancio',_AJ['casco'])
def vg(o,name):
    g=o.vertex_groups.new(name=name); g.add(range(len(o.data.vertices)),1.0,'REPLACE'); return o
# --- scheletro Nina (sez. 9), braccia ruotate giù di 22° attorno alla spalla PRIMA dello skin
V=[Vector(v) for v in [(0,0,0.86),(0,0,1.02),(0,0.0,1.2),(0,0,1.34),
   (0.1,0,0.84),(0.11,-0.01,0.47),(0.115,0.0,0.12),(0.115,-0.11,0.06),
   (-0.1,0,0.84),(-0.11,-0.01,0.47),(-0.115,0.0,0.12),(-0.115,-0.11,0.06),
   (0.19,0,1.27),(0.33,0.0,1.05),(0.45,-0.02,0.85),(0.5,-0.03,0.76),
   (-0.19,0,1.27),(-0.33,0.0,1.05),(-0.45,-0.02,0.85),(-0.5,-0.03,0.76)]]
for sx,ids in ((1,(13,14,15)),(-1,(17,18,19))):
    piv=Vector((sx*0.19,0,1.27))
    for i in ids: V[i]=piv+(Matrix.Rotation(math.radians(sx*22),3,'Y')@(V[i]-piv))
print("JOINTS",[(i,tuple(round(c,3) for c in V[i])) for i in (12,13,14,15,0,3,5,6)])
E=[(0,1),(1,2),(2,3),(0,4),(4,5),(5,6),(6,7),(0,8),(8,9),(9,10),(10,11),(2,12),(12,13),(13,14),(14,15),(2,16),(16,17),(17,18),(18,19)]
BONE={(0,1):"spine",(1,2):"chest",(2,3):"neck",(0,4):"hip",(4,5):"thigh",(5,6):"shin",(6,7):"foot",(2,12):"clav",(12,13):"upper",(13,14):"fore",(14,15):"hand"}
for a,b in list(BONE): pass
def bone_of(e):
    a,b=e; k=(a,b)
    if k in BONE: return BONE[k]
    m={8:4,9:5,10:6,11:7,16:12,17:13,18:14,19:15}; k=(m.get(a,a),m.get(b,b)); return BONE.get(k,"spine")
def body(cfg):
    R={0:(0.19,0.13),1:(0.18,0.125),2:(0.215,0.14),3:(0.06,0.06),4:(0.095,0.095),5:(0.072,0.072),6:(0.066,0.075),7:(0.07,0.065),
       12:(0.078,0.078),13:(0.064,0.064),14:(0.05,0.05),15:(0.056,0.04)}
    for a,b in [(4,8),(5,9),(6,10),(7,11),(12,16),(13,17),(14,18),(15,19)]: R[b]=R[a]
    me=bpy.data.meshes.new("b"); me.from_pydata([tuple(v) for v in V],E,[]); o=bpy.data.objects.new("b",me); sc.collection.objects.link(o)
    sk=o.modifiers.new("skin","SKIN"); sk.use_smooth_shade=True
    for i,v in enumerate(me.skin_vertices[0].data): v.radius=R.get(i,(0.05,0.05))
    me.skin_vertices[0].data[0].use_root=True
    ss=o.modifiers.new("sub","SUBSURF"); ss.levels=1
    dc=o.modifiers.new("dec","DECIMATE"); dc.ratio=cfg.get("dec",0.62)
    setactive(o); bpy.ops.object.modifier_apply(modifier="skin"); bpy.ops.object.modifier_apply(modifier="sub"); bpy.ops.object.modifier_apply(modifier="dec"); bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))   # v02: smooth per angolo, niente V sul petto
    names=[]
    def mi(n):
        if n not in names: names.append(n); o.data.materials.append(cmat(n))
        return names.index(n)
    for p in o.data.polygons:
        c=p.center; best=None
        for e in E:
            q,t=intersect_point_line(c,V[e[0]],V[e[1]]); t=min(1,max(0,t)); d=(c-(V[e[0]]+(V[e[1]]-V[e[0]])*t)).length
            if best is None or d<best[0]: best=(d,bone_of(e),t)
        _,b,t=best
        if b=="hand": m="pelle"
        elif b=="fore": m=cfg["top"] if cfg.get("sleeve","long")=="long" else "pelle"
        elif b in ("upper","clav","chest","spine"): m=cfg["top"]
        elif b=="neck": m="pelle" if c.z>1.3 else cfg["top"]
        elif b in ("spine","chest","hip","thigh") and abs(c.x)<0.26: m=cfg["top"] if c.z>0.9 else cfg["legs"]          # pancia/fianchi sopra la vita = maglia
        elif b=="foot" or c.z<0.15: m=cfg["shoes"]
        elif c.z<0.22: m=cfg.get("socks",cfg["legs"] if cfg.get("pants_z",0.15)<=0.15 else "pelle")
        elif c.z<cfg.get("pants_z",0.15): m="pelle"
        else: m=cfg["legs"]
        p.material_index=mi(m)
    return o
def head(P,cfg,rnd):
    P.append(uvs((0,-0.01,1.53),0.185,"pelle",10,7,(1,0.95,1.05)))
    for sx in (1,-1):
        P.append(uvs((sx*0.065,-0.172,1.535),0.026,"capelli",6,3,(1,0.5,1.35)))     # occhi
        P.append(uvs((sx*0.1,-0.155,1.475),0.027,"guance",6,3,(1,0.4,0.7)))
        P.append(cube((sx*0.068,-0.172,1.59),(0.055,0.014,0.014),cfg.get("brow","capelli")))
        P.append(uvs((sx*0.183,-0.005,1.52),0.032,"pelle_d",5,3,(0.5,1,1.2)))         # orecchie
    P.append(uvs((0,-0.19,1.5),0.022,"pelle_d",5,3)); P.append(cube((0,-0.175,1.45),(0.055,0.01,0.012),"pelle_d"))
    if cfg.get("hair",True):
        hz=(1.55,0.78) if cfg.get("hat") else (1.58,0.92)    # sotto il cappello capelli più bassi (niente compenetrazioni)
        P.append(vg(uvs((0,0.03,hz[0]),0.19,cfg.get("hair_col","capelli"),10,5,(1.02,0.98,hz[1])),"hair"))
def acc(P,o,g): P.append(vg(o,g)); return o
# mano dx (x-) ~(-0.29,-0.03,0.70); mano sx (x+) ~(0.29,-0.03,0.70)
HR=Vector((-0.29,-0.03,0.70)); HL=Vector((0.29,-0.03,0.70))
def residente(P):
    acc(P,cyl((0,0.0,1.70),0.205,0.13,"terracotta_d",14,bev=0.02),"hat")             # coppola grande
    acc(P,cube((0,-0.2,1.64),(0.32,0.14,0.03),"terracotta_d",bev=0.01,rot=(0.2,0,0)),"hat")
    acc(P,uvs((0,-0.15,1.43),0.1,"capelli",8,4,(1.1,0.5,0.5)),"hair")               # barba corta
    acc(P,cube((0,-0.14,1.18),(0.05,0.02,0.2),"terracotta_d"),"shirt_placket")
    acc(P,ico(HR+Vector((-0.02,0,-0.2)),0.11,"legno",1,(1,0.8,1.1)),"bag")           # retina
    for k,c in enumerate(("arancio","limone","foglia")): acc(P,ico(HR+Vector((-0.02+0.04*(k-1),-0.05,-0.15-0.03*k)),0.05,c,1),"bag")
    acc(P,cyl(HR+Vector((0,0,-0.06)),0.012,0.12,"legno"),"bag")
def rider(P):
    acc(P,cyl((0,0.0,1.70),0.205,0.13,"terracotta",14,bev=0.02),"hat")
    acc(P,cube((0,-0.2,1.64),(0.32,0.14,0.03),"terracotta",bev=0.01,rot=(0.2,0,0)),"hat")
    acc(P,cube((0,0.27,1.12),(0.44,0.32,0.44),"menta_d",bev=0.03),"box")            # box consegne GRANDE (letto dall'alto)
    acc(P,cube((0,0.27,1.36),(0.4,0.28,0.03),"menta_l"),"box")
    acc(P,cube((0,0.435,1.14),(0.12,0.01,0.12),"sole"),"box")
    acc(P,cube((0.07,-0.15,1.2),(0.06,0.01,0.06),"sole"),"chest_logo")
    acc(P,uvs(HR+Vector((-0.03,-0.04,-0.06)),0.12,"bruno_m",10,6,(1,1,0.9)),"helmet")   # casco in mano
    acc(P,cube(HR+Vector((-0.03,-0.15,-0.06)),(0.16,0.02,0.07),"schermo"),"helmet")
def anziana(P):
    acc(P,uvs((0,0.05,1.6),0.205,"sole",12,7,(1.0,0.85,0.95)),"headscarf")   # v02: arretrato, faccia libera             # fazzoletto giallo GRANDE
    acc(P,ico((0,0.19,1.42),0.05,"sole_d",1),"headscarf")
    acc(P,cyl((0,0.02,1.18),0.27,0.3,"bruno",12,r2=0.12),"shawl")                   # scialle bruno #3B2A20
    acc(P,cyl((0,0,0.55),0.28,0.62,"bruno_m",12,r2=0.17),"skirt")                   # gonna lunga
    acc(P,cyl(HR+Vector((-0.02,-0.04,-0.35)),0.014,0.7,"legno",6),"cane")            # bastone
    acc(P,cube(HR+Vector((-0.02,-0.04,0.03)),(0.02,0.08,0.02),"legno"),"cane")
    acc(P,cyl(HL+Vector((0.07,-0.05,-0.08)),0.11,0.1,"legno",10,r2=0.13),"basket")   # cesto limoni
    for k in range(4): acc(P,ico(HL+Vector((0.07+0.05*math.cos(k*1.6),-0.05+0.05*math.sin(k*1.6),-0.02)),0.04,"limone",1),"basket")
    acc(P,cyl(HL+Vector((0.07,-0.05,0.02)),0.012,0.22,"legno",6,(0,math.pi/2,0)),"basket")
def turista(P):
    acc(P,cyl((0,0.0,1.7),0.33,0.025,"sole",16),"hat")                               # cappello a tesa larga GIALLO
    acc(P,cyl((0,0.0,1.76),0.17,0.12,"sole",12,r2=0.15),"hat")
    acc(P,cyl((0,0.0,1.73),0.175,0.035,"bruno",12),"hat")
    acc(P,cube((0,-0.17,1.08),(0.12,0.07,0.09),"schermo",bev=0.01),"camera")
    acc(P,cyl((0,-0.215,1.08),0.03,0.03,"vetro",8,(math.pi/2,0,0)),"camera")
    for sx in (1,-1): acc(P,cube((sx*0.1,-0.13,1.22),(0.015,0.02,0.24),"bruno",rot=(0,math.radians(sx*-25),0)),"camera")
    acc(P,cube(HR+Vector((-0.06,-0.1,0.06)),(0.2,0.01,0.26),"tufo_b",rot=(0,0,0.5)),"map")
    acc(P,cube(HL+Vector((0.05,0.0,0.02)),(0.06,0.18,0.14),"bruno_m",bev=0.01),"bag")
def vip(P):
    acc(P,ico((0,0.2,1.6),0.085,"capelli",1),"hair_bun")                                   # chignon dietro, sotto la tesa
    acc(P,cyl((0,0.0,1.72),0.32,0.025,"terracotta",16,rot=(0.12,0,0)),"hat")                    # v02: tesa larga terracotta (lettura dall'alto)
    acc(P,cyl((0,0.01,1.78),0.16,0.1,"terracotta",12,r2=0.14,rot=(0.12,0,0)),"hat")
    acc(P,cyl((0,0.007,1.745),0.165,0.03,"sole_d",12,rot=(0.12,0,0)),"hat")
    for sx in (1,-1): acc(P,cyl((sx*0.065,-0.19,1.54),0.045,0.02,"schermo",10,(math.pi/2,0,0)),"glasses")
    acc(P,cube((0,-0.19,1.565),(0.22,0.012,0.014),"sole_d"),"glasses")
    acc(P,cyl((0,0.0,0.55),0.26,0.66,"bruno_m",12,r2=0.21),"coat_skirt")
    for sx in (1,-1): acc(P,cube((sx*0.06,-0.15,0.85),(0.02,0.03,0.85),"sole_d"),"coat_trim")
    acc(P,cube((0,-0.15,0.92),(0.25,0.03,0.03),"sole_d"),"coat_trim")
    acc(P,cube(HR+Vector((0,-0.07,0.06)),(0.07,0.01,0.12),"glow"),"phone")
    for sx in (1,-1): acc(P,ico((sx*0.18,-0.03,1.47),0.015,"sole",1),"earrings")

def shell_front(o,ymax):
    # tiene solo le facce laterali frontali (centro y<ymax): guscio aperto aderente alla curva del cappotto
    import bmesh
    o.data.transform(o.matrix_world); o.matrix_world=Matrix()
    bm=bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.calc_center_median().y>ymax or abs(f.normal.z)>0.7],context='FACES')
    bm.to_mesh(o.data); bm.free(); return o
def front_y(o,z0,z1,xmax=0.09):
    vs=[(o.matrix_world@v.co) for v in o.data.vertices]; vs=[v.y for v in vs if z0<v.z<z1 and abs(v.x)<xmax]
    return min(vs)
def ada(P):
    # Nonna Ada v02 (review Concept Artist PR #85): grembiule aderente (guscio sulla curva del cardigan + pettorina dentro il colletto),
    # niente catenella occhiali, niente fiocco/asta sul fianco. Statura 0.88 di Nina: SOLO scala nel prefab (rig condiviso).
    acc(P,uvs((0,0.02,1.6),0.192,"crema",10,5,(1.03,1.0,0.8)),"hair")
    acc(P,ico((0,0.07,1.79),0.095,"crema",1,(1,1,0.9)),"hair_bun")
    acc(P,cyl((0.05,0.07,1.84),0.012,0.26,"sole",6,(0.3,1.2,0)),"pencil")
    for sx in (1,-1): acc(P,cyl((sx*0.068,-0.19,1.54),0.048,0.012,"sole_d",8,(math.pi/2,0,0)),"glasses")
    acc(P,cube((0,-0.19,1.545),(0.05,0.012,0.012),"sole_d"),"glasses")
    by=front_y(P[0],1.05,1.25)                                                        # fronte del torso
    acc(P,cube((0,by+0.006,1.2),(0.11,0.012,0.14),"menta"),"blouse_collar")           # scollo a V menta
    acc(P,cyl((0,-0.02,1.32),0.13,0.06,"menta",10),"blouse_collar")                    # colletto (copre la cima della pettorina)
    acc(P,cyl((0,0,0.68),0.255,0.5,"terracotta",12,r2=0.215),"cardigan_skirt")
    acc(P,shell_front(cyl((0,0,0.69),0.263,0.46,"bruno_m",12,r2=0.223),-0.1),"apron")  # telo grembiule sulla curva della gonna
    acc(P,cube((0,by-0.012,1.1),(0.22,0.01,0.38),"bruno_m"),"apron")                  # pettorina piatta sul petto, sale fin dentro il colletto (z 1.29)
    sy=-0.262*0.8                                                                       # fronte della gonna a z~0.72
    acc(P,cube((0,sy-0.012,0.72),(0.30,0.008,0.15),"stivali"),"apron")                 # tasche
    for x,c in ((-0.11,"sole"),(-0.085,"bruno"),(-0.06,"legno_l")): acc(P,cube((x,sy-0.02,0.82),(0.015,0.012,0.09),c),"apron")
    acc(P,cube((0.07,sy-0.022,0.8),(0.1,0.012,0.07),"tufo"),"ledger")
    acc(P,cube((0.11,by-0.01,1.24),(0.03,0.012,0.05),"sole"),"pin_faro")
    acc(P,cube((0.11,by-0.012,1.28),(0.035,0.012,0.015),"terracotta_d"),"pin_faro")
CFG={"base":dict(top="tufo_b",legs="bruno_m",shoes="stivali"),
 "residente":dict(top="terracotta",legs="bruno_m",shoes="stivali",sleeve="short",fn=residente,hat=True),
 "rider":dict(top="menta",legs="menta",shoes="bruno_m",fn=rider,hat=True),
 "anziana":dict(no_belt=True,top="bruno",legs="bruno",shoes="stivali",hair=False,fn=anziana,socks="bruno_m"),
 "turista":dict(top="limone",legs="bruno_m",shoes="bruno_m",sleeve="short",pants_z=0.5,socks="calze",fn=turista,hat=True),
 "ada":dict(no_belt=True,top="terracotta",legs="pantaloni",shoes="stivali",pants_z=0.15,brow="tufo_d",hair=False,fn=ada,name="chr_nonna_ada"),
 "vip":dict(no_belt=True,top="bruno_m",legs="bruno_m",shoes="bruno",pants_z=0.4,fn=vip,hat=True)}
res=[]
for name,cfg in CFG.items():
    if ONLY and name not in ONLY: continue
    rnd=random.Random(hash(name)%1000); P=[body(cfg)]; head(P,cfg,rnd)
    # v02: cintura che chiude pulita la linea vita (corpo, nessun gruppo: pesa su pelvis)
    if not cfg.get("no_belt"): P.append(cyl((0,0,0.9),0.205,0.07,cfg.get("belt","bruno"),14))
    for sx in (1,-1): P.append(cube((sx*0.115,-0.05,0.035),(0.15,0.28,0.07),cfg["shoes"] if cfg["shoes"]!="stivali" else "bruno_m",bev=0.02))   # suole (come Nina)
    if "fn" in cfg: cfg["fn"](P)
    o=join(P,cfg.get("name","chr_cliente_"+name)); to_atlas(o); o.data.validate()
    mz=min(v.co.z for v in o.data.vertices)   # nessuna traslazione: giunti identici a Nina; suole come Nina -> min Z 0
    for s_ in bpy.context.selected_objects: s_.select_set(False)
    o.select_set(True); bpy.context.view_layer.objects.active=o
    f=os.path.join(OUT,o.name)
    bpy.ops.export_scene.fbx(filepath=f+".fbx",use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,path_mode='STRIP')
    bpy.ops.export_scene.gltf(filepath=f+".glb",export_format='GLB',use_selection=True)
    nsc=bpy.data.scenes.new(o.name+"_v01"); nsc.collection.objects.link(o)
    bpy.data.libraries.write(f+".blend",{nsc},fake_user=True)
    res.append((o.name,tris(o),round(mz,4),[g.name for g in o.vertex_groups]))
    bpy.data.scenes.remove(nsc); bpy.data.objects.remove(o)   # v02: niente oggetti residui nei GLB successivi
print("CLIENTI",res)
