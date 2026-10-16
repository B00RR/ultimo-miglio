# KIT DEPOSITO v03 (issue #47, 16/10/2026) – deriva dal premium pass v1 (15/10/2026) - kit Bancone stile "chibi low-poly levigato": smussi ovunque, zoccolo + cornice, pavimento a piastrelle con fughe,
# bancone con modanature, pacchi bombati con nastro+etichetta, scaffale carico, piante in vaso, lampione, insegne. Solo swatch T_Palette_Atlas.
import bpy,bmesh,json,math,os,random,sys
from mathutils import Vector,Matrix
import pathlib
R=str(pathlib.Path(__file__).resolve().parents[2])  # radice del repo
ATL=R+'/Assets/_Game/Art/Textures/T_Palette_Atlas.png'
EX={e['name']:e['uv']['center'] for e in json.load(open(ATL.replace('.png','.json')))['extras']}
KIT=R+'/Assets/_Game/Art/Environment/Kit'; PROPS=R+'/Assets/_Game/Art/Props'; GLB=os.environ.get('UM_GLB_OUT','/tmp/um_glb_v03')
os.makedirs(GLB,exist_ok=True)
REP=[]
class P:
    def __init__(s): s.bm=bmesh.new(); s.uv=s.bm.loops.layers.uv.new('UV0')
    def _add(s,tmp,col,bev=0.0,seg=1,ang=None):
        if bev>0:
            ed=tmp.edges[:] if ang is None else [e for e in tmp.edges if len(e.link_faces)==2 and e.calc_face_angle()>math.radians(ang)]
            bmesh.ops.bevel(tmp,geom=ed,offset=bev,segments=seg,affect='EDGES',profile=0.5,clamp_overlap=True)
        for lay in list(tmp.loops.layers.uv.values()): tmp.loops.layers.uv.remove(lay)
        u=tmp.loops.layers.uv.new('UV0')
        for f in tmp.faces:
            c=col(f) if callable(col) else col
            if c.startswith('EMI:'): c=c[4:]; f.material_index=1
            else: f.material_index=0
            for l in f.loops: l[u].uv=EX[c]
        me=bpy.data.meshes.new('t'); tmp.to_mesh(me); tmp.free(); s.bm.from_mesh(me); bpy.data.meshes.remove(me)
    def box(s,a,b,col,bev=0.012,seg=1):
        t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0)
        c=[(a[i]+b[i])/2 for i in range(3)]; d=[abs(b[i]-a[i]) for i in range(3)]
        bmesh.ops.transform(t,matrix=Matrix.Translation(c)@Matrix.Diagonal((*d,1)),verts=t.verts); s._add(t,col,min(bev,min(d)*0.45),seg)
    def cyl(s,x,y,z0,z1,r,col,seg=8,bev=0.01,r2=None,rot=None):
        t=bmesh.new(); bmesh.ops.create_cone(t,cap_ends=True,segments=seg,radius1=r,radius2=r if r2 is None else r2,depth=z1-z0)
        m=Matrix.Translation((x,y,(z0+z1)/2)) if rot is None else Matrix.Translation((x,y,z0))@rot@Matrix.Translation((0,0,(z1-z0)/2))
        bmesh.ops.transform(t,matrix=m,verts=t.verts); s._add(t,col,bev,1,ang=30)
    def blob(s,x,y,z,r,col,sc=(1,1,1),sub=1,seed=0):
        t=bmesh.new(); bmesh.ops.create_icosphere(t,subdivisions=sub,radius=r); rr=random.Random(seed)
        for v in t.verts: v.co*=1+rr.uniform(-0.08,0.08)
        bmesh.ops.transform(t,matrix=Matrix.Translation((x,y,z))@Matrix.Diagonal((*sc,1)),verts=t.verts); s._add(t,col)
    def tris(s): return sum(len(f.verts)-2 for f in s.bm.faces)
def mat():
    m=bpy.data.materials.get('M_Diorama_Atlas') or bpy.data.materials.new('M_Diorama_Atlas'); m.use_nodes=True
    if not any(n.type=='TEX_IMAGE' for n in m.node_tree.nodes):
        t=m.node_tree.nodes.new('ShaderNodeTexImage'); t.image=bpy.data.images.load(ATL,check_existing=True); t.interpolation='Closest'
        m.node_tree.links.new(t.outputs[0],m.node_tree.nodes['Principled BSDF'].inputs['Base Color'])
    return m
