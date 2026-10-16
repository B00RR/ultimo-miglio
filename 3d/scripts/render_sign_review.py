import bpy,math,os
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='/tmp/um_glb_v03/prop_sign_vela_figli.glb')
o=[x for x in bpy.data.objects if x.type=='MESH'][0]
for m in o.data.materials:
    if 'Emissive' in m.name and m.use_nodes:
        b=m.node_tree.nodes.get('Principled BSDF')
        if b: b.inputs['Emission Color'].default_value=(1,.8,.4,1); b.inputs['Emission Strength'].default_value=3
    for n in (m.node_tree.nodes if m.use_nodes else []):
        if n.type=='TEX_IMAGE': n.interpolation='Closest'
sc=bpy.context.scene; sc.render.engine='CYCLES'; sc.cycles.samples=32; sc.cycles.device='CPU'; sc.render.resolution_x=1280; sc.render.resolution_y=720
w=bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(.55,.6,.65,1)
sc.view_settings.view_transform='Standard'
c=o.matrix_world@ (sum((Vector(b) for b in o.bound_box),Vector())/8)
L=bpy.data.objects.new('L',bpy.data.lights.new('L','SUN')); L.data.energy=3; L.rotation_euler=(math.radians(50),0,math.radians(-30)); sc.collection.objects.link(L)
cam=bpy.data.objects.new('C',bpy.data.cameras.new('C')); sc.collection.objects.link(cam); sc.camera=cam
out=os.environ['OUT']
for name,d in [('insegna_iso34',(1.6,-2.4,1.2)),('insegna_fronte',(0,-3.2,0)),('insegna_lato',(-3.0,-0.6,0.2))]:
    # glTF: Y-up -> Blender Z-up; front faces -Y in Blender
    cam.location=c+Vector(d); cam.rotation_euler=(c-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'{out}/{name}.png'; bpy.ops.render.render(write_still=True)
