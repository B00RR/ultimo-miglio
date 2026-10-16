import bpy,json,math
out=[]
for o in bpy.context.scene.objects:
    if o.type!='MESH' or o.hide_render: continue
    out.append(dict(n=o.name,d=o.data.name,loc=[round(v,4) for v in o.location],rot=[round(math.degrees(v),2) for v in o.rotation_euler],s=[round(v,4) for v in o.scale],dim=[round(v,3) for v in o.dimensions]))
json.dump(out,open('/tmp/dump.json','w'),indent=0); print('N',len(out))
