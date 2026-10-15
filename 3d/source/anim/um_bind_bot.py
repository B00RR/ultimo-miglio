# Ultimo Miglio - rig Generic + animazioni per i robot (NONNO, TICO) direttamente dai .blend v02 del 3D Artist.
# Ri-eseguibile: rilanciare dopo ogni re-export (es. cambi colore della testa di TICO): geometria e gruppi vengono riletti.
# Uso: blender -b 3d/export/bot/bot_nonno_v02.blend -P um_bind_bot.py -- nonno
#      blender -b 3d/export/bot/bot_tico_v02.blend  -P um_bind_bot.py -- tico
# Requisiti mesh: oggetto <nome>_mesh con vertex group = nomi ossa (pesi rigidi 1.0), pivot a terra al centro, fronte -Y.
import bpy, sys, os, math
from mathutils import Vector, Matrix
CHAR=sys.argv[sys.argv.index("--")+1]
ROOT=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),"..",".."))
S,C,TAU=math.sin,math.cos,2*math.pi
def ease(x): x=max(0,min(1,x)); return x*x*(3-2*x)
def win(t,a,b): return ease((t-a)/(b-a))
sc=bpy.context.scene
NAME={"nonno":"bot_nonno","tico":"bot_tico"}[CHAR]
mesh=bpy.data.objects[NAME+"_mesh"]
for o in list(bpy.data.objects):
    if o!=mesh: bpy.data.objects.remove(o)
for a in list(bpy.data.actions): bpy.data.actions.remove(a)
mesh.parent=None; mesh.matrix_world=Matrix(); mesh.name=NAME+"_LOD0"; mesh.modifiers.clear()
groups=[g.name for g in mesh.vertex_groups]
def gbox(n):
    gi=mesh.vertex_groups[n].index; P=[v.co for v in mesh.data.vertices if any(g.group==gi and g.weight>0.5 for g in v.groups)]
    return Vector([min(p[i] for p in P) for i in range(3)]),Vector([max(p[i] for p in P) for i in range(3)])
B={n:gbox(n) for n in groups}
def ctr(n): return (B[n][0]+B[n][1])/2
# ------------------------------------------------ scheletri (pivot calcolati dai bounding box dei gruppi)
FORK_MAX=0.40   # cima forche (0.36+0.40=0.76 m) sotto la linea dei baffi (0.78): il viso resta visibile
if CHAR=="nonno":
    wf,wr=ctr("wheels_front"),ctr("wheels_rear"); hd=B["head"]; ms=B["mustache"]; mt=B["mast"]; fk=B["forks"]; bd=B["body"]
    BONES=[("root",(0,0,0),(0,0.3,0),None),
      ("wheels_front",(0,wf.y,wf.z),(0.3,wf.y,wf.z),"root"),("wheels_rear",(0,wr.y,wr.z),(0.3,wr.y,wr.z),"root"),
      ("body",(0,ctr("body").y,0.3),(0,ctr("body").y,0.9),"root"),
      ("mast",(0,ctr("mast").y,mt[0].z),(0,ctr("mast").y,mt[1].z),"body"),
      ("forks",(0,mt[0].y,fk[0].z+0.02),(0,fk[0].y,fk[0].z+0.02),"mast"),
      ("head",(0,ctr("head").y,hd[0].z+0.1),(0,ctr("head").y,hd[1].z),"body"),
      ("mustache",(0,ms[1].y,ctr("mustache").z),(0,ms[0].y-0.08,ctr("mustache").z),"head")]
    RIG="bot_nonno_rig"
