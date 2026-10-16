# Render di review #2: lineup dei 6 clienti (iso 3/4 e fronte). blender -b -P render_review_2.py -- <dir_clienti> <out>
import bpy,sys,math,os
from mathutils import Vector
a=sys.argv[sys.argv.index("--")+1:]; CLI,OUT=a; os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True); sc=bpy.context.scene
sc.render.engine='CYCLES'; sc.cycles.samples=32; sc.render.film_transparent=False
w=bpy.data.worlds.new("w"); w.use_nodes=True; w.node_tree.nodes["Background"].inputs[0].default_value=(0.95,0.89,0.78,1); w.node_tree.nodes["Background"].inputs[1].default_value=0.45; sc.world=w
sc.view_settings.view_transform='Standard'
AT=bpy.data.images.load(os.environ['UM_ATLAS']+'.png')
def atl(o):
    m=bpy.data.materials.new('M_Diorama_Atlas'); m.use_nodes=True; t=m.node_tree.nodes.new('ShaderNodeTexImage'); t.image=AT; t.interpolation='Closest'
    b=m.node_tree.nodes['Principled BSDF']; m.node_tree.links.new(t.outputs[0],b.inputs[0]); b.inputs['Roughness'].default_value=0.9
    o.data.materials.clear(); o.data.materials.append(m)
def imp(p,x):
    bpy.ops.import_scene.fbx(filepath=p); o=[q for q in bpy.context.selected_objects if q.type=='MESH'][0]; atl(o); o.location.x+=x; return o
sun=bpy.data.objects.new("s",bpy.data.lights.new("s","SUN")); sun.data.energy=2.2; sun.data.color=(1,0.93,0.82); sun.rotation_euler=(math.radians(50),0,math.radians(-35)); sc.collection.objects.link(sun)
bpy.ops.mesh.primitive_plane_add(size=40); g=bpy.context.object; m=bpy.data.materials.new("g"); m.diffuse_color=(0.86,0.76,0.6,1); g.data.materials.append(m)
cam=bpy.data.objects.new("c",bpy.data.cameras.new("c")); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'
def shot(name,target,dist_dir,scale,res=(900,1200)):
    sc.render.resolution_x,sc.render.resolution_y=res
    cam.location=Vector(target)+Vector(dist_dir).normalized()*10; cam.data.ortho_scale=scale
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.join(OUT,name); bpy.ops.render.render(write_still=True)
names=["base","residente","rider","anziana","turista","vip"]
for i,n in enumerate(names): imp(os.path.join(CLI,"chr_cliente_%s.fbx"%n),i*0.9)
shot("lineup_iso34.png",(2.25,0,0.9),(0.6,-1,1.0),6.8,(1800,1000)); shot("lineup_fronte.png",(2.25,0,0.9),(0,-1,0.1),6.6,(1800,900))
