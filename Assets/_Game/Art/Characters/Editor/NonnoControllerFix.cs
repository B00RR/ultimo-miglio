// #59: controller NONNO a 2 layer. Prima Lift (bool tenuto per tutto il trasporto) e Happy sostituivano la Locomotion:
// NONNO scivolava con le ruote ferme. Ora: Base = Idle/Drive (+Happy interrompibile dal moto), layer "Forche" (override,
// maschera mast+forks) = Lift tenuto finché Lift==true, poi Drop. Le ruote girano sempre in Drive.
// Unity -batchmode -projectPath <copia> -buildTarget WebGL -executeMethod UltimoMiglio.Anim.EditorTools.NonnoControllerFix.Run -quit
using System.Linq; using UnityEditor; using UnityEditor.Animations; using UnityEngine;
namespace UltimoMiglio.Anim.EditorTools {
public static class NonnoControllerFix {
  const string Fbx = "Assets/_Game/Art/Characters/bot_nonno.fbx", Ctl = "Assets/_Game/Art/Characters/bot_nonno.controller", MaskPath = "Assets/_Game/Art/Characters/bot_nonno_forche.mask";
  public static void Run() {
    var clips = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
    AnimationClip C(string n) { if (!clips.TryGetValue(n, out var c)) throw new System.Exception("clip mancante " + n); return c; }
    var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
    var mask = new AvatarMask();
    var paths = model.GetComponentsInChildren<Transform>(true).Select(t => AnimationUtility.CalculateTransformPath(t, model.transform)).ToArray();
    mask.transformCount = paths.Length;
    for (int i = 0; i < paths.Length; i++) { mask.SetTransformPath(i, paths[i]); var leaf = paths[i].Split('/').Last(); mask.SetTransformActive(i, leaf == "mast" || leaf == "forks"); }
    AssetDatabase.DeleteAsset(MaskPath); AssetDatabase.CreateAsset(mask, MaskPath);

    // riusa il GUID del controller esistente (referenziato da scena/prefab): svuota e ricostruisci
    var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(Ctl);
    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Ctl)) if (o != ac && !(o is AnimatorController)) AssetDatabase.RemoveObjectFromAsset(o);
    ac.layers = new AnimatorControllerLayer[0]; ac.parameters = new AnimatorControllerParameter[0];
    ac.AddParameter("Speed", AnimatorControllerParameterType.Float); ac.AddParameter("Lift", AnimatorControllerParameterType.Bool);
    ac.AddParameter("Drop", AnimatorControllerParameterType.Trigger); ac.AddParameter("Happy", AnimatorControllerParameterType.Trigger);

    ac.AddLayer("Base Layer"); var sm = ac.layers[0].stateMachine;
    var loco = ac.CreateBlendTreeInController("Locomotion", out var bt, 0); bt.blendParameter = "Speed"; bt.useAutomaticThresholds = false;
    bt.AddChild(C("Idle"), 0f); bt.AddChild(C("Drive"), 1f);
    var happy = sm.AddState("Happy"); happy.motion = C("Happy");
    var t = sm.AddAnyStateTransition(happy); t.AddCondition(AnimatorConditionMode.If, 0, "Happy"); t.canTransitionToSelf = false; t.duration = 0.1f;
    t = happy.AddTransition(loco); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.15f;
    t = happy.AddTransition(loco); t.hasExitTime = false; t.duration = 0.15f; t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed"); // mai Happy mentre guida
    sm.defaultState = loco;

    var forche = new AnimatorControllerLayer { name = "Forche", defaultWeight = 1f, blendingMode = AnimatorLayerBlendingMode.Override, avatarMask = mask, stateMachine = new AnimatorStateMachine { name = "Forche" } };
    AssetDatabase.AddObjectToAsset(forche.stateMachine, ac); forche.stateMachine.hideFlags = HideFlags.HideInHierarchy;
    ac.AddLayer(forche); var fs = ac.layers[1].stateMachine;
    var down = fs.AddState("Giu"); // vuoto: passa il Base Layer (forche a riposo)
    var lift = fs.AddState("Lift"); lift.motion = C("Lift");
    var drop = fs.AddState("Drop"); drop.motion = C("Drop");
    fs.defaultState = down;
    t = down.AddTransition(lift); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.If, 0, "Lift");
    t = lift.AddTransition(drop); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.IfNot, 0, "Lift");
    t = drop.AddTransition(down); t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f;
    t = drop.AddTransition(lift); t.hasExitTime = false; t.duration = 0.1f; t.AddCondition(AnimatorConditionMode.If, 0, "Lift");
    EditorUtility.SetDirty(ac); AssetDatabase.SaveAssets();
    Debug.Log($"[NonnoControllerFix] ok: maschera {paths.Count(p => p.EndsWith("mast") || p.EndsWith("forks"))} ossa, layer {ac.layers.Length}");
  }
}}
