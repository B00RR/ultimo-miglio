// Nonna Ada: controller (Speed -> Idle/Walk, bool Talk) + prefab (scala 0.88 SOLO sul root) + verifica del verso del passo.
// Unity -batchmode -projectPath <copia> -buildTarget WebGL -executeMethod UltimoMiglio.Anim.EditorTools.AdaSetup.Run -quit
using System.Linq; using UnityEditor; using UnityEditor.Animations; using UnityEngine;
namespace UltimoMiglio.Anim.EditorTools {
public static class AdaSetup {
  const string Dir = "Assets/_Game/Art/Characters/", Fbx = Dir + "chr_nonna_ada.fbx", Ctl = Dir + "chr_nonna_ada.controller", Pf = Dir + "chr_nonna_ada.prefab";
  public const float PrefabScale = 0.88f;
  public static void Run() {
    AssetDatabase.ImportAsset(Fbx, ImportAssetOptions.ForceSynchronousImport);
    var clips = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
    var av = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
    if (!av || !av.isValid || !av.isHuman) throw new System.Exception("[AdaSetup] avatar Humanoid non valido");
    foreach (var n in new[] { "Idle", "Walk", "Talk" }) if (!clips.ContainsKey(n)) throw new System.Exception("[AdaSetup] clip mancante " + n);
    var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(Ctl) ?? AnimatorController.CreateAnimatorControllerAtPath(Ctl);
    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Ctl)) if (o != ac && !(o is AnimatorController)) AssetDatabase.RemoveObjectFromAsset(o);
    ac.layers = new AnimatorControllerLayer[0]; ac.parameters = new AnimatorControllerParameter[0];
    ac.AddParameter("Speed", AnimatorControllerParameterType.Float); ac.AddParameter("Talk", AnimatorControllerParameterType.Bool);
    ac.AddLayer("Base Layer"); var sm = ac.layers[0].stateMachine;
    var loco = ac.CreateBlendTreeInController("Locomotion", out var bt, 0); bt.blendParameter = "Speed"; bt.useAutomaticThresholds = false;
    bt.AddChild(clips["Idle"], 0f); bt.AddChild(clips["Walk"], 1.2f);
    var talk = sm.AddState("Talk"); talk.motion = clips["Talk"]; sm.defaultState = loco;
    var t = loco.AddTransition(talk); t.hasExitTime = false; t.duration = 0.2f; t.AddCondition(AnimatorConditionMode.If, 0, "Talk"); t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
    t = talk.AddTransition(loco); t.hasExitTime = false; t.duration = 0.2f; t.AddCondition(AnimatorConditionMode.IfNot, 0, "Talk");
    t = talk.AddTransition(loco); t.hasExitTime = false; t.duration = 0.15f; t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
    EditorUtility.SetDirty(ac); AssetDatabase.SaveAssets();

    var root = new GameObject("chr_nonna_ada"); root.transform.localScale = Vector3.one * PrefabScale; // scala nel prefab, non nel rig
    var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx), root.transform);
    model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; // fronte +Z come Nina
    var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/VFX/Materials/M_Char_Atlas.mat");
    if (mat) foreach (var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, _ => mat);
    var an = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
    an.avatar = av; an.runtimeAnimatorController = ac; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    PrefabUtility.SaveAsPrefabAsset(root, Pf); Object.DestroyImmediate(root);
    Debug.Log($"[AdaSetup] ok: clip {string.Join(",", clips.Keys)} | prefab {Pf} scala {PrefabScale}");
    Verify();
  }
  /// <summary>Simula il cammino su +Z: posa visiva (bacino) davanti e piede d'appoggio che scivola verso -Z.</summary>
  public static void Verify() {
    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Pf));
    var an = go.GetComponentInChildren<Animator>(); an.Rebind(); an.SetFloat("Speed", 1.2f);
    float dt = 1f / 60f, foot = 0; int n = 0; float vis = 0; Vector3? last = null; int lastF = -1;
    for (int i = 0; i < 120; i++) {
      go.transform.position += Vector3.forward * 1.2f * dt; an.Update(dt);
      var lf = an.GetBoneTransform(HumanBodyBones.LeftFoot); var rf = an.GetBoneTransform(HumanBodyBones.RightFoot);
      var lu = an.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var ru = an.GetBoneTransform(HumanBodyBones.RightUpperLeg);
      var f = Vector3.Cross(ru.position - lu.position, Vector3.up); f.y = 0; vis += Vector3.Dot(f.normalized, Vector3.forward);
      int k = lf.position.y <= rf.position.y ? 0 : 1; var lp = go.transform.InverseTransformPoint((k == 0 ? lf : rf).position);
      if (i > 20 && last.HasValue && lastF == k) { foot += lp.z - last.Value.z; n++; }
      last = lp; lastF = k;
    }
    vis /= 120f; float fs = n > 0 ? foot / n : 0;
    Object.DestroyImmediate(go);
    var ok = vis > 0.7f && fs < 0f;
    Debug.Log($"[AdaSetup] verifica Walk: bacino·avanti={vis:F2} piede d'appoggio dz/frame={fs:F4} -> {(ok ? "VIA" : "NO-VIA")}");
    if (!ok) throw new System.Exception("[AdaSetup] Ada cammina all'indietro");
  }
}}
