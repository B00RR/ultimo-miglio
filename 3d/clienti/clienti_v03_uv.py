# Clienti v03 (review Concept Artist PR #85): SOLO remap UV su T_Palette_Atlas, topologia e ordine vertici invariati rispetto ai v02 riggati (0c05198).
# a) rider: cappello limone #F7E26B (era terracotta come il residente)  b) 3 toni di pelle caldi (celle esistenti, nessuna cella nuova).
# blender -b -P clienti_v03_uv.py -- <dir_v02_blend> <out>   (env UM_ATLAS)
import bpy,sys,os,json
SRC,OUT=sys.argv[sys.argv.index("--")+1:]; os.makedirs(OUT,exist_ok=True)
J=json.load(open(os.environ["UM_ATLAS"]+".json")); C={e["name"]:e["uv"]["center"] for e in J["extras"]}
# toni: A = pelle/pelle_d (invariato), B = pelle_d/legno (ambrato), C = intonaco_terra/pelle (chiaro caldo)
TONE={"A":("pelle","pelle_d"),"B":("pelle_d","legno"),"C":("intonaco_terra","pelle")}
WHO={"base":"A","residente":"B","rider":"C","anziana":"A","turista":"C","vip":"B"}
def near(uv,n): c=C[n]; return abs(uv[0]-c[0])<0.02 and abs(uv[1]-c[1])<0.02
rep=[]
for n,t in WHO.items():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(SRC,"chr_cliente_%s.blend"%n))
    o=[q for q in bpy.data.objects if q.type=='MESH'][0]; me=o.data; uv=me.uv_layers.active.data
    nv0=len(me.vertices); co0=[tuple(v.co) for v in me.vertices]
    hat=set()
    if n=="rider":
        gi=o.vertex_groups["hat"].index; hat={v.index for v in me.vertices if any(g.group==gi and g.weight>0.5 for g in v.groups)}
    sk,dk=TONE[WHO[n]]; ch=0
    for p in me.polygons:
        ls=list(p.loop_indices); u=uv[ls[0]].uv
        tgt=None
        if hat and all(v in hat for v in p.vertices): tgt="limone"
        elif near(u,"pelle"): tgt=sk
        elif near(u,"pelle_d"): tgt=dk
        if tgt and (tgt not in ("pelle",) or sk!="pelle") and not near(u,tgt):
            for l in ls: uv[l].uv=C[tgt]
            ch+=1
    assert len(me.vertices)==nv0 and all(tuple(v.co)==c for v,c in zip(me.vertices,co0))
    for s in bpy.context.selected_objects: s.select_set(False)
    o.select_set(True); bpy.context.view_layer.objects.active=o
    f=os.path.join(OUT,"chr_cliente_%s"%n)
    bpy.ops.export_scene.fbx(filepath=f+".fbx",use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_space_transform=True,mesh_smooth_type='FACE',add_leaf_bones=False,path_mode='STRIP')
    bpy.ops.export_scene.gltf(filepath=f+".glb",export_format='GLB',use_selection=True)
    bpy.ops.wm.save_as_mainfile(filepath=f+".blend")
    rep.append((n,WHO[n],len(me.vertices),sum(len(p.vertices)-2 for p in me.polygons),ch))
print("UVREP",rep)