else:
    hd=B["head"]; bd=B["body"]; aL,aR,st=B["arm_L"],B["arm_R"],B["stamp"]
    BONES=[("root",(0,0,0),(0,0.25,0),None),
      ("leg_L",(ctr("leg_L").x,ctr("leg_L").y,B["leg_L"][1].z),(ctr("leg_L").x,ctr("leg_L").y,0.05),"root"),
      ("leg_R",(ctr("leg_R").x,ctr("leg_R").y,B["leg_R"][1].z),(ctr("leg_R").x,ctr("leg_R").y,0.05),"root"),
      ("body",(0,ctr("body").y,bd[0].z),(0,ctr("body").y,bd[1].z),"root"),
      ("head",(0,ctr("head").y,hd[0].z),(0,ctr("head").y,hd[1].z),"body"),
      ("arm_L",(ctr("arm_L").x,ctr("arm_L").y,aL[1].z-0.04),(ctr("arm_L").x,ctr("arm_L").y,aL[0].z),"body"),
      ("arm_R",(ctr("arm_R").x,ctr("arm_R").y,aR[1].z-0.04),(ctr("arm_R").x,ctr("arm_R").y,aR[0].z),"body"),
      ("stamp",(ctr("stamp").x,ctr("stamp").y,st[1].z),(ctr("stamp").x,ctr("stamp").y,st[0].z),"arm_L")]
    RIG="bot_tico_rig"
ad=bpy.data.armatures.new(RIG); arm=bpy.data.objects.new(RIG,ad); sc.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
for n,h,t,p in BONES:
    b=ad.edit_bones.new(n); b.head=Vector(h); b.tail=Vector(t); b.roll=0
    if p: b.parent=ad.edit_bones[p]
bpy.ops.object.mode_set(mode='OBJECT')
missing=[n for n,_,_,_ in BONES if n not in groups and n!="root"]; assert not missing, f"gruppi mancanti: {missing}"
mesh.parent=arm; md=mesh.modifiers.new("Armature",'ARMATURE'); md.object=arm
print("UM_BONES",[(n,tuple(round(x,3) for x in h)) for n,h,t,p in BONES])
# ------------------------------------------------ helper posa (rotazioni negli assi armature, delta dal rest)
def setrot(bn,x=0,y=0,z=0):
    pb=arm.pose.bones[bn]; R=ad.bones[bn].matrix_local.to_3x3()
    W=Matrix.Rotation(x,3,'X')@Matrix.Rotation(y,3,'Y')@Matrix.Rotation(z,3,'Z')
    pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(R.inverted()@W@R).to_quaternion()
def setloc(bn,v): arm.pose.bones[bn].location=ad.bones[bn].matrix_local.to_3x3().inverted()@Vector(v)
def setscl(bn,s): arm.pose.bones[bn].scale=s
CLIPS=[]
def make(name,n,fn,loop):
    act=bpy.data.actions.new(name); act.use_fake_user=True; arm.animation_data_create(); arm.animation_data.action=act
    for f in range(n+1):
        t=(f%n)/n if loop else f/n
        for pb in arm.pose.bones: pb.location=(0,0,0); pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
        fn(t)
        # v03: baffi ristretti e portati avanti dall'artista -> scala al 100% (rimosso il ripiego 0.82 della v02)
        MUST_W=float(os.environ.get("UM_MUST_W","1.0"))
        if CHAR=="nonno" and MUST_W!=1.0:
            sx=arm.pose.bones["mustache"].scale; arm.pose.bones["mustache"].scale=(sx[0]*MUST_W,sx[1],sx[2])
        for pb in arm.pose.bones:
            for k in ("location","rotation_quaternion","scale"): pb.keyframe_insert(k,frame=f)
    act.frame_range=(0,n); act.use_frame_range=True; act.use_cyclic=loop; CLIPS.append((name,n,loop))
