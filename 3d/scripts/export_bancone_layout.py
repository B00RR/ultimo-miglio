# Estrae il layout della stanza Bancone (vela_figli_final_v04.blend, dump in /tmp/dump.json) e lo converte in coordinate Unity.
# Blender (destrorso, Z-up) -> Unity (sinistrorso, Y-up), export FBX -Z forward / Y up:
#   pos_u = (-x, z, -y) ; rotY_u = -rotZ_b ; scala invariata.
import json
import os,sys
OUT=sys.argv[1] if len(sys.argv)>1 else 'bancone_layout.json'
D=json.load(open(os.environ.get('UM_DUMP','/tmp/dump.json')))
KIT='Assets/_Game/Art/Environment/Kit/'
COL={'Cube':'tufo_c','Cube.001':'tufo_c','Cube.002':'tufo_d','Cube.003':'tufo_d','Cube.004':'sole'}
def conv(loc,rz): return [round(-loc[0],4),round(loc[2],4),round(-loc[1],4)], round(-rz,2)+0.0
items=[]
REPL={'prop_box_m':None,'prop_box_s':None,'prop_lemontree_pot':(0.45,0.5,0,0,0.9)}
for o in D:
    n,mesh=o['n'],o['d']
    if mesh.startswith('env_plinth_edge'): continue  # bordo di taglio camera: sostituito dai muri chiusi
    if mesh.startswith(('chr_','bot_')): continue      # personaggi solo-render: vedi spawn
    if mesh in REPL:
        if REPL[mesh] is None: continue
        x,y,z,rz,sc=REPL[mesh]; o=dict(o,loc=[x,y,z],rot=[0,0,rz],s=[sc]*3)
    p,ry=conv(o['loc'],o['rot'][2])
    if n.startswith('Cube'):
        role='zone_pad_bancone' if n=='Cube.004' else 'env_niche_'+{'Cube':'back','Cube.001':'floor','Cube.002':'side_l','Cube.003':'side_r'}[n]
        items.append(dict(name=role,fbx='',primitive='cube',swatch=COL[n],position=p,rotationEuler=[0,ry,0],scale=[o['dim'][0],o['dim'][2],o['dim'][1]],note='primitiva centrata (cubo 1 m scalato); colore swatch atlas'))
    else:
        items.append(dict(name=mesh,fbx=KIT+mesh+'.fbx',primitive='',swatch='',position=p,rotationEuler=[0,ry,0],scale=[o['s'][0],o['s'][2],o['s'][1]],note=''))
# muri di chiusura (nel beauty erano tagliati per la camera); lato interno del muro = -Y locale Blender
for (nm,x,y,rz) in [('env_wall_2m_a',2,-0.15,180),('env_wall_2m_b',4,-0.15,180),('env_wall_2m_b',4.15,2,-90),('env_wall_2m_a',4.15,4,-90),
                    ('env_wall_corner',-0.15,-0.15,0),('env_wall_corner',4.15,-0.15,0),('env_wall_corner',4.15,4.15,0)]:
    p,ry=conv((x,y,0),rz); items.append(dict(name=nm,fbx=KIT+nm+'.fbx',primitive='',swatch='',position=p,rotationEuler=[0,ry,0],scale=[1,1,1],note='muro di chiusura aggiunto (non presente nel beauty)'))

# ---- PREMIUM v1: set dressing denso (coordinate Blender x,y,z,rotZ,scala)
PR='Assets/_Game/Art/Props/'; TP='Assets/_Game/Art/Environment/ThirdParty/Props/'
DENSE=[('prop_shelf_2m_loaded',KIT,3.8,3.0,0,90,1),('prop_parcel_stack_a',KIT,1.45,3.6,0,10,1),
 ('prop_parcel_box',PR,0.55,2.3,0,15,1),('prop_parcel_box_flat',PR,0.55,2.3,0.3,40,1),('prop_parcel_box_long',PR,0.6,1.7,0,-80,1),
 ('prop_parcel_box',PR,2.75,3.7,0,-8,1),('prop_parcel_box_flat',PR,2.75,3.7,0.3,15,1),('prop_parcel_box_cube',PR,1.05,0.45,0,20,1),
 ('prop_parcel_box_long',PR,1.6,0.35,0,-5,1),('prop_parcel_box_flat',PR,2.75,1.8,1.03,5,0.8),
 ('prop_rug_bancone',KIT,2.0,0.75,0,0,1),('prop_plant_pot_tall',KIT,3.75,0.4,0,0,1),('prop_plant_pot_round',KIT,0.4,1.2,0,0,1),
 ('prop_planter_box',KIT,2.9,0.25,0,0,1),('prop_sign_hanging_parcel',KIT,0.03,0.4,2.6,90,1),('prop_sign_board_aperto',KIT,3.2,3.8,0,-20,1),
 ('prop_streetlamp',KIT,4.7,4.7,0,-135,1),('prop_tp_basket',TP,2.95,1.0,0,10,1),
 ('prop_tp_lemon',TP,1.65,1.62,1.03,0,1),('prop_tp_lemon',TP,1.73,1.7,1.03,40,1),('prop_tp_orange',TP,1.62,1.74,1.03,0,1)]
for (nm,fold,x,y,z,rz,sc) in DENSE:
    p,ry=conv((x,y,z),rz); items.append(dict(name=nm,fbx=fold+nm+'.fbx',primitive='',swatch='',position=p,rotationEuler=[0,ry,0],scale=[sc,sc,sc],note='premium v1 set dressing'))
spawn=[dict(name='spawn_nina',position=conv((3.55,1.5,0),35)[0],rotationEuler=[0,-35.0,0],note='nel beauty Nina era scalata 1.35 solo per il render: in gioco scala 1'),
       dict(name='spawn_tico_cameo',position=conv((1.0,0.8,0),60)[0],rotationEuler=[0,-60.0,0],note='TICO entra al cap.4: solo cameo opzionale')]
out=dict(room='bancone',source='3d/final/vela_figli_final_v04.blend (build_final.py)',units='metri',coords='Unity sinistrorso Y-up; pos_u=(-x_b, z_b, -y_b), rotY_u=-rotZ_b',
  bounds_unity=dict(x=[-4.43,0.43],y=[0,3.2],z=[-4.43,0.43]),walls_closed=True,items=items,spawns=spawn)
json.dump(out,open(''+OUT+'','w'),indent=1)
print(len(items))
