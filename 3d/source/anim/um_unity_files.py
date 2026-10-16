# Genera .fbx.meta (GUID fissi, clip con internalID fissi) + Animator Controller YAML per Nina e NONNO.
# Uso: python3 um_unity_files.py [nomi...]   (default: tutti; copia anche gli FBX da 3d/export/ nella cartella Unity)
# GUID e fileID sono deterministici (md5 del nome): rigenerare non cambia i riferimenti.
import sys
import os, shutil, hashlib
ROOT="/workspace/ultimo-miglio"; DST=os.environ.get("UM_DST",f"{ROOT}/wt/animator/Assets/_Game/Art/Characters"); os.makedirs(DST,exist_ok=True)
def guid(s): return hashlib.md5(("ultimo-miglio/"+s).encode()).hexdigest()
def cid(fbx,clip): return int(hashlib.md5(f"{fbx}/{clip}".encode()).hexdigest()[:15],16)  # int64 positivo stabile
CH={"chr_nina_vela":dict(src="3d/export/chr",rig="chr_nina_vela_rig",humanoid=True,
      clips=[("Idle",60,1),("Walk",24,1),("Run",18,1),("Carry_Idle",60,1),("Carry_Walk",24,1),("Deliver",16,0),("Cheer",40,0)]),
    "bot_nonno":dict(src="3d/export/bot",rig="bot_nonno_rig",humanoid=False,
      clips=[("Idle",60,1),("Drive",20,1),("Lift",24,0),("Drop",20,0),("Happy",30,1)]),
    "bot_tico":dict(src="3d/export/bot",rig="bot_tico_rig",humanoid=False,
      clips=[("Idle",60,1),("Move",20,1),("Stamp",24,0),("Happy",30,1)])}