# ================================================= NONNO: muletto anziano, burbero e affettuoso
FORK_MAX=0.40   # cima forche (0.36+0.40=0.76 m) sotto la linea dei baffi (0.78): il viso resta visibile
if CHAR=="nonno":
    def stache(t,amp=0.12,freq=1): setrot("mustache", y=0.35*amp*S(TAU*freq*t)); setloc("mustache",(0,0,0.15*amp*S(TAU*freq*2*t))); setscl("mustache",(1-0.4*amp*abs(S(TAU*freq*t)),1,1))
    def wheels(a): setrot("wheels_front",x=-a); setrot("wheels_rear",x=-a*1.2)   # ruote posteriori piu' piccole
    def idle(t):          # respiro del motore, sguardo intorno, pisolino e risveglio coi baffi che tremano
        p=t*TAU; setloc("body",(0,0,0.008*S(2*p))); setrot("body",y=0.01*S(p))
        nap=win(t,.45,.58)*(1-win(t,.74,.78))
        setrot("head",x=0.22*nap+0.03*S(p),z=0.35*S(p)*(1-nap)*(1-win(t,.4,.45)*0+0))
        setloc("head",(0,0,0.03*win(t,.74,.77)*(1-win(t,.77,.9))))
        stache(t,0.05+0.25*win(t,.74,.77)*(1-win(t,.8,.95)),2 if t>.7 else 1)
        setloc("forks",(0,0,0.005*S(p)))
    def drive(t):         # ruote che girano (1 giro ciclo ant.), molleggio, baffi al vento
        p=t*TAU; wheels(p); setloc("body",(0,0,0.015*abs(S(2*p)))); setrot("body",x=-0.03,y=0.02*S(p))
        setrot("head",x=-0.08,y=0.04*S(2*p)); setrot("mast",x=0.015*(1+S(2*p))); setloc("forks",(0,0,0.05))
        setrot("mustache",x=0.05+0.03*S(4*p)); setloc("mustache",(0,0,0.006*S(4*p))); setscl("mustache",(0.97+0.02*S(4*p),1,1))
    def lift(t):          # carica: si prepara, sforzo, forche su, sospiro soddisfatto
        u=win(t,.15,.75); shake=0.012*S(TAU*7*t)*win(t,.15,.25)*(1-win(t,.6,.75))
        setloc("forks",(0,0,FORK_MAX*u+shake)); setrot("mast",x=0.04*u); setrot("body",x=0.04*win(t,0,.15)*(1-u)-0.02*u)
        setloc("body",(0,0,-0.02*win(t,.1,.3)*(1-win(t,.6,.8))))
        setrot("head",x=-0.15*win(t,.1,.3)*(1-win(t,.75,.9))-0.06*u,z=0.1*win(t,.85,1)); setloc("head",(0,0.03*u,0))
        setrot("mustache",y=0.06*win(t,.8,.9)*S(TAU*3*t),x=-0.06*win(t,.15,.3)*(1-win(t,.7,.8))); setloc("mustache",(0,0,0.01*win(t,.8,.9)*S(TAU*3*t)))
    def drop(t):
        u=1-win(t,.1,.7); setloc("forks",(0,0,FORK_MAX*u)); setrot("mast",x=0.04*u+0.03*win(t,.7,.8)*(1-win(t,.8,1)))
        setrot("head",x=-0.06*u+0.12*win(t,.75,.9)*(1-win(t,.9,1)),z=-0.15*win(t,.6,.85)); setloc("head",(0,0.03*u,0)); stache(t,0.1*win(t,.7,.8))
    def happy(t):         # saltella sulle sospensioni, testa che dondola, baffi che ballano
        p=t*TAU; setloc("body",(0,0,0.05*abs(S(2*p)))); setrot("body",y=0.05*S(2*p))
        setrot("head",y=0.25*S(2*p),z=0.12*S(p)); setloc("head",(0,0,0.02*abs(S(4*p))))
        setrot("mustache",y=0.08*S(4*p)); setloc("mustache",(0,0,0.012*S(4*p))); setscl("mustache",(0.95+0.05*S(2*p),1,1)); setloc("forks",(0,0,0.12*abs(S(2*p)))); wheels(0.3*S(2*p))
    for c in (("Idle",60,idle,1),("Drive",20,drive,1),("Lift",24,lift,0),("Drop",20,drop,0),("Happy",30,happy,1)): make(c[0],c[1],c[2],bool(c[3]))