def export(p,name,folder=KIT):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bmesh.ops.remove_doubles(p.bm,verts=p.bm.verts,dist=1e-5)
    me=bpy.data.meshes.new(name); p.bm.to_mesh(me); p.bm.free(); me.materials.append(mat())
    if any(f.material_index==1 for f in me.polygons):
        em=bpy.data.materials.get('M_Diorama_Atlas_Emissive') or bpy.data.materials.new('M_Diorama_Atlas_Emissive'); me.materials.append(em)
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o); bpy.context.view_layer.objects.active=o; o.select_set(True)
    tri=sum(len(f.vertices)-2 for f in me.polygons)
    bpy.ops.export_scene.fbx(filepath=f'{folder}/{name}.fbx',use_selection=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,mesh_smooth_type='FACE',path_mode='STRIP',add_leaf_bones=False)
    bpy.ops.export_scene.gltf(filepath=f'{GLB}/{name}.glb',use_selection=True,export_format='GLB')
    d=o.dimensions; REP.append(f'{name}\t{tri} tri\t{d.x:.2f}x{d.y:.2f}x{d.z:.2f} m\t{os.path.relpath(folder,R)}\n'); print('EXP',name,tri)
# ---------- MURI (pivot inizio, x 0..L, spessore y ±0.24, h 3.2; lato interno = -y)
def wall(name,L=2.0,variant='a',seed=1):
    p=P(); rr=random.Random(seed)
    p.box((0.01,-0.17,0.0),(L-0.01,0.17,3.2),'tufo_b',0.02)                               # anima
    rows=[(0.32,0.95),(0.95,1.55),(1.55,2.15),(2.15,2.82)]
    for k,(z0,z1) in enumerate(rows):
        if variant=='b' and k>=2:   # parte alta intonacata crema con fascia
            p.box((0.02,-0.2,z0),(L-0.02,-0.17,z1),'tufo',0.015); continue
        n=2 if k%2==0 else 3; xs=[0.02]+sorted(rr.uniform(0.35,L-0.35) for _ in range(n-1))+[L-0.02] if n>1 else [0.02,L-0.02]
        if k%2==1: xs=[0.02,L/3+rr.uniform(-.1,.1),2*L/3+rr.uniform(-.1,.1),L-0.02]
        for a,b in zip(xs,xs[1:]):
            p.box((a+0.02,-0.21,z0+0.02),(b-0.02,-0.17,z1-0.02),rr.choice(['tufo','tufo','tufo_b','intonaco_sole' if variant=='a' and rr.random()<0.15 else 'tufo']),0.03,2)
    if variant=='b': p.box((0.0,-0.215,2.12),(L,-0.17,2.2),'terracotta',0.015)              # fascia
    p.box((0.0,-0.24,0.0),(L,-0.17,0.32),'terracotta_d',0.025,2)                           # zoccolo
    p.box((0.0,-0.225,0.32),(L,-0.17,0.36),'terracotta',0.012)
    p.box((0.0,-0.24,2.98),(L,0.24,3.2),'terracotta',0.03,2)                               # cornice
    p.box((0.0,-0.205,2.86),(L,0.205,2.98),'tufo_c',0.02)
    p.box((0.0,0.17,0.0),(L,0.24,0.3),'tufo_d',0.02)                                       # zoccolo esterno
    export(p,name)
def corner():
    p=P(); p.box((-0.26,-0.26,0),(0.26,0.26,3.2),'tufo_b',0.04,2)
    p.box((-0.28,-0.28,0),(0.28,0.28,0.32),'terracotta_d',0.03,2); p.box((-0.28,-0.28,2.98),(0.28,0.28,3.2),'terracotta',0.03,2)
    export(p,'env_wall_corner')
# ---------- PAVIMENTO 2x2 (pivot angolo, top a ~0.03): fondo-fuga + 16 piastrelle smussate
def floor(name,variant):
    p=P(); rr=random.Random(hash(name)%100)
    p.box((0,0,-0.14),(2,2,0.0),'tufo_d',0.0)
    n=4; s=2/n
    for i in range(n):
        for j in range(n):
            if variant=='a': col=['tufo','tufo_b'][(i+j)%2]
            else: col=['intonaco_terra','terracotta' if rr.random()<0.5 else 'intonaco_terra'][(i+j)%2]
            h=0.03+rr.uniform(-0.004,0.004)
            p.box((i*s+0.018,j*s+0.018,-0.01),(i*s+s-0.018,j*s+s-0.018,h),col,0.014,1)
    export(p,name)
