# Insegna «Vela & Figli» v04 (#70): legno noce con cornice terracotta, scritta crema in rilievo (Fredoka Bold, OFL, già nel repo),
# faro (emblema Faro Spento) in rilievo con lanterna emissiva. Profondità reale: niente più decal piatta (DepotKit.DressSignFace non nasconde più la mesh).
# blender -b -P 3d/scripts/sign_vela_figli_v04.py   (riusa helper/atlas/export di kit_deposito_v03.py)
import pathlib
_S=open(str(pathlib.Path(__file__).resolve().parent/'kit_deposito_v03.py')).read()
exec(_S[:_S.index('# ---------- MURI')])
FONT=R+'/Assets/_Game/UI/Fonts/Fredoka-Bold.ttf'
def sign_faro_v04():
    p=P()
    p.box((-1.22,-0.06,-0.40),(1.22,0.05,0.40),'terracotta',0.03,2)                 # cornice terracotta (spessore 11 cm)
    p.box((-1.12,-0.10,-0.31),(1.12,0.0,0.31),'bruno_m',0.02,1)                      # tavola noce, sporge 4 cm dalla cornice
    for z in (-0.16,0.0,0.16): p.box((-1.10,-0.103,z-0.004),(1.10,-0.098,z+0.004),'bruno',0.0)   # fughe tra le doghe
    # faro in rilievo (sporge ~12 cm): base, torre rastremata a strisce crema/terracotta, ballatoio, lanterna emissiva, cupola
    fx,fy=-0.86,-0.12
    p.box((fx-0.15,fy-0.04,-0.30),(fx+0.15,fy+0.06,-0.24),'tufo_c',0.01)
    for k in range(4):
        r0=0.12-0.018*k; r1=0.12-0.018*(k+1)
        p.cyl(fx,fy,-0.24+k*0.085,-0.155+k*0.085,r0,['crema','terracotta'][k%2],10,0.0,r2=r1)
    p.cyl(fx,fy,0.10,0.125,0.085,'bruno',10,0.0)                                      # ballatoio
    p.cyl(fx,fy,0.125,0.205,0.058,'EMI:glow',10,0.0)                                  # lanterna
    p.cyl(fx,fy,0.205,0.29,0.075,'terracotta_d',10,0.0,r2=0.01)                       # cupola
    for s in (-1,1): p.box((fx+s*0.07,fy-0.03,0.15),(fx+s*0.28,fy-0.025,0.18),'EMI:glow',0.0)   # raggi corti
    bpy.ops.object.text_add(location=(0,0,0)); t=bpy.context.object; t.data.body='Vela & Figli'
    t.data.font=bpy.data.fonts.load(FONT); t.data.extrude=0.018; t.data.size=0.30; t.data.resolution_u=1
    t.data.align_x='CENTER'; t.data.align_y='CENTER'; t.rotation_euler=(math.pi/2,0,0); t.location=(0.17,-0.12,-0.01)
    bpy.ops.object.convert(target='MESH'); bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.dissolve_limited(angle_limit=0.05); bpy.ops.object.mode_set(mode='OBJECT')
    tb=bmesh.new(); tb.from_mesh(t.data); bmesh.ops.triangulate(tb,faces=tb.faces[:]); bpy.data.objects.remove(t)
    p._add(tb,'crema')
    export(p,'prop_sign_vela_figli')
sign_faro_v04(); print(''.join(REP))
