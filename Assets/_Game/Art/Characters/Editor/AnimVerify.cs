// Ultimo Miglio - Animator: verifica import personaggi (batchmode) + still isolato.
// Unity -batchmode -projectPath <wt> -executeMethod UltimoMiglio.Anim.EditorTools.AnimVerify.Run -quit
using System.IO; using System.Linq; using System.Text;
using UnityEditor; using UnityEditor.Animations; using UnityEngine;
namespace UltimoMiglio.Anim.EditorTools {
public static class AnimVerify {
  const string Dir = "Assets/_Game/Art/Characters/";
  static readonly (string name, bool human, string[] clips)[] Chars = {
    ("chr_nina_vela", true,  new[]{"Idle","Walk","Run","Carry_Idle","Carry_Walk","Deliver","Cheer"}),
    ("bot_nonno",     false, new[]{"Idle","Drive","Lift","Drop","Happy"}),
    ("bot_tico",      false, new[]{"Idle","Move","Stamp","Happy"}) };
  public static void Run() {
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    var sb = new StringBuilder(); bool ok = true;
    foreach (var c in Chars) {
      string fbx = Dir + c.name + ".fbx";
      var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
      if (imp == null) { sb.AppendLine($"FAIL {c.name}: FBX non importato"); ok = false; continue; }
      var objs = AssetDatabase.LoadAllAssetsAtPath(fbx);
      var clips = objs.OfType<AnimationClip>().Where(a => !a.name.StartsWith("__preview__")).ToArray();
      var avatar = objs.OfType<Avatar>().FirstOrDefault();
      sb.AppendLine($"{c.name}: guid={AssetDatabase.AssetPathToGUID(fbx)} animType={imp.animationType} avatar={(avatar? avatar.name:"none")} valid={(avatar? avatar.isValid:false)} human={(avatar? avatar.isHuman:false)}");
      if (c.human && (avatar == null || !avatar.isValid || !avatar.isHuman)) ok = false;
      if (c.human && avatar) foreach (var hb in avatar.humanDescription.human.OrderBy(h=>h.humanName)) sb.AppendLine($"   map {hb.humanName} -> {hb.boneName}");
      foreach (var n in c.clips) {
        var cl = clips.FirstOrDefault(x => x.name == n);
        if (cl == null) { sb.AppendLine($"   FAIL clip {n} mancante"); ok = false; continue; }
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(cl, out string g, out long id);
        sb.AppendLine($"   clip {n,-11} len={cl.length:0.000}s frames={Mathf.RoundToInt(cl.length*cl.frameRate)} loop={cl.isLooping} human={cl.humanMotion} fileID={id}");
      }
      var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(Dir + c.name + ".controller");
      if (ctrl == null) { sb.AppendLine("   FAIL controller non caricato"); ok = false; continue; }
      sb.AppendLine($"   controller params: {string.Join(",", ctrl.parameters.Select(p => p.name + ":" + p.type))}");
      foreach (var st in ctrl.layers[0].stateMachine.states) {
        var m = st.state.motion; string desc;
        if (m is BlendTree bt) desc = "BlendTree[" + string.Join(",", bt.children.Select(ch => (ch.motion ? ch.motion.name : "NULL") + "@" + ch.threshold)) + "]";
        else desc = m ? m.name : "NULL";
        if (desc.Contains("NULL")) ok = false;
        sb.AppendLine($"   state {st.state.name}: {desc}");
      }
      Still(c.name, fbx, ctrl, c.clips[1]); Still(c.name, fbx, ctrl, c.clips[2]);
    }
    sb.AppendLine(ok ? "RESULT OK" : "RESULT FAIL");
    Directory.CreateDirectory("Captures"); File.WriteAllText("Captures/anim_verify.txt", sb.ToString());
    Debug.Log("ANIM_VERIFY\n" + sb);
  }
  // equivalente batchmode della skill screenshot-isolated: layer dedicato, camera Front/3-4, sfondo pieno, luce direzionale (50,-30,0)
  static void Still(string name, string fbx, RuntimeAnimatorController ctrl, string clip) {
    if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
    var go = (GameObject)Object.Instantiate(prefab); const int L = 31;
    foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = L;
    var cl = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(a => a.name == clip);
    cl.SampleAnimation(go, cl.length * 0.25f);
    var b = new Bounds(go.transform.position, Vector3.zero);
    foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
    var lightGo = new GameObject("L"); var li = lightGo.AddComponent<Light>(); li.type = LightType.Directional; li.intensity = 1.2f;
    lightGo.transform.rotation = Quaternion.Euler(50, -30, 0); li.cullingMask = 1 << L;
    var camGo = new GameObject("C"); var cam = camGo.AddComponent<Camera>();
    cam.cullingMask = 1 << L; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.95f, 0.87f, 0.71f);
    cam.fieldOfView = 30; float dist = b.extents.magnitude * 1.2f / Mathf.Tan(15 * Mathf.Deg2Rad);
    var dir = new Vector3(0.45f, 0.35f, 1f).normalized;   // davanti (+Z Unity) e 3/4
    cam.transform.position = b.center + dir * dist; cam.transform.LookAt(b.center);
    var rt = new RenderTexture(768, 768, 24); cam.targetTexture = rt; cam.Render();
    RenderTexture.active = rt; var tex = new Texture2D(768, 768, TextureFormat.RGB24, false);
    tex.ReadPixels(new Rect(0, 0, 768, 768), 0, 0); tex.Apply(); RenderTexture.active = null;
    Directory.CreateDirectory("Captures"); File.WriteAllBytes($"Captures/{name}_{clip}_isolated.png", tex.EncodeToPNG());
    Object.DestroyImmediate(go); Object.DestroyImmediate(camGo); Object.DestroyImmediate(lightGo);
  }
}}