def floor_small(name,variant):
    rr=random.Random(7 if variant=='a' else 13); t=bmesh.new()
    bmesh.ops.create_grid(t,x_segments=8,y_segments=8,size=1.0)
    bmesh.ops.transform(t,matrix=Matrix.Translation((1,1,0)),verts=t.verts)
    tops=t.faces[:]
    r=bmesh.ops.inset_individual(t,faces=tops,thickness=0.014,depth=0.012)
    pal=['tufo','tufo','tufo_b','crema','tufo_b'] if variant=='a' else ['tufo_b','tufo','tufo_c','tufo_b','crema']
    topset=set(tops); colmap={f:rr.choice(pal) for f in tops}
    def col(f): return colmap[f] if f in topset else 'tufo_c'
    p=P(); p._add(t,col)
    p.box((0,0,-0.14),(2,2,-0.0005),'tufo_d',0.0)
    export(p,name)
# ---------- BANCONE 2m (pivot centro base, 2.08x0.88x1.05)
def counter():   # v03: banco in legno noce, pannelli a cornice, piano chiaro, cucitura ambra emissiva sotto il piano
    p=P()
    p.box((-1.0,-0.38,0),(1.0,0.38,0.1),'bruno',0.02)
    p.box((-0.98,-0.36,0.1),(0.98,0.36,0.93),'bruno_m',0.02)
    for x0 in (-0.95,-0.32,0.31):
        p.box((x0+0.04,-0.4,0.18),(x0+0.6,-0.36,0.84),'bruno_m',0.02,2)
        p.box((x0+0.1,-0.415,0.24),(x0+0.54,-0.395,0.78),'legno',0.012)
    p.box((-1.0,-0.405,0.865),(1.0,0.405,0.885),'EMI:glow',0.0)          # cucitura ambra
    p.box((-1.0,-0.4,0.885),(1.0,0.4,0.93),'bruno',0.01)
    p.box((-1.04,-0.44,0.93),(1.04,0.44,1.03),'legno_l',0.03,2)
    export(p,'prop_counter_2m')
# ---------- PACCHI (pivot centro base, top/bottom piatti, fianchi bombati, nastro + etichetta)
def parcel(name,W,D,H,label=True,tape='crema',body='cartone',seed=0,bulge=0.012):
    t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0)
    bmesh.ops.transform(t,matrix=Matrix.Translation((0,0,H/2))@Matrix.Diagonal((W,D,H,1)),verts=t.verts)
    bmesh.ops.bevel(t,geom=t.edges[:],offset=min(W,D,H)*0.07,segments=1,affect='EDGES',profile=0.5)
    g=t.verts[:]+t.edges[:]+t.faces[:]; r=bmesh.ops.bisect_plane(t,geom=g,plane_co=(0,0,H/2),plane_no=(0,0,1))
    for v in [e for e in r['geom_cut'] if isinstance(e,bmesh.types.BMVert)]:
        v.co.x*=1+bulge/(W/2)*1.0; v.co.y*=1+bulge/(D/2)
    tw=min(W,D)*0.11
    for c in (tw,-tw):
        g=t.verts[:]+t.edges[:]+t.faces[:]; bmesh.ops.bisect_plane(t,geom=g,plane_co=(0,c,0),plane_no=(0,1,0))
    def col(f):
        c=f.calc_center_median(); n=f.normal
        if abs(c.y)<tw and n.z>-0.5 and abs(n.y)<0.6: return tape
        if n.z<-0.6: return 'cartone_d'
        return body
    p=P(); p._add(t,col)
    if label:  # etichetta crema con riga scura sul fianco -y
        p.box((W*0.08,-D/2-bulge-0.004,H*0.35),(W*0.42,-D/2-bulge+0.004,H*0.75),'crema',0.0)
        p.box((W*0.12,-D/2-bulge-0.008,H*0.58),(W*0.38,-D/2-bulge-0.002,H*0.64),'bruno_m',0.0)
    export(p,name,PROPS)