def meta(name,c):
    g=guid(name+".fbx"); L=[f"fileFormatVersion: 2\nguid: {g}\nModelImporter:\n  serializedVersion: 22200\n  internalIDToNameTable:"]
    for n,_,_ in c["clips"]: L.append(f"  - first:\n      74: {cid(name,n)}\n    second: {n}")
    L.append("  externalObjects: {}\n  materials:\n    materialImportMode: 2\n  animations:\n    legacyGenerateAnimations: 4\n    bakeSimulation: 0\n    resampleCurves: 1\n    optimizeGameObjects: 0\n    importAnimatedCustomProperties: 0\n    animationCompression: 1\n    animationRotationError: 0.5\n    animationPositionError: 0.5\n    animationScaleError: 0.5\n    animationWrapMode: 0\n    clipAnimations:")
    for n,N,lp in c["clips"]:
        L.append(f"""    - serializedVersion: 16
      name: {n}
      takeName: {c['rig']}|{n}
      internalID: {cid(name,n)}
      firstFrame: 0
      lastFrame: {N}
      wrapMode: 0
      orientationOffsetY: 0
      level: 0
      cycleOffset: 0
      loop: 0
      hasAdditiveReferencePose: 0
      loopTime: {lp}
      loopBlend: {lp}
      loopBlendOrientation: 1
      loopBlendPositionY: 1
      loopBlendPositionXZ: 1
      keepOriginalOrientation: 1
      keepOriginalPositionY: 1
      keepOriginalPositionXZ: 1
      heightFromFeet: 0
      mirror: 0
      bodyMask: 01000000010000000100000001000000010000000100000001000000010000000100000001000000010000000100000001000000
      curves: []
      events: []
      transformMask: []
      maskType: 3
      maskSource: {{instanceID: 0}}
      additiveReferencePoseFrame: 0""")
    L.append(f"""    isReadable: 0
  meshes:
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useFileUnits: 1
    useFileScale: 1
    importBlendShapes: 1
  importAnimation: 1
  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    legTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName: 
    hasTranslationDoF: 0
    hasExtraRoot: 1
    skeletonHasParents: 1
  lastHumanDescriptionAvatarSource: {{instanceID: 0}}
  autoGenerateAvatarMappingIfUnspecified: 1
  animationType: {3 if c['humanoid'] else 2}
  humanoidOversampling: 1
  avatarSetup: 1
  addHumanoidExtraRootOnlyWhenUsingAvatar: 1
  importBlendShapeDeformPercent: 1
  remapMaterialsIfPossible: 1
  additionalBone: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
    return g,"\n".join(L)
_id=[100100000]
def nid(): _id[0]+=11; return _id[0]
def motion(name,clip): return f"{{fileID: {cid(name,clip)}, guid: {guid(name+'.fbx')}, type: 3}}"
def controller(name,c):
    docs=[]; states=[]; trans=[]
    sm=nid(); layerless=None
    def blend(bname,param,children):
        i=nid(); ch="".join(f"""
  - serializedVersion: 2
    m_Motion: {m}
    m_Threshold: {th}
    m_Position: {{x: 0, y: 0}}
    m_TimeScale: 1
    m_CycleOffset: 0
    m_DirectBlendParameter: {param}
    m_Mirror: 0""" for m,th in children)
        docs.append(f"""--- !u!206 &{i}
BlendTree:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {bname}
  m_Childs:{ch}
  m_BlendParameter: {param}
  m_BlendParameterY: {param}
  m_MinThreshold: {children[0][1]}
  m_MaxThreshold: {children[-1][1]}
  m_UseAutomaticThresholds: 0
  m_NormalizedBlendValues: 0
  m_BlendType: 0""")
        return i
    def state(sname,mot_id_or_ref,loop_x,y,trs):
        i=nid(); mref=f"{{fileID: {mot_id_or_ref}}}" if isinstance(mot_id_or_ref,int) else mot_id_or_ref
        states.append((i,loop_x,y))
        docs.append(f"""--- !u!1102 &{i}
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {sname}
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions:{''.join(chr(10)+'  - {fileID: '+str(t)+'}' for t in trs) if trs else ' []'}
  m_StateMachineBehaviours: []
  m_Position: {{x: 50, y: 50, z: 0}}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {mref}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: """)
        return i
    def tr(dst,conds,exit_time=0,dur=0.15):
        i=nid(); cs="".join(f"""
  - m_ConditionMode: {m}
    m_ConditionEvent: {p}
    m_EventTreshold: {v}""" for m,p,v in conds)
        docs.append(f"""--- !u!1101 &{i}
AnimatorStateTransition:
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: 
  m_Conditions:{cs if conds else ' []'}
  m_DstStateMachine: {{fileID: 0}}
  m_DstState: {{fileID: {dst}}}
  m_Solo: 0
  m_Mute: 0
  m_IsExit: 0
  serializedVersion: 3
  m_TransitionDuration: {dur}
  m_TransitionOffset: 0
  m_ExitTime: {0.9 if exit_time else 0.75}
  m_HasExitTime: {exit_time}
  m_HasFixedDuration: 1
  m_InterruptionSource: 0
  m_OrderedInterruption: 1
  m_CanTransitionToSelf: 1""")
        return i
    # ConditionMode: 1=If(true) 2=IfNot 3=Greater 4=Less
    if name=="chr_nina_vela":
        params=[("Speed",1),("Carry",4),("Deliver",9),("Cheer",9)]
        # soglie in m/s: NinaController passa Speed=|velocita| (max 3.0 m/s, 2.7 con pila); 1.2/2.8 = velocita naturali dei cicli
        loco=blend("Locomotion","Speed",[(motion(name,"Idle"),0),(motion(name,"Walk"),1.2),(motion(name,"Run"),2.8)])
        carry=blend("CarryLocomotion","Speed",[(motion(name,"Carry_Idle"),0),(motion(name,"Carry_Walk"),1.15),(motion(name,"Carry_Walk"),2.7)])
        # ids pre-allocati per riferimenti incrociati
        ids={k:nid() for k in ("Locomotion","Carry","Deliver","Cheer")}
        t_lc=tr(ids["Carry"],[(1,"Carry",0)]); t_cl=tr(ids["Locomotion"],[(2,"Carry",0)])
        t_ld=tr(ids["Deliver"],[(1,"Deliver",0)],dur=0.08); t_cd=tr(ids["Deliver"],[(1,"Deliver",0)],dur=0.08)
        t_lch=tr(ids["Cheer"],[(1,"Cheer",0)]); t_cch=tr(ids["Cheer"],[(1,"Cheer",0)])
        t_dback=tr(ids["Locomotion"],[],exit_time=1); t_chback=tr(ids["Locomotion"],[],exit_time=1)
        spec=[("Locomotion",f"{{fileID: {loco}}}",[t_lc,t_ld,t_lch]),("Carry",f"{{fileID: {carry}}}",[t_cl,t_cd,t_cch]),
              ("Deliver",motion(name,"Deliver"),[t_dback]),("Cheer",motion(name,"Cheer"),[t_chback])]
    elif name=="bot_tico":
        params=[("Speed",1),("Stamp",9),("Happy",9)]
        loco=blend("IdleMove","Speed",[(motion(name,"Idle"),0),(motion(name,"Move"),1)])
        ids={k:nid() for k in ("Locomotion","Stamp","Happy")}
        t1=tr(ids["Stamp"],[(1,"Stamp",0)],dur=0.08); t2=tr(ids["Happy"],[(1,"Happy",0)])
        t3=tr(ids["Locomotion"],[],exit_time=1); t4=tr(ids["Locomotion"],[],exit_time=1)
        spec=[("Locomotion",f"{{fileID: {loco}}}",[t1,t2]),("Stamp",motion(name,"Stamp"),[t3]),("Happy",motion(name,"Happy"),[t4])]
    else:
        params=[("Speed",1),("Lift",4),("Drop",9),("Happy",9)]
        loco=blend("IdleDrive","Speed",[(motion(name,"Idle"),0),(motion(name,"Drive"),1)])
        ids={k:nid() for k in ("Locomotion","Lift","Drop","Happy")}
        t1=tr(ids["Lift"],[(1,"Lift",0)]); t2=tr(ids["Drop"],[(1,"Drop",0)]); t3=tr(ids["Happy"],[(1,"Happy",0)])
        t4=tr(ids["Locomotion"],[(2,"Lift",0)],exit_time=1); t5=tr(ids["Locomotion"],[],exit_time=1); t6=tr(ids["Locomotion"],[],exit_time=1)
        spec=[("Locomotion",f"{{fileID: {loco}}}",[t1,t2,t3]),("Lift",motion(name,"Lift"),[t4]),
              ("Drop",motion(name,"Drop"),[t5]),("Happy",motion(name,"Happy"),[t6])]
    for k,(sn,m,trs) in enumerate(spec):
        _id[0]=ids[sn]-11; state(sn,m,0,0,trs)
    pdoc="".join(f"""
  - m_Name: {p}
    m_Type: {t}
    m_DefaultFloat: 0
    m_DefaultInt: 0
    m_DefaultBool: 0
    m_Controller: {{fileID: 9100000}}""" for p,t in params)
    cs="".join(f"""
  - serializedVersion: 1
    m_State: {{fileID: {i}}}
    m_Position: {{x: {250+(k%2)*250}, y: {120+(k//2)*90}, z: 0}}""" for k,(i,_,_) in enumerate(states))
    head=f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!91 &9100000
AnimatorController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  serializedVersion: 5
  m_AnimatorParameters:{pdoc}
  m_AnimatorLayers:
  - serializedVersion: 5
    m_Name: Base Layer
    m_StateMachine: {{fileID: {sm}}}
    m_Mask: {{fileID: 0}}
    m_Motions: []
    m_Behaviours: []
    m_BlendingMode: 0
    m_SyncedLayerIndex: -1
    m_DefaultWeight: 0
    m_IKPass: 0
    m_SyncedLayerAffectsTiming: 0
    m_Controller: {{fileID: 9100000}}
--- !u!1107 &{sm}
AnimatorStateMachine:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Base Layer
  m_ChildStates:{cs}
  m_ChildStateMachines: []
  m_AnyStateTransitions: []
  m_EntryTransitions: []
  m_StateMachineTransitions: {{}}
  m_StateMachineBehaviours: []
  m_AnyStatePosition: {{x: 50, y: 20, z: 0}}
  m_EntryPosition: {{x: 50, y: 120, z: 0}}
  m_ExitPosition: {{x: 800, y: 120, z: 0}}
  m_ParentStateMachinePosition: {{x: 800, y: 20, z: 0}}
  m_DefaultState: {{fileID: {states[0][0]}}}
"""
    return head+"\n".join(docs)+"\n"
SEL=sys.argv[1:] or list(CH)
for name,c in [(n,CH[n]) for n in SEL]:
    shutil.copy(f"{ROOT}/{c['src']}/{name}.fbx", f"{DST}/{name}.fbx")
    g,m=meta(name,c); open(f"{DST}/{name}.fbx.meta","w").write(m)
    open(f"{DST}/{name}.controller","w").write(controller(name,c))
    open(f"{DST}/{name}.controller.meta","w").write(f"fileFormatVersion: 2\nguid: {guid(name+'.controller')}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 9100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    print(name,"fbx guid",g)
