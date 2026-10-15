# Ultimo Miglio - rig + animazioni (Animator, Sprint 1). Blender 4.2 headless.
# Uso:  blender -b -P um_rig_anim.py -- nina|nonno [--mesh /path/final.fbx|.glb|.obj]
#  - senza --mesh: costruisce blockout low-poly (stand-in) e lo skinna rigidamente alle ossa
#  - con --mesh: importa la mesh finale del 3D Artist (in piedi, 1u=1m, fronte -Y Blender, piedi a z=0),
#    la lega al rig con pesi automatici (max 4 influenze) e ri-esporta: le animazioni sono sul rig, quindi si riapplicano.
# Output: 3d/source/<chr|bot>/<nome>_rig.blend + 3d/export/<chr|bot>/<nome>.fbx (take = 1 per action, 30 fps)
import bpy, sys, os, math
from mathutils import Matrix, Vector, Euler
argv = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else ["nina"]
CHAR = argv[0]; MESH = argv[argv.index("--mesh")+1] if "--mesh" in argv else None
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FPS = 30
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene; sc.render.fps = FPS; sc.unit_settings.scale_length = 1.0
def hexc(h): 
    c=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple([(x/12.92 if x<=0.04045 else ((x+0.055)/1.055)**2.4) for x in c]+[1.0])
S, C, TAU = math.sin, math.cos, 2*math.pi
# ---------------------------------------------------------------- skeleton
def build_arm(name, bones):
    ad = bpy.data.armatures.new(name+"_rig"); ad.display_type='OCTAHEDRAL'
    ob = bpy.data.objects.new(name+"_rig", ad); sc.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob; bpy.ops.object.mode_set(mode='EDIT')
    for n,h,t,p,deform in bones:
        b = ad.edit_bones.new(n); b.head=Vector(h); b.tail=Vector(t); b.roll=0; b.use_deform=deform
        if p: b.parent = ad.edit_bones[p]; b.use_connect=False
    bpy.ops.object.mode_set(mode='OBJECT'); return ob
# ---------------------------------------------------------------- blockout mesh (vertex colors, 1 material)
PARTS=[]
def part(prim, bone, col, loc, scale, rot=(0,0,0), **kw):
    fn={'cube':bpy.ops.mesh.primitive_cube_add,'cyl':bpy.ops.mesh.primitive_cylinder_add,
        'sph':bpy.ops.mesh.primitive_uv_sphere_add,'ico':bpy.ops.mesh.primitive_ico_sphere_add,'cone':bpy.ops.mesh.primitive_cone_add}[prim]
    fn(location=loc, rotation=rot, **kw); o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.shade_flat()
    ca=o.data.color_attributes.new("Col",'BYTE_COLOR','CORNER')
    for d in ca.data: d.color=hexc(col)
    vg=o.vertex_groups.new(name=bone); vg.add(range(len(o.data.vertices)),1.0,'REPLACE')
    PARTS.append(o); return o
def finish_mesh(name, arm):
    bpy.ops.object.select_all(action='DESELECT')
    for o in PARTS: o.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]; bpy.ops.object.join()
    m=bpy.context.object; m.name=name+"_LOD0"; m.data.name=name
    mat=bpy.data.materials.new("M_"+name); mat.use_nodes=True
    nt=mat.node_tree; bsdf=nt.nodes["Principled BSDF"]; vc=nt.nodes.new("ShaderNodeVertexColor"); vc.layer_name="Col"
    nt.links.new(vc.outputs[0], bsdf.inputs[0]); bsdf.inputs["Roughness"].default_value=0.9
    m.data.materials.clear(); m.data.materials.append(mat)
    bind(m, arm, auto=False); return m
def bind(m, arm, auto):
    if auto:
        bpy.ops.object.select_all(action='DESELECT'); m.select_set(True); arm.select_set(True)
        bpy.context.view_layer.objects.active=arm; bpy.ops.object.parent_set(type='ARMATURE_AUTO')
        bpy.context.view_layer.objects.active=m
        bpy.ops.object.vertex_group_limit_total(group_select_mode='ALL', limit=4)
        bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    else:
        m.parent=arm; md=m.modifiers.new("Armature",'ARMATURE'); md.object=arm