# ---------- SCAFFALE CARICO (2x0.6x2, pivot centro base)
def shelf_loaded():
    p=P(); rr=random.Random(5)
    for x in (-0.97,0.97):
        for y in (-0.26,0.26): p.box((x-0.035,y-0.035,0),(x+0.035,y+0.035,2.0),'legno',0.012)
    p.box((-1.0,-0.3,1.96),(1.0,0.3,2.04),'legno_l',0.02)
    for z in (0.12,0.72,1.32):
        p.box((-1.0,-0.3,z),(1.0,0.3,z+0.05),'legno_l',0.015); p.box((-1.0,-0.305,z-0.03),(1.0,-0.28,z+0.05),'terracotta',0.008)
        x=-0.92
        while x<0.8:
            w=rr.choice([0.3,0.36,0.42]); h=rr.choice([0.26,0.32,0.4]); d=rr.choice([0.3,0.36])
            col=rr.choice(['cartone','cartone','cartone_d','legno_l'])
            p.box((x,-d/2,z+0.05),(x+w,d/2,z+0.05+h),col,0.025)
            p.box((x+w/2-0.025,-d/2-0.003,z+0.05),(x+w/2+0.025,d/2+0.003,z+0.05+h+0.003),'crema',0.0)
            if rr.random()<0.4: p.box((x+w*0.6,-d/2-0.006,z+0.12),(x+w*0.9,-d/2,z+0.22),'crema',0.0)
            x+=w+0.04
    p.box((-0.9,0.22,1.8),(-0.88,0.28,1.96),'sole',0.0)    # targhetta colore (tag A)
    p.box((-0.96,-0.33,1.8),(-0.7,-0.3,1.94),'sole',0.008)
    export(p,'prop_shelf_2m_loaded')
# ---------- PIANTE
def plant_pot(name,kind,seed):
    p=P(); rr=random.Random(seed)
    if kind=='round':
        p.cyl(0,0,0,0.38,0.2,'terracotta',10,0.015,r2=0.25); p.cyl(0,0,0.36,0.42,0.27,'terracotta_d',10,0.012)
        p.cyl(0,0,0.38,0.41,0.23,'bruno_m',10,0)
        for i in range(5):
            a=i*1.25+rr.random(); p.blob(0.12*math.cos(a),0.12*math.sin(a),0.55+rr.uniform(0,0.15),0.2,rr.choice(['foglia','foglia_l']),(1,1,1.1),1,i)
        p.blob(0,0,0.78,0.22,'foglia',(1,1,1.2),1,9)
        for i in range(3): p.blob(rr.uniform(-.15,.15),rr.uniform(-.2,-.1),0.6+rr.uniform(0,.2),0.045,rr.choice(['terracotta','sole','crema']),(1,1,1),0,i)   # fiori
    elif kind=='tall':
        p.cyl(0,0,0,0.5,0.17,'intonaco_terra',8,0.015,r2=0.21); p.cyl(0,0,0.48,0.54,0.23,'terracotta',8,0.012)
        for k,(z,r) in enumerate(((0.75,0.24),(1.05,0.2),(1.3,0.15),(1.48,0.09))): p.blob(0,0,z,r,['foglia','foglia_l'][k%2],(1,1,1.25),1,k)
    else:  # fioriera in legno
        p.box((-0.4,-0.16,0),(0.4,0.16,0.3),'legno',0.02); p.box((-0.42,-0.18,0.27),(0.42,0.18,0.31),'legno_l',0.015)
        for i in range(5):
            x=-0.3+i*0.15; p.blob(x,0,0.38,0.11,'foglia_l',(1,1,0.9),1,i); p.blob(x+0.03,-0.06,0.46,0.04,['terracotta','sole','limone','crema','terracotta'][i],(1,1,1),0,i)
    export(p,name)
# ---------- LAMPIONE (pivot base)
def streetlamp():
    p=P()
    p.cyl(0,0,0,0.12,0.16,'bruno',8,0.02,r2=0.12); p.cyl(0,0,0.12,2.6,0.045,'bruno',8,0.0,r2=0.035)
    p.cyl(0,0,0.5,0.62,0.07,'bruno_m',8,0.01)
    p.box((-0.03,-0.03,2.55),(0.5,0.03,2.6),'bruno',0.01)
    p.cyl(0.5,0,2.0,2.1,0.06,'bruno',8,0.01)   # coppa sotto
    p.cyl(0.5,0,2.1,2.4,0.1,'vetro',6,0.0,r2=0.13)
    p.cyl(0.5,0,2.4,2.5,0.16,'bruno',6,0.02,r2=0.04)
    p.cyl(0.5,0,2.5,2.57,0.025,'bruno',6,0)
    export(p,'prop_streetlamp')
