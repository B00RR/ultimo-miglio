// Ultimo Miglio - headless VFX verification: runs the 3 builders, builds a test diorama, renders review PNGs.
// xvfb-run Unity -batchmode -buildTarget WebGL -projectPath <worktree> -executeMethod UltimoMiglio.VFX.EditorTools.UM_VfxVerify.Run -quit
// (needs a GPU context: do NOT pass -nographics). Output dir: env UM_VFX_OUT or <project>/Captures/vfx
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimoMiglio.VFX.EditorTools
{
    public static class UM_VfxVerify
    {
        static string Out;
        public static void Run()
        {
            Out = System.Environment.GetEnvironmentVariable("UM_VFX_OUT");
            if (string.IsNullOrEmpty(Out)) Out = Path.GetFullPath("Captures/vfx");
            Directory.CreateDirectory(Out);
            Debug.Log("[UM VFX] graphics: " + SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName);

            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
            {   // Tech Lead's ProjectSetup creates/assigns URP_Mobile (other assembly: call by reflection)
                var t = System.Type.GetType("UltimoMiglio.EditorTools.ProjectSetup, UltimoMiglio.Editor");
                t?.GetMethod("EnsureUrp")?.Invoke(null, null);
            }
            Debug.Log("[UM VFX] pipeline: " + GraphicsSettings.defaultRenderPipeline);
            // Verification-only (worktree): palette atlas as 05_3D_PIPELINE wants it (no mip, Point). Real fix belongs to 3D/TL meta.
            var ati = AssetImporter.GetAtPath("Assets/_Game/Art/Textures/T_Palette_Atlas.png") as TextureImporter;
            if (ati && (ati.mipmapEnabled || ati.filterMode != FilterMode.Point)) { Debug.LogWarning("[UM VFX] atlas meta: mip/bilinear -> forcing no-mip Point in worktree"); ati.mipmapEnabled = false; ati.filterMode = FilterMode.Point; ati.SaveAndReimport(); }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/FX/fx_coin.fbx"))
                if (o is Mesh cm) { var uv = new System.Collections.Generic.List<Vector2>(); cm.GetUVs(0, uv);
                    Vector2 mn = Vector2.one * 9, mx = -mn; foreach (var u in uv) { mn = Vector2.Min(mn, u); mx = Vector2.Max(mx, u); }
                    Debug.Log($"[UM VFX] coin mesh {cm.name} tris={cm.triangles.Length / 3} bounds={cm.bounds.size} uv0={uv.Count} uvMin={mn} uvMax={mx}"); }
            UM_FxPrefabBuilder.BuildAll();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            UM_PostProcessBuilder.BuildAll();
            var sunGo = new GameObject("Sun"); var sun = sunGo.AddComponent<Light>(); sun.type = LightType.Directional;
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<Camera>();
            UM_LightingSetup.Setup();
            var rig = sun.GetComponent<UM_LightingRig>();

            cam.orthographic = true; cam.orthographicSize = 4.2f;
            cam.transform.position = new Vector3(-7, 9, -7); cam.transform.rotation = Quaternion.Euler(40, 45, 0);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            string M = "Assets/_Game/VFX/Materials/";
            var setMat = AssetDatabase.LoadAssetAtPath<Material>(M + "M_Diorama_Atlas.mat");
            var charMat = AssetDatabase.LoadAssetAtPath<Material>(M + "M_Char_Atlas.mat");
            var propMat = AssetDatabase.LoadAssetAtPath<Material>(M + "M_Props_Interactive.mat");
            var lampMat = AssetDatabase.LoadAssetAtPath<Material>(M + "M_Diorama_Atlas_Emissive.mat");

            // Test diorama: primitives use flat-tint copies (primitive UVs span the whole atlas)
            Material Flat(Material src, Color c) { var m = new Material(src); m.SetTexture("_BaseMap", Texture2D.whiteTexture); m.SetColor("_BaseColor", c); return m; }
            GameObject Box(string n, Vector3 p, Vector3 s, Material m) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n; g.transform.position = p; g.transform.localScale = s; g.GetComponent<Renderer>().sharedMaterial = m; return g; }
            var mPav = Flat(setMat, UM_Palette.Pavimento); var mTufo = Flat(setMat, UM_Palette.Tufo); var mTerra = Flat(setMat, UM_Palette.Terracotta);
            var mWood = Flat(setMat, UM_Palette.Legno); var mBox = Flat(propMat, UM_Palette.Cartone); var mVan = Flat(propMat, UM_Palette.Menta);
            var mTeal = Flat(setMat, UM_Palette.TealOmni);
            Box("env_floor", new Vector3(0, -0.05f, 0), new Vector3(14, 0.1f, 14), mPav);
            Box("env_wall_back", new Vector3(0, 1.5f, 4), new Vector3(10, 3, 0.4f), mTufo);
            Box("env_wall_left", new Vector3(-4, 1.5f, 0), new Vector3(0.4f, 3, 8), mTufo);
            Box("env_roof", new Vector3(0, 3.1f, 3.4f), new Vector3(10.4f, 0.25f, 1.6f), mTerra);
            Box("env_shelf", new Vector3(-2.5f, 0.8f, 3), new Vector3(2.5f, 1.6f, 0.6f), mWood);
            Box("env_tower_omni", new Vector3(14, 6, 22), new Vector3(4, 12, 4), mTeal);
            for (int i = 0; i < 4; i++) Box("prop_package_" + i, new Vector3(1.2f + (i % 2) * 0.55f, 0.25f + (i / 2) * 0.5f, 1.4f), Vector3.one * 0.5f, mBox);
            Box("veh_van_test", new Vector3(2.8f, 0.6f, -1.2f), new Vector3(1.4f, 1.2f, 2.4f), mVan);
            var lamp = Box("prop_lantern", new Vector3(0.3f, 2.3f, 3.7f), Vector3.one * 0.3f, lampMat);
            var lampLight = new GameObject("lantern_light").AddComponent<Light>(); lampLight.type = LightType.Point;
            lampLight.transform.position = new Vector3(0.3f, 2.0f, 3.2f); lampLight.color = UM_Palette.Lanterna; lampLight.range = 6; lampLight.intensity = 0; 

            GameObject chr = null;
            var nina = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Characters/chr_nina_vela.fbx");
            if (nina) { chr = (GameObject)Object.Instantiate(nina); chr.name = "chr_nina_vela"; chr.transform.position = new Vector3(0, 0, 0.5f); chr.transform.rotation = Quaternion.Euler(0, 200, 0);
                foreach (var r in chr.GetComponentsInChildren<Renderer>()) { var a = r.sharedMaterials; for (int k = 0; k < a.Length; k++) a[k] = charMat; r.sharedMaterials = a; } }
            else Debug.LogWarning("[UM VFX] chr_nina_vela.fbx not found");

            // contact AO under character + packages
            var blobPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/VFX/Prefabs/fx_contact_shadow.prefab");
            if (blobPf)
            {
                if (chr) { var b = (GameObject)PrefabUtility.InstantiatePrefab(blobPf); b.transform.position = chr.transform.position + Vector3.up * 0.012f; b.transform.localScale = Vector3.one * 0.7f; }
                foreach (var t in new[] { new Vector3(1.2f, 0, 1.4f), new Vector3(1.75f, 0, 1.4f) }) { var b = (GameObject)PrefabUtility.InstantiatePrefab(blobPf); b.transform.position = t + Vector3.up * 0.012f; b.transform.localScale = Vector3.one * 0.85f; }
            }
            var toon = new[] { setMat, charMat, propMat, lampMat, mPav, mTufo, mTerra, mWood, mBox, mVan, mTeal };
            UM_QualityTier.Apply(UMTier.Web, toon);
            foreach (var mm in toon) Debug.Log($"[UM VFX] {mm.name} outlinePass={mm.GetShaderPassEnabled("SRPDefaultUnlit")}");

            var vol = Object.FindFirstObjectByType<Volume>();
            var night = AssetDatabase.LoadAssetAtPath<VolumeProfile>(UM_PostProcessBuilder.Dir + "/PP_Diorama_Night.asset");
            var day = vol.sharedProfile;

            rig.preset = UM_LightingRig.Preset.Day; rig.Apply();
            float lDay = Shot(cam, "vfx_day.png");
            rig.preset = UM_LightingRig.Preset.Night; rig.Apply(); vol.sharedProfile = night; lampLight.intensity = 1.7f;
            float lNight = Shot(cam, "vfx_night.png");
            Debug.Log($"[UM VFX] LUMA day={lDay:F3} night={lNight:F3} ratio={lNight / lDay:P1}");
            File.WriteAllText(Path.Combine(Out, "luminance.txt"), $"day={lDay:F4}\nnight={lNight:F4}\nratio={lNight / lDay:F3}\n");

            // Toon close-up: character + packages (outline) vs set (no outline)
            rig.preset = UM_LightingRig.Preset.Day; rig.Apply(); vol.sharedProfile = day; lampLight.intensity = 0;
            cam.orthographicSize = 2.0f; cam.transform.position = new Vector3(-6.2f, 8.5f, -6.2f) + new Vector3(1, 0, 1.2f);
            Shot(cam, "vfx_toon_outline_vs_set.png");

            // Particles
            Vector3 fxPos = new Vector3(0.6f, 0.05f, -1.6f);
            cam.orthographicSize = 1.4f; cam.transform.position = fxPos + new Vector3(0, 0.6f, 0) - cam.transform.forward * 15f;
            foreach (var (name, t) in new[] { ("fx_package_pop", 0.12f), ("fx_coins", 0.3f), ("fx_upgrade", 0.6f), ("fx_dust", 0.3f) })
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/VFX/Prefabs/{name}.prefab");
                var g = (GameObject)PrefabUtility.InstantiatePrefab(pf); g.SetActive(true); g.transform.position = fxPos;
                var ps = g.GetComponent<ParticleSystem>();
                if (name == "fx_dust") { // simulate walking: move emitter across 1.5 m
                    ps.Play(true);
                    for (int s = 0; s < 15; s++) { g.transform.position = fxPos + new Vector3(-0.75f + s * 0.1f, 0, 0); ps.Simulate(0.02f, true, false, false); }
                } else ps.Simulate(t, true, true, false);
                Debug.Log($"[UM VFX] {name} alive={ps.particleCount}");
                Shot(cam, name.Replace("fx_", "vfx_fx_") + ".png");
                Object.DestroyImmediate(g);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/_Game/VFX/Scenes_VFX_Test.unity");
            Debug.Log("[UM VFX] verify done -> " + Out);
        }

        static float Shot(Camera cam, string file, int w = 720, int h = 1280)
        {
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false, false);
            RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(Out, file), tex.EncodeToPNG());
            double sum = 0; var px = tex.GetPixels32();
            foreach (var p in px) sum += (0.2126 * p.r + 0.7152 * p.g + 0.0722 * p.b) / 255.0;
            Object.DestroyImmediate(tex);
            return (float)(sum / px.Length);
        }
    }
}