def import_final(path, name, arm):
    ext=os.path.splitext(path)[1].lower(); before=set(bpy.data.objects)
    {'.fbx':lambda: bpy.ops.import_scene.fbx(filepath=path), '.glb':lambda: bpy.ops.import_scene.gltf(filepath=path),
     '.gltf':lambda: bpy.ops.import_scene.gltf(filepath=path), '.obj':lambda: bpy.ops.wm.obj_import(filepath=path)}[ext]()
    new=[o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.type!='MESH': bpy.data.objects.remove(o); continue
        o.parent=None; o.modifiers.clear(); o.vertex_groups.clear(); bind(o, arm, auto=True)
    print("UM: final mesh bound:", [o.name for o in new if o.name in bpy.data.objects])
# ---------------------------------------------------------------- pose helpers (rotations in armature-rest axes, Z-up, fronte -Y)
ARM=None
def setrot(bn, x=0,y=0,z=0):
    pb=ARM.pose.bones[bn]; R=ARM.data.bones[bn].matrix_local.to_3x3()
    W=(Matrix.Rotation(x,3,'X')@Matrix.Rotation(y,3,'Y')@Matrix.Rotation(z,3,'Z'))
    pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(R.inverted()@W@R).to_quaternion()
def setloc(bn, v):
    R=ARM.data.bones[bn].matrix_local.to_3x3(); ARM.pose.bones[bn].location=R.inverted()@Vector(v)
def make_action(name, n, posefn, loop=True):
    act=bpy.data.actions.new(name); act.use_fake_user=True; ARM.animation_data_create(); ARM.animation_data.action=act
    for f in range(n+1):
        t=(f%n)/n if loop else f/n
        for pb in ARM.pose.bones: pb.location=(0,0,0); pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
        posefn(t)
        for pb in ARM.pose.bones:
            pb.keyframe_insert("location",frame=f); pb.keyframe_insert("rotation_quaternion",frame=f); pb.keyframe_insert("scale",frame=f)
    act.frame_range=(0,n); act.use_frame_range=True; act.use_cyclic=loop
    ARM.pose.bones  # keep
    CLIPS.append((name,0,n,loop)); return act
CLIPS=[]
def ease(x): x=max(0,min(1,x)); return x*x*(3-2*x)
def win(t,a,b): return ease((t-a)/(b-a))
# ================================================================ NINA
def nina():
    global ARM
    L=lambda s,x: (x if s=='Left' else -x)
    B=[("Root",(0,0,0),(0,0.25,0),None,False),("Hips",(0,0,0.92),(0,0,1.02),"Root",True),
       ("Spine",(0,0,1.02),(0,0,1.16),"Hips",True),("Chest",(0,0,1.16),(0,0,1.36),"Spine",True),
       ("Neck",(0,0,1.36),(0,0,1.42),"Chest",True),("Head",(0,0,1.42),(0,0,1.72),"Neck",True),
       ("Backpack",(0,0.17,1.12),(0,0.17,1.32),"Chest",True)]
    for s in ("Left","Right"):
        B+=[(s+"Shoulder",(L(s,.04),0,1.33),(L(s,.16),0,1.33),"Chest",True),
            (s+"Arm",(L(s,.16),0,1.33),(L(s,.40),0,1.33),s+"Shoulder",True),
            (s+"ForeArm",(L(s,.40),0,1.33),(L(s,.62),0,1.33),s+"Arm",True),
            (s+"Hand",(L(s,.62),0,1.33),(L(s,.74),0,1.33),s+"ForeArm",True),
            (s+"UpLeg",(L(s,.10),0,0.90),(L(s,.10),0,0.50),"Hips",True),
            (s+"Leg",(L(s,.10),0,0.50),(L(s,.10),0.02,0.10),s+"UpLeg",True),
            (s+"Foot",(L(s,.10),0.02,0.10),(L(s,.10),-0.12,0.03),s+"Leg",True),
            (s+"ToeBase",(L(s,.10),-0.12,0.03),(L(s,.10),-0.20,0.03),s+"Foot",True)]
    B.append(("Prop_R",(-0.78,0,1.33),(-0.78,0,1.45),"RightHand",True))
    ARM=build_arm("chr_nina_vela",B)
    if MESH: import_final(MESH,"chr_nina_vela",ARM)
    else:
        OR,TE,SK,HA,CR,DK,CB="E5743A","3E8C86","E9B48A","4A2E26","FFF4D6","3B2F2A","C99A62"
        part('cube','Hips',TE,(0,0,0.96),(0.17,0.11,0.09))
        part('cube','Spine',OR,(0,0,1.10),(0.16,0.11,0.08))
        part('cube','Chest',OR,(0,0,1.26),(0.19,0.12,0.12))
        part('cube','Chest',CR,(0,-0.121,1.27),(0.05,0.005,0.08))           # zip/badge crema
        part('cyl','Neck',SK,(0,0,1.40),(0.045,0.045,0.04),vertices=6)
        part('ico','Head',SK,(0,-0.01,1.56),(0.15,0.14,0.15),subdivisions=1)
        part('sph','Head',OR,(0,0.005,1.6),(0.165,0.16,0.12),segments=8,ring_count=5)   # casco
        part('cube','Head',CR,(0,-0.14,1.655),(0.12,0.03,0.015))           # visiera
        part('cube','Head',HA,(0,0.12,1.48),(0.07,0.04,0.09))              # coda capelli
        for x in (-0.055,0.055): part('cube','Head',DK,(x,-0.135,1.55),(0.018,0.01,0.03))   # occhi
        part('cube','Head',SK,(0,-0.15,1.51),(0.02,0.02,0.02))             # naso
        part('cube','Backpack',CB,(0,0.2,1.2),(0.14,0.07,0.14))            # zaino/porta-pila
        for s in ("Left","Right"):
            x=L(s,1)
            part('cube',s+"Shoulder",OR,(0.12*x,0,1.33),(0.06,0.07,0.06))
            part('cube',s+"Arm",OR,(0.28*x,0,1.33),(0.12,0.055,0.055))
            part('cube',s+"ForeArm",SK,(0.51*x,0,1.33),(0.11,0.045,0.045))
            part('ico',s+"Hand",SK,(0.69*x,0,1.33),(0.075,0.06,0.06),subdivisions=1)    # mano a muffola grande
            part('cube',s+"UpLeg",TE,(0.10*x,0,0.71),(0.075,0.08,0.2))
            part('cube',s+"Leg",TE,(0.10*x,0.01,0.31),(0.06,0.065,0.2))
            part('cube',s+"Foot",CR,(0.10*x,-0.04,0.05),(0.075,0.12,0.05))
            part('cube',s+"Foot",OR,(0.10*x,-0.04,0.105),(0.07,0.1,0.008))
            part('cube',s+"ToeBase",CR,(0.10*x,-0.18,0.04),(0.07,0.04,0.04))
        finish_mesh("chr_nina_vela",ARM)
    def arms_down(sw_l=0, sw_r=0, el_l=0, el_r=0, out=0.12):
        setrot("LeftArm", x=sw_l, y=1.35-out); setrot("RightArm", x=sw_r, y=-(1.35-out))
        setrot("LeftForeArm", z=-el_l); setrot("RightForeArm", z=el_r)
    def legs(ph, A, knee, lift=0):
        for s,o in (("Left",0),("Right",math.pi)):
            p=ph+o; setrot(s+"UpLeg", x=-A*S(p)-lift*max(0,S(p)))
            setrot(s+"Leg", x=knee*max(0,S(p-1.2))+0.08*knee)
            setrot(s+"Foot", x=-0.25*C(p))
    # --- idle 60: respiro, spostamento peso, sguardo curioso, piccolo saltello di caviglia
    def idle(t):
        p=t*TAU; setloc("Hips",(0.015*S(p),0,-0.008*(1+C(2*p))/2))
        setrot("Hips", y=0.03*S(p)); setrot("Chest", x=-0.04*S(2*p)-0.02, y=-0.02*S(p))
        setrot("Head", z=0.35*win(t,.3,.42)*(1-win(t,.62,.74)), x=0.05*S(2*p), y=0.08*win(t,.45,.55)*(1-win(t,.62,.74)))
        arms_down(0.05*S(p), -0.05*S(p), 0.25+0.05*S(2*p), 0.25+0.05*S(2*p))
        setrot("LeftUpLeg", y=-0.03*S(p)); setrot("RightUpLeg", y=-0.03*S(p))
    # --- walk 24: passo molleggiato, braccia ampie, testa che annuisce
    def walk(t):
        p=t*TAU; setloc("Hips",(0.02*S(p),0,0.035*abs(S(p))-0.03))
        setrot("Hips", z=0.12*S(p)); setrot("Spine", x=0.06, z=-0.1*S(p)); setrot("Chest", z=-0.08*S(p), y=0.03*S(p))
        setrot("Head", x=-0.06*C(2*p), z=0.05*S(p))
        legs(p,0.5,0.9); arms_down(0.55*S(p),-0.55*S(p),0.5,0.5)
    # --- run 18: inclinata in avanti, ginocchia alte, braccia a pistone
    def run(t):
        p=t*TAU; setloc("Hips",(0,0,0.06*abs(S(p))-0.07))
        setrot("Hips", z=0.15*S(p)); setrot("Spine", x=0.25); setrot("Chest", z=-0.15*S(p))
        setrot("Head", x=-0.18-0.05*C(2*p))
        legs(p,0.85,1.5,0.35); arms_down(0.95*S(p),-0.95*S(p),1.4,1.4,out=0.3)
    # --- carry_idle 60 / carry_walk 24 (upper body: braccia sotto la pila, schiena all'indietro)
    def carry_upper(p, bob):
        setrot("Spine", x=-0.1); setrot("Chest", x=-0.06+0.02*S(p))
        setrot("LeftArm", x=-1.25+bob, y=1.2, z=0.35); setrot("RightArm", x=-1.25+bob, y=-1.2, z=-0.35)
        setrot("LeftForeArm", z=-1.0); setrot("RightForeArm", z=1.0)
        setrot("LeftHand", y=0.6); setrot("RightHand", y=-0.6)
        setloc("Backpack",(0,0,0.015*S(p))); setrot("Backpack", x=0.03*S(p))
    def carry_idle(t):
        p=t*TAU; setloc("Hips",(0.01*S(p),0,-0.012*(1+C(2*p))/2)); carry_upper(2*p,0.04*S(2*p))
        setrot("Head", x=-0.12, z=0.15*S(p)); setrot("LeftUpLeg", y=-0.03); setrot("RightUpLeg", y=0.03)
    def carry_walk(t):
        p=t*TAU; setloc("Hips",(0.025*S(p),0,0.03*abs(S(p))-0.035))
        setrot("Hips", z=0.08*S(p), y=0.04*S(p)); legs(p,0.45,0.85); carry_upper(2*p,0.06*S(2*p))
        setrot("Head", x=-0.1, y=-0.06*S(p))
    # --- hand_over 16 (deliver): spinta in avanti delle braccia + inchino rapido e sorriso (testa)
    def hand_over(t):
        r=win(t,0,.45)*(1-win(t,.7,1)); setrot("Spine", x=0.25*r); setrot("Chest", x=0.1*r)
        setrot("Head", x=-0.25*r+0.2*win(t,.45,.6)*(1-win(t,.75,1)), y=0.15*r)
        setrot("LeftArm", x=0.15*r, y=1.23*(1-r), z=-1.4*r); setrot("RightArm", x=0.15*r, y=-1.23*(1-r), z=1.4*r)
        setrot("LeftForeArm", z=-0.25*(1-r)); setrot("RightForeArm", z=0.25*(1-r))
        setloc("Hips",(0,0,-0.04*r)); setrot("LeftUpLeg", x=-0.15*r); setrot("LeftLeg", x=0.3*r); setrot("RightLeg", x=0.15*r)
        setloc("Prop_R",(0,-0.05*r,0))
    # --- celebrate 40: carica, salto con pugni al cielo, atterraggio e doppio fist-pump
    def celebrate(t):
        crouch=win(t,0,.18)*(1-win(t,.18,.28)) + win(t,.55,.62)*(1-win(t,.62,.75))
        air=S(math.pi*min(1,max(0,(t-.25)/.32))) if .25<t<.57 else 0
        setloc("Hips",(0,0,-0.15*crouch+0.32*air))
        setrot("Spine", x=0.3*crouch-0.15*air); setrot("Head", x=-0.3*air+0.2*crouch, z=0.2*S(t*TAU*2)*win(t,.7,.8))
        for s,g in (("LeftUpLeg",1),("RightUpLeg",1)): setrot(s, x=-0.6*crouch+0.5*air*(1 if s[0]=='L' else 0.3))
        for s in ("LeftLeg","RightLeg"): setrot(s, x=1.2*crouch+0.9*air)
        for s in ("LeftFoot","RightFoot"): setrot(s, x=-0.5*crouch+0.6*air)
        up=win(t,.15,.3); pump=0.35*S(TAU*2*min(1,max(0,(t-.6)/.4)))*win(t,.6,.65)
        setrot("LeftArm", y=1.23*(1-up)-1.1*up+pump, z=-0.2*up); setrot("RightArm", y=-1.23*(1-up)+1.1*up-pump, z=0.2*up)
        setrot("LeftForeArm", z=-0.3*(1-up)-0.6*up); setrot("RightForeArm", z=0.3*(1-up)+0.6*up)
    for n,N,f,lp in (("Idle",60,idle,1),("Walk",24,walk,1),("Run",18,run,1),("Carry_Idle",60,carry_idle,1),
                     ("Carry_Walk",24,carry_walk,1),("Deliver",16,hand_over,0),("Cheer",40,celebrate,0)):
        make_action(n,N,f,bool(lp))
    return "chr_nina_vela","chr"
# ================================================================ NONNO (muletto robot anziano e affettuoso)
def nonno():
    global ARM
    B=[("Root",(0,0,0),(0,0.3,0),None,False),("Base",(0,0,0.18),(0,0,0.5),"Root",True),
       ("Mast",(0,-0.42,0.2),(0,-0.42,1.3),"Base",True),("Forks",(0,-0.5,0.12),(0,-0.9,0.12),"Mast",True),
       ("Neck",(0,0.05,0.75),(0,0.05,0.95),"Base",True),("Head",(0,0.05,0.95),(0,0.05,1.3),"Neck",True)]
    for n,x,y in (("Wheel_FL",.32,-.3),("Wheel_FR",-.32,-.3),("Wheel_BL",.32,.35),("Wheel_BR",-.32,.35)):
        B.append((n,(x,y,0.14),(x+(0.1 if x>0 else -0.1),y,0.14),"Base",True))
    ARM=build_arm("bot_nonno",B)
    if MESH: import_final(MESH,"bot_nonno",ARM)
    else:
        MN,TE,DK,CR,YE,OR,GR="8FD3B6","5F8F8A","3B2F2A","FFF4D6","F2C14E","E5743A","D9C4A0"
        part('cube','Base',MN,(0,0.05,0.42),(0.3,0.38,0.22)); part('cube','Base',TE,(0,0.05,0.2),(0.33,0.42,0.06))
        part('cube','Base',YE,(0,0.44,0.5),(0.26,0.03,0.08))   # contrappeso giallo
        for x in (-0.24,0.24): part('cube','Mast',TE,(x,-0.42,0.75),(0.03,0.03,0.55))
        part('cube','Mast',TE,(0,-0.42,1.28),(0.27,0.03,0.03))
        part('cube','Forks',GR,(0,-0.48,0.3),(0.22,0.025,0.2))
        for x in (-0.13,0.13): part('cube','Forks',DK,(x,-0.75,0.13),(0.035,0.28,0.015))
        part('cyl','Neck',TE,(0,0.05,0.85),(0.05,0.05,0.12),vertices=6)
        part('cube','Head',CR,(0,0.05,1.12),(0.2,0.15,0.15))
        part('cube','Head',DK,(0,-0.1,1.13),(0.16,0.01,0.1))                  # schermo viso
        for x in (-0.07,0.07): part('cube','Head',MN,(x,-0.115,1.16),(0.025,0.01,0.025))   # occhi
        part('cube','Head',CR,(0,-0.12,1.07),(0.1,0.02,0.02))                   # baffoni da nonno
        for x in (-0.09,0.09): part('cube','Head',CR,(x,-0.12,1.05),(0.025,0.02,0.03))
        part('cyl','Head',OR,(0,0.05,1.3),(0.18,0.14,0.03),vertices=8)          # coppola arancio
        part('cube','Head',OR,(0,-0.12,1.29),(0.15,0.06,0.012))
        for n,x,y in (("Wheel_FL",.32,-.3),("Wheel_FR",-.32,-.3),("Wheel_BL",.32,.35),("Wheel_BR",-.32,.35)):
            part('cyl',n,DK,(x,y,0.14),(0.14,0.14,0.06),rot=(0,math.pi/2,0),vertices=8)
            part('cube',n,YE,(x+(0.065 if x>0 else -0.065),y,0.14),(0.01,0.05,0.05))  # mozzo (mostra rotazione)
        finish_mesh("bot_nonno",ARM)
    def wheels(a):
        for n in ("Wheel_FL","Wheel_FR","Wheel_BL","Wheel_BR"): setrot(n, x=-a)
    def idle(t):   # respira col pistone, si guarda intorno, sonnecchia e si risveglia di colpo
        p=t*TAU; setloc("Base",(0,0,0.01*S(2*p)))
        nod=win(t,.45,.6)*(1-win(t,.72,.76))
        setrot("Neck", x=0.04*S(p)+0.35*nod); setrot("Head", z=0.4*S(p)*(1-nod), y=0.1*S(2*p)*(1-nod))
        setloc("Head",(0,0,0.04*win(t,.72,.76)*(1-win(t,.76,.9))))   # sobbalzo al risveglio
        setloc("Forks",(0,0,0.01*S(p)))
    def move(t):   # guida: ruote girano (2 giri/ciclo), molleggio, testa avanti fiera
        p=t*TAU; wheels(p); setloc("Base",(0,0,0.02*abs(S(2*p))))
        setrot("Base", x=-0.03, y=0.025*S(p)); setrot("Neck", x=0.15); setrot("Head", x=-0.1, y=0.06*S(2*p))
        setrot("Mast", x=0.04*S(2*p)); setloc("Forks",(0,0,0.06))
    def lift(t):   # carica: fatica, forche su, sospiro di soddisfazione
        u=win(t,.1,.75); setloc("Forks",(0,0,0.55*u+0.02*S(t*TAU*6)*(win(t,.1,.2)*(1-win(t,.6,.75)))))
        setrot("Mast", x=-0.08*u); setrot("Base", x=0.05*win(t,0,.15)*(1-u)-0.03*u)
        setrot("Neck", x=-0.25*win(t,.1,.3)*(1-win(t,.75,.9))); setrot("Head", x=0.2*win(t,.75,.9), z=0.15*win(t,.85,1))
        setloc("Base",(0,0,-0.03*win(t,.1,.3)*(1-win(t,.6,.8))))
    def drop(t):
        u=1-win(t,.1,.7); setloc("Forks",(0,0,0.55*u)); setrot("Mast", x=-0.08*u+0.06*win(t,.7,.8)*(1-win(t,.8,1)))
        setrot("Neck", x=0.2*win(t,.2,.6)*(1-win(t,.8,1))); setrot("Head", z=-0.2*win(t,.6,.8))
    def happy(t):  # contento: saltello sulle sospensioni, testa che dondola, coppola che salta
        p=t*TAU; setloc("Base",(0,0,0.06*abs(S(2*p)))); setrot("Base", y=0.06*S(2*p))
        setrot("Head", y=0.3*S(2*p), z=0.15*S(p)); setloc("Head",(0,0,0.03*abs(S(4*p))))
        setloc("Forks",(0,0,0.15*abs(S(2*p)))); wheels(0.4*S(2*p))
    for n,N,f,lp in (("Idle",60,idle,1),("Drive",20,move,1),("Lift",24,lift,0),("Drop",20,drop,0),("Happy",30,happy,1)):
        make_action(n,N,f,bool(lp))
    return "bot_nonno","bot"
name,cat = nina() if CHAR=="nina" else nonno()
# --- NLA: una strip per action (comodo per review), action attiva = idle
ad=ARM.animation_data
for a in bpy.data.actions:
    tr=ad.nla_tracks.new(); tr.name=a.name; tr.strips.new(a.name,0,a); tr.mute=True
ad.action=bpy.data.actions["Idle"]; sc.frame_start=0; sc.frame_end=59
os.makedirs(f"{ROOT}/source/{cat}",exist_ok=True); os.makedirs(f"{ROOT}/export/{cat}",exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=f"{ROOT}/source/{cat}/{name}_rig.blend")
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=f"{ROOT}/export/{cat}/{name}.fbx", use_selection=False, object_types={'ARMATURE','MESH'},
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', use_space_transform=True,
    bake_space_transform=False, use_armature_deform_only=True, add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X',
    bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE', use_mesh_modifiers=True)
tris=sum(len(p.vertices)-2 for o in sc.objects if o.type=='MESH' for p in o.data.polygons)
print("UM_RESULT", name, "tris", tris, "bones", len(ARM.data.bones), "clips", CLIPS)