# ---------- INSEGNE
def sign_hanging():
    p=P()   # insegna a bandiera: staffa a muro (pivot sul muro, sporge verso -y)
    p.box((-0.03,-0.04,0.0),(0.03,0.0,0.12),'bruno',0.008); p.box((-0.015,-0.72,0.1),(0.015,0.0,0.13),'bruno',0.006)
    for y in (-0.62,-0.12): p.box((-0.006,y-0.006,-0.02),(0.006,y+0.006,0.1),'bruno',0)
    p.box((-0.03,-0.68,-0.48),(0.03,-0.06,-0.02),'terracotta',0.02,2)
    p.box((-0.036,-0.63,-0.43),(0.036,-0.11,-0.07),'sole',0.012)
    p.box((-0.04,-0.47,-0.33),(0.04,-0.27,-0.17),'cartone',0.012)   # icona pacco
    p.box((-0.042,-0.38,-0.33),(0.042,-0.36,-0.17),'crema',0)
    export(p,'prop_sign_hanging_parcel')
def sign_board():
    p=P()   # lavagna a cavalletto "APERTO"
    for s_ in (-1,1):
        rot=Matrix.Rotation(math.radians(12*s_),4,'X')
        t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0); bmesh.ops.transform(t,matrix=Matrix.Translation((0,s_*0.12,0.5))@rot@Matrix.Diagonal((0.6,0.04,1.0,1)),verts=t.verts); p._add(t,'legno',0.015)
        t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0); bmesh.ops.transform(t,matrix=Matrix.Translation((0,s_*0.145,0.55))@rot@Matrix.Diagonal((0.5,0.012,0.7,1)),verts=t.verts); p._add(t,'schermo',0.0)
        t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0); bmesh.ops.transform(t,matrix=Matrix.Translation((0,s_*0.153,0.7))@rot@Matrix.Diagonal((0.34,0.006,0.06,1)),verts=t.verts); p._add(t,'crema',0.0)
        t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0); bmesh.ops.transform(t,matrix=Matrix.Translation((0,s_*0.153,0.52))@rot@Matrix.Diagonal((0.26,0.006,0.05,1)),verts=t.verts); p._add(t,'sole',0.0)
    export(p,'prop_sign_board_aperto')
def rug():
    p=P(); p.box((-0.9,-0.6,0),(0.9,0.6,0.02),'terracotta',0.01); p.box((-0.8,-0.5,0.005),(0.8,0.5,0.024),'sole_d',0.006); p.box((-0.7,-0.4,0.01),(0.7,0.4,0.027),'intonaco_terra',0.006)
    export(p,'prop_rug_bancone')
def parcel_stack():   # pila di pacchi unica mesh per set dressing (pivot base)
    p=P(); rr=random.Random(11)
    lay=[[(-0.21,-0.16,0.4,0.3,0.3),(0.21,-0.16,0.4,0.3,0.3),(-0.21,0.16,0.4,0.3,0.3),(0.21,0.16,0.4,0.3,0.3)],[(-0.18,0,0.36,0.6,0.28),(0.22,0,0.4,0.5,0.3)],[(0,0.02,0.5,0.4,0.16)]]
    z=0
    for row in lay:
        hmax=0
        for (x,y,w,d,h) in row:
            p.box((x-w/2,y-d/2,z),(x+w/2,y+d/2,z+h),rr.choice(['cartone','cartone','cartone_d']),0.025)
            p.box((x-w/2-0.002,y-0.02,z),(x+w/2+0.002,y+0.02,z+h+0.003),'crema',0.0); hmax=max(hmax,h)
        z+=hmax
    export(p,'prop_parcel_stack_a')