# ================================================= TICO: assistente del Bancone, pignolo e zelante
else:
    def idle(t):          # dondola sui cingoli, occhio curioso, si sistema gli occhiali, tamburella col timbro
        p=t*TAU; setrot("body",y=0.03*S(p)); setloc("body",(0,0,0.006*S(2*p)))
        setrot("head",z=0.3*S(p)*(1-win(t,.55,.6)),y=0.1*win(t,.3,.4)*(1-win(t,.5,.55)))
        g=win(t,.55,.65)*(1-win(t,.78,.88))                 # mano destra agli occhiali
        setrot("arm_R",x=-1.6*g,y=0.35*g,z=0.2*g); setrot("head",x=-0.12*g)
        tap=max(0,S(TAU*4*t))*0.15*(1-g)
        setrot("arm_L",x=-0.25-tap,y=-0.05); setrot("stamp",x=0.1*S(TAU*4*t))
    def move(t):          # cammina sui cingoli: leggero ondeggio, si sporge in avanti, braccia a pendolo
        p=t*TAU; setloc("leg_L",(0,0,0.02*max(0,S(p)))); setloc("leg_R",(0,0,0.02*max(0,-S(p))))
        setrot("body",x=0.1,y=0.04*S(p)); setloc("body",(0,0,0.015*abs(S(p))))
        setrot("head",x=-0.1,z=0.05*S(p)); setrot("arm_L",x=-0.3-0.3*S(p)); setrot("arm_R",x=0.3*S(p))
    def stamp(t):         # carica il timbro in alto, TUNK!, rimbalzo e annuisce soddisfatto
        up=win(t,.05,.4); hit=win(t,.42,.5); rec=win(t,.55,.9)
        a=-2.4*up*(1-hit)-0.9*hit*(1-rec)-0.25*rec
        setrot("arm_L",x=a); setrot("stamp",x=0.5*up*(1-hit))
        squash=hit*(1-win(t,.5,.65)); setscl("body",(1+0.06*squash,1+0.06*squash,1-0.08*squash))
        setloc("body",(0,0,-0.04*squash)); setrot("body",x=0.15*up*(1-hit)+0.2*squash*(1-rec))
        setrot("head",x=-0.25*up*(1-hit)+0.25*win(t,.6,.7)*(1-win(t,.8,.95)))
        setrot("arm_R",x=-0.3*up*(1-hit),y=0.5*squash)
    def happy(t):         # saltello, braccia al cielo, testa che gira, timbro agitato
        p=t*TAU; j=abs(S(2*p)); setloc("body",(0,0,0.08*j)); setloc("head",(0,0,0.02*j))
        setrot("head",z=0.35*S(2*p),y=0.1*S(4*p)); setrot("body",y=0.06*S(2*p))
        setrot("arm_L",x=-0.3,y=-2.3+0.25*S(4*p)); setrot("arm_R",x=-0.3,y=2.3+0.25*S(4*p)); setrot("stamp",x=0.4*S(4*p))
        setloc("leg_L",(0,0,0.03*j)); setloc("leg_R",(0,0,0.03*j))
    for c in (("Idle",60,idle,1),("Move",20,move,1),("Stamp",24,stamp,0),("Happy",30,happy,1)): make(c[0],c[1],c[2],bool(c[3]))
arm.animation_data.action=bpy.data.actions["Idle"]
sc.render.fps=30; sc.render.fps_base=1.0; sc.frame_start=0; sc.frame_end=59
os.makedirs(f"{ROOT}/source/bot",exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=f"{ROOT}/source/bot/{NAME}_final_rig.blend")
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=f"{ROOT}/export/bot/{NAME}.fbx", object_types={'ARMATURE','MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', use_armature_deform_only=False,
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE')
print("UM_DONE",NAME,"tris",sum(len(p.vertices)-2 for p in mesh.data.polygons),"clips",CLIPS)
