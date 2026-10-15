using System.IO; using UnityEngine.Playables; using UnityEngine.Animations; using System.Linq; using UnityEditor; using UnityEngine;
namespace UltimoMiglio.Anim.EditorTools {
// Verifica direzione passo: Nina trasla lungo +Z mentre suona Walk/Run/Carry_Walk; camera laterale e frontale.
// Uso: Unity -batchmode -buildTarget WebGL -executeMethod UltimoMiglio.Anim.EditorTools.AnimWalkVideo.Run (serve GPU/xvfb, non -nographics)
public static class AnimWalkVideo {
  const string FBX = "Assets/_Game/Art/Characters/chr_nina_vela.fbx";
  public static void Run() {
    ShaderUtil.allowAsyncCompilation = false;   // niente shader segnaposto nei frame
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
    var clips = AssetDatabase.LoadAllAssetsAtPath(FBX).OfType<AnimationClip>().ToDictionary(c => c.name);
    var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/Textures/T_Palette_Atlas.png");
    var sh = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
    Debug.Log("WALKVID shader " + sh.name + " pipeline " + UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
    var mat = new Material(sh); if (atlas) { mat.mainTexture = atlas; if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", atlas); }
    var go = (GameObject)Object.Instantiate(prefab);
    foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => mat).ToArray();
    var gm = new Material(sh); gm.color = new Color(0.85f, 0.72f, 0.52f); var gm2 = new Material(sh); gm2.color = new Color(0.35f, 0.25f, 0.2f);
    var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); floor.transform.localScale = new Vector3(3, 1, 10); floor.transform.position = new Vector3(0, 0, 40); floor.GetComponent<Renderer>().sharedMaterial = gm;
    for (int i = -4; i < 90; i++) { var m = GameObject.CreatePrimitive(PrimitiveType.Cube); m.transform.position = new Vector3(-0.6f, 0.01f, i * 0.5f); m.transform.localScale = new Vector3(0.3f, 0.02f, 0.06f); m.GetComponent<Renderer>().sharedMaterial = gm2; }
    var animr = go.GetComponent<Animator>() ?? go.AddComponent<Animator>(); animr.applyRootMotion = false;
    var graph = PlayableGraph.Create("walkvid"); graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    var outp = AnimationPlayableOutput.Create(graph, "o", animr);
    bool useAM = true; AnimationMode.StartAnimationMode();
    var pls = new System.Collections.Generic.Dictionary<AnimationClip, AnimationClipPlayable>(); graph.Play();
    System.Action<AnimationClip, float> sample = (c, t) => { if (!pls.TryGetValue(c, out var pl)) { pl = AnimationClipPlayable.Create(graph, c); pl.SetApplyFootIK(false); pls[c] = pl; }
      outp.SetSourcePlayable(pl); pl.SetTime(t); pl.SetTime(t); graph.Evaluate(0f);
      if (useAM) { AnimationMode.BeginSampling(); AnimationMode.SampleAnimationClip(go, c, t); AnimationMode.EndSampling(); } };
    Debug.Log("WALKVID animator avatar=" + animr.avatar + " human=" + (animr.avatar ? animr.avatar.isHuman : false) + " clips=" + string.Join(",", clips.Keys));
    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.65f, 0.6f, 0.55f);
    // In batchmode senza player loop lo SkinnedMeshRenderer non ri-skinna prima di Camera.Render():
    // le ossa si muovono ma la mesh resta ferma. Per il video: proxy statico aggiornato con BakeMesh a ogni frame.
    var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(); var proxies = new System.Collections.Generic.List<(SkinnedMeshRenderer, Mesh, Transform)>();
    foreach (var smr in smrs) { var pm = new Mesh(); var px = new GameObject("proxy_" + smr.name); px.AddComponent<MeshFilter>().sharedMesh = pm;
      px.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials; smr.enabled = false; proxies.Add((smr, pm, px.transform)); }
    System.Action bake = () => { foreach (var (smr, pm, px) in proxies) { smr.BakeMesh(pm, true); px.SetPositionAndRotation(smr.transform.position, smr.transform.rotation); } };
    var lg = new GameObject("L"); var li = lg.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.3f; lg.transform.rotation = Quaternion.Euler(50, -30, 0);
    var cam = new GameObject("C").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.95f, 0.87f, 0.71f); cam.fieldOfView = 30;
    var rt = new RenderTexture(640, 640, 24); cam.targetTexture = rt; var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
    string outDir = "Captures/walkvid"; if (Directory.Exists(outDir)) Directory.Delete(outDir, true); Directory.CreateDirectory(outDir);
    var seq = new (string clip, float speed)[] { ("Walk", 1.2f), ("Run", 2.8f), ("Carry_Walk", 1.15f) };
    int idx = 0; var log = new System.Text.StringBuilder();
    foreach (var view in new[] { "side", "front" })
      foreach (var (clipName, speed) in seq) {
        var cl = clips[clipName]; float z = 0;
        sample(cl, 0); go.transform.position = Vector3.zero; float lift = 0; { var bb = new Bounds(); bool first = true;
          foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (first) { bb = r.bounds; first = false; } else bb.Encapsulate(r.bounds); } lift = -bb.min.y; }
        for (int f = 0; f < 75; f++) {
          float t = f / 30f; sample(cl, t % cl.length);
          z = speed * t; go.transform.position = new Vector3(0, lift, z); go.transform.rotation = Quaternion.identity;
          if (view == "side") { var hp = animr.GetBoneTransform(HumanBodyBones.Hips).position; var l = animr.GetBoneTransform(HumanBodyBones.LeftFoot).position - hp; var r2 = animr.GetBoneTransform(HumanBodyBones.RightFoot).position - hp;
            log.AppendLine($"  {clipName} f{f} L=({l.z:F2},{l.y:F2}) R=({r2.z:F2},{r2.y:F2})"); }
          var c = new Vector3(0, 0.85f, z);
          if (view == "side") { cam.transform.position = c + new Vector3(5.5f, 0.6f, 0); }
          else { cam.transform.position = c + new Vector3(0.8f, 0.8f, 5.5f); }
          bake(); cam.transform.LookAt(c); cam.Render();
          RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); tex.Apply(); RenderTexture.active = null;
          File.WriteAllBytes($"{outDir}/{idx:D4}_{view}_{clipName}.png", tex.EncodeToPNG()); idx++;
        }
        var anim = go.GetComponent<Animator>(); var lf = anim ? anim.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
        log.AppendLine($"{view} {clipName} speed={speed} facing={go.transform.forward}");
      }
    graph.Destroy(); AnimationMode.StopAnimationMode(); File.WriteAllText($"{outDir}/log.txt", log.ToString()); Debug.Log("WALKVID frames " + idx); EditorApplication.Exit(0);
  }
}}