# ---------- ARCO 4 m (pivot inizio, x 0..4, apertura 2,4 m, imposta 1,5 m) e PLINTO bordo 2 m  (premium v1.1)
def arch():
    p=P(); L=4.0; W=2.4; sp=1.5; Rr=W/2; cx=L/2
    x0,x1=cx-W/2,cx+W/2
    for a,b in ((0.01,x0),(x1,L-0.01)):
        p.box((a,-0.17,0),(b,0.17,3.2),'tufo_b',0.02)
        for k,(z0,z1) in enumerate(((0.32,0.95),(0.95,1.55),(1.55,2.15),(2.15,2.82))):
            p.box((a+0.02,-0.21,z0+0.02),(b-0.02,-0.17,z1-0.02),['tufo','tufo_b'][k%2],0.03,2)
        p.box((a,-0.24,0),(b,-0.17,0.32),'terracotta_d',0.025,2); p.box((a,0.17,0),(b,0.24,0.3),'tufo_d',0.02)
        p.box((a if a<1 else a,-0.23,sp-0.08),(b,0.23,sp),'tufo_c',0.015)            # imposta
    # timpano sopra l'arco: blocco con foro cilindrico (boolean esatto)
    t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0); bmesh.ops.transform(t,matrix=Matrix.Translation((cx,0,(sp+2.98)/2))@Matrix.Diagonal((W,0.34,2.98-sp,1)),verts=t.verts)
    me=bpy.data.meshes.new('sp'); t.to_mesh(me); t.free(); o=bpy.data.objects.new('sp',me); bpy.context.scene.collection.objects.link(o)
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=Rr,depth=2,location=(cx,0,sp),rotation=(math.pi/2,0,0)); cu=bpy.context.object
    bo=o.modifiers.new('b','BOOLEAN'); bo.object=cu; bo.operation='DIFFERENCE'; bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier='b')
    t=bmesh.new(); t.from_mesh(o.data); p._add(t,'tufo_b'); bpy.data.objects.remove(cu); bpy.data.objects.remove(o)
    n=9   # conci dell'arco, smussati
    for i in range(n):
        a=math.pi*(i+0.5)/n; w=math.pi*Rr/n*0.92
        t=bmesh.new(); bmesh.ops.create_cube(t,size=1.0)
        bmesh.ops.transform(t,matrix=Matrix.Translation((cx+math.cos(a)*(Rr+0.17),0,sp+math.sin(a)*(Rr+0.17)))@Matrix.Rotation(-a+math.pi/2,4,'Y')@Matrix.Diagonal((w,0.5,0.34,1)),verts=t.verts)
        p._add(t,'tufo' if i!=n//2 else 'sole_d',0.025,1)
    p.box((0,-0.24,2.98),(L,0.24,3.2),'terracotta',0.03,2); p.box((0,-0.205,2.86),(L,0.205,2.98),'tufo_c',0.02)
    p.box((x0,-0.2,-0.0),(x1,0.2,0.04),'tufo_c',0.01)                                   # soglia
    export(p,'env_wall_arch_4m')
def plinth():
    p=P(); rr=random.Random(4)
    xs=[0.01,0.7+rr.uniform(-.1,.1),1.35+rr.uniform(-.1,.1),2.05]
    for a,b in zip(xs,xs[1:]): p.box((a+0.01,-0.2,-0.39),(b-0.01,0.2,-0.07),rr.choice(['tufo_b','tufo_c']),0.03,2)
    p.box((0.0,-0.21,-0.08),(2.05,0.21,-0.01),'terracotta',0.025,2)
    export(p,'env_plinth_edge_2m')
if '--only-arch' in sys.argv:
    arch(); plinth(); open(GLB+'/tri_report_premium_v1_1.txt','w').writelines(REP); print(''.join(REP)); raise SystemExit
def sign_faro():   # insegna «Vela & Figli» col faro (pivot centro, piatta sul muro, lato leggibile -y)
    p=P()
    p.box((-1.2,-0.05,-0.36),(1.2,0.05,0.36),'bruno_m',0.03,2)
    p.box((-1.15,-0.07,-0.31),(1.15,-0.04,0.31),'legno',0.015)
    p.box((-1.2,-0.08,-0.36),(1.2,-0.05,-0.31),'terracotta',0.01); p.box((-1.2,-0.08,0.31),(1.2,-0.05,0.36),'terracotta',0.01)
    # faro: torre a strisce, lanterna emissiva, tetto
    for k in range(4):
        r0=0.13-0.02*k; r1=0.13-0.02*(k+1)
        p.cyl(-0.88,-0.08,-0.28+k*0.1,-0.18+k*0.1,r0,['crema','terracotta'][k%2],8,0.0,r2=r1)
    p.cyl(-0.88,-0.08,0.12,0.2,0.07,'EMI:glow',8,0.0)
    p.cyl(-0.88,-0.08,0.2,0.29,0.09,'terracotta',8,0.0,r2=0.01)
    for a_ in (-0.35,0.35):
        p.box((-0.88+(-0.08 if a_<0 else 0.08),-0.1,0.15),(-0.88+(-0.3 if a_<0 else 0.3),-0.095,0.17),'EMI:glow',0.0)
    # testo
    bpy.ops.object.text_add(location=(0,0,0)); t=bpy.context.object; t.data.body='Vela & Figli'; t.data.extrude=0.01; t.data.size=0.34; t.data.resolution_u=1
    t.data.align_x='CENTER'; t.data.align_y='CENTER'; t.rotation_euler=(math.pi/2,0,0); t.location=(0.15,-0.09,-0.02)
    bpy.ops.object.convert(target='MESH'); bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    tb=bmesh.new(); tb.from_mesh(t.data); bmesh.ops.triangulate(tb,faces=tb.faces[:]); bpy.data.objects.remove(t)
    p._add(tb,'crema')
    export(p,'prop_sign_vela_figli')
def parapet():   # sostituisce i muretti in mattoni: parapetto basso in tufo con corrimano in legno (x 0..2.05, h 0.6)
    p=P(); rr=random.Random(4)
    xs=[0.01,0.7+rr.uniform(-.1,.1),1.35+rr.uniform(-.1,.1),2.05]
    for a,b in zip(xs,xs[1:]):
        p.box((a+0.01,-0.18,0.0),(b-0.01,0.18,0.24),rr.choice(['tufo_b','tufo']),0.025,1)
        p.box((a+0.01,-0.17,0.24),(b-0.01,0.17,0.46),rr.choice(['tufo','tufo_b','crema']),0.025,1)
    p.box((0.0,-0.21,0.46),(2.05,0.21,0.52),'tufo_c',0.02)
    for x in (0.08,1.02,1.97): p.box((x-0.03,-0.03,0.52),(x+0.03,0.03,0.82),'bruno_m',0.01)
    p.box((0.0,-0.045,0.8),(2.05,0.045,0.86),'bruno_m',0.015,2)
    export(p,'env_plinth_edge_2m')
if '--v03' in sys.argv:
    wall('env_wall_2m_a',2,'a',1); wall('env_wall_2m_b',2,'b',2); corner(); arch(); parapet()
    floor_small('env_floor_2x2_a','a'); floor_small('env_floor_2x2_b','b')
    counter(); sign_faro(); shelf_loaded()
    parcel('prop_parcel_box',0.4,0.3,0.3,True,'crema','cartone',1)
    parcel('prop_parcel_box_flat',0.5,0.4,0.16,True,'crema','cartone_d',2,0.008)
    parcel('prop_parcel_box_cube',0.32,0.32,0.32,False,'sole','cartone',3)
    parcel('prop_parcel_box_long',0.62,0.26,0.24,True,'crema','legno_l',4,0.008)
    plant_pot('prop_plant_pot_round','round',1); plant_pot('prop_plant_pot_tall','tall',2); plant_pot('prop_planter_box','box',3)
    streetlamp(); sign_hanging(); sign_board(); rug(); parcel_stack()
    open(GLB+'/tri_report_v03.txt','w').writelines(REP); print(''.join(REP)); raise SystemExit
# ---------- RUN
wall('env_wall_2m_a',2,'a',1); wall('env_wall_2m_b',2,'b',2); corner()
floor('env_floor_2x2_a','a'); floor('env_floor_2x2_b','b')
counter(); shelf_loaded()
parcel('prop_parcel_box',0.4,0.3,0.3,True,'crema','cartone',1)
parcel('prop_parcel_box_flat',0.5,0.4,0.16,True,'crema','cartone_d',2,0.008)
parcel('prop_parcel_box_cube',0.32,0.32,0.32,False,'sole','cartone',3)
parcel('prop_parcel_box_long',0.62,0.26,0.24,True,'crema','legno_l',4,0.008)
plant_pot('prop_plant_pot_round','round',1); plant_pot('prop_plant_pot_tall','tall',2); plant_pot('prop_planter_box','box',3)
streetlamp(); sign_hanging(); sign_board(); rug(); parcel_stack()
open(GLB+'/tri_report_premium_v1.txt','w').writelines(REP); print(''.join(REP))
