using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;
using UltimoMiglio.VFX;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Generates the chapter-1 vertical-slice scene from code (graybox with palette colours) so it is reproducible headless.
    /// Distances from data/ch1/zones.json: Arrivi↔scaffali 6 m, scaffali↔Bancone 9.4 m.
    /// Batchmode: -executeMethod UltimoMiglio.EditorTools.SliceSceneBuilder.Build</summary>
    public static class SliceSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Ch1_Deposito.unity";
        const string PrefabDir = "Assets/_Game/Prefabs";

        [MenuItem("Ultimo Miglio/Scene/Build Ch1 Vertical Slice")]
        public static void Build()
        {
            ChapterDataImporter.Import();
            TryRun("UltimoMiglio.VFX.EditorTools.UM_FxPrefabBuilder", "BuildAll");
            Directory.CreateDirectory(PrefabDir); Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var zones = AssetDatabase.LoadAssetAtPath<ChapterDataAsset>(ChapterDataImporter.AssetPath).Data.zones;
            float dArr = (float)zones.distancesM.arriviToShelves, dBan = (float)zones.distancesM.shelvesToBancone;

            var world = Mat("M_Diorama_Atlas", UM_Palette.Pavimento); var props = Mat("M_Props_Interactive", UM_Palette.Cartone); var chars = Mat("M_Char_Atlas", UM_Palette.AranceNina);

            // --- floor + walls
            var floor = Box("Floor", null, new Vector3(0, -0.05f, 0), new Vector3(18, 0.1f, 22), world, UM_Palette.Pavimento);
            floor.isStatic = true;
            Box("Wall_N", null, new Vector3(0, 1.2f, 11), new Vector3(18, 2.4f, 0.3f), world, UM_Palette.Tufo).isStatic = true;
            Box("Wall_W", null, new Vector3(-9, 1.2f, 0), new Vector3(0.3f, 2.4f, 22), world, UM_Palette.Tufo).isStatic = true;

            // --- GameRoot + audio + FX
            var root = new GameObject("GameRoot").AddComponent<GameRoot>();
            var audio = BuildAudio(root.transform);
            var fxGo = new GameObject("FxPlayer"); fxGo.transform.SetParent(root.transform);
            var fx = fxGo.AddComponent<UM_FxPlayer>();
            fx.packagePop = FindPrefab("Assets/_Game/VFX/Prefabs", "pop"); fx.coins = FindPrefab("Assets/_Game/VFX/Prefabs", "coin"); fx.upgrade = FindPrefab("Assets/_Game/VFX/Prefabs", "upgrade");

            // --- prefabs
            var parcel = SavePrefab(Box("Parcel", null, Vector3.zero, Vector3.one * 0.3f, props, UM_Palette.Cartone), "Parcel");
            var coinMesh = new[] { "Assets/_Game/Art/FX/fx_coin.fbx", "Assets/_Game/Art/FX/fx_coin_lira.fbx" }.Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault(g => g);
            var coinGo = coinMesh ? (GameObject)PrefabUtility.InstantiatePrefab(coinMesh) : Prim(PrimitiveType.Cylinder, "Coin", null, Vector3.zero, new Vector3(0.25f, 0.02f, 0.25f), props, UM_Palette.GialloSole);
            coinGo.name = "Coin"; foreach (var c in coinGo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var coin = SavePrefab(coinGo, "Coin");
            var custGo = Prim(PrimitiveType.Capsule, "Customer", null, Vector3.zero, new Vector3(0.5f, 0.8f, 0.5f), chars, UM_Palette.Menta);
            Object.DestroyImmediate(custGo.GetComponent<Collider>()); var cust = SavePrefab(custGo, "Customer").AddComponentPrefab<Customer>();

            // --- Nina
            var nina = new GameObject("Nina"); nina.transform.position = new Vector3(0, 0, 0);
            var cc = nina.AddComponent<CharacterController>(); cc.center = new Vector3(0, 0.8f, 0); cc.height = 1.6f; cc.radius = 0.3f;
            Prim(PrimitiveType.Capsule, "Body", nina.transform, new Vector3(0, 0.8f, 0), new Vector3(0.6f, 0.8f, 0.6f), chars, UM_Palette.AranceNina);
            Prim(PrimitiveType.Sphere, "Helmet", nina.transform, new Vector3(0, 1.65f, 0.02f), Vector3.one * 0.42f, chars, UM_Palette.Terracotta);
            var anchor = new GameObject("StackAnchor").transform; anchor.SetParent(nina.transform, false); anchor.localPosition = new Vector3(0, 0.9f, -0.42f);
            var stack = nina.AddComponent<PackageStack>(); stack.Anchor = anchor; stack.ParcelPrefab = parcel;
            var ctrl = nina.AddComponent<NinaController>(); ctrl.Stack = stack; root.Nina = ctrl;
            var joy = new GameObject("FloatingJoystick").AddComponent<UltimoMiglio.UI.FloatingJoystick>(); ctrl.Joystick = joy;

            // --- zones
            var arrPos = new Vector3(-5, 0, 6);
            var arrivals = Zone<ArrivalsZone>("Zona_Arrivi", arrPos, new Vector3(2.6f, 1, 2.6f), UM_Palette.GialloSole, props);
            var shelves = new Shelf[3]; var cols = new[] { UM_Palette.Terracotta, UM_Palette.Menta, UM_Palette.Limone };
            for (int i = 0; i < 3; i++)
            {
                var p = arrPos + new Vector3((i - 1) * 2.2f, 0, -dArr);
                var body = Box($"Scaffale_{(char)('A' + i)}", null, p + new Vector3(0, 0.9f, -0.6f), new Vector3(1.8f, 1.8f, 0.5f), world, UM_Palette.Legno);
                body.AddComponent<NavMeshObstacle>().carving = true;
                Box("Code", body.transform, new Vector3(0, 0.55f, 0.52f), new Vector3(0.9f, 0.08f, 0.1f), world, cols[i]);
                shelves[i] = Zone<Shelf>($"Zona_Scaffale_{(char)('A' + i)}", p + new Vector3(0, 0, 0.6f), new Vector3(1.8f, 1, 1.2f), cols[i], props);
                shelves[i].Color = (ParcelColor)i; shelves[i].Count = 4;
                var pick = Zone<ShelfPickup>($"Zona_Prendi_{(char)('A' + i)}", p + new Vector3(0, 0, 1.6f), new Vector3(1.8f, 1, 0.7f), UM_Palette.Crema, props); pick.Shelf = shelves[i];
            }
            var shelfC = arrPos + new Vector3(0, 0, -dArr);
            var banPos = shelfC + new Vector3(dBan * 0.8f, 0, -dBan * 0.6f); // 9.4 m
            Box("Bancone_Mesh", null, banPos + new Vector3(0, 0.5f, -0.9f), new Vector3(2.4f, 1f, 0.7f), world, UM_Palette.Terracotta).AddComponent<NavMeshObstacle>().carving = true;
            var bancone = Zone<Bancone>("Zona_Bancone", banPos, new Vector3(2.4f, 1, 1.2f), UM_Palette.Terracotta, props);
            var coins = new GameObject("Coins").AddComponent<CoinSpawner>(); coins.CoinPrefab = coin; bancone.Coins = coins;
            var queue = new GameObject("CustomerQueue").AddComponent<CustomerQueue>(); queue.Prefab = cust;
            queue.CounterSlot = new GameObject("CounterSlot").transform; queue.CounterSlot.position = banPos + new Vector3(0, 0, -1.8f);
            queue.Door = new GameObject("Door").transform; queue.Door.position = new Vector3(8.5f, 0, -10.5f); queue.LineStep = new Vector3(0.7f, 0, -0.7f);
            bancone.Queue = queue;
            var cassa = Zone<CassaZone>("Zona_Cassa", banPos + new Vector3(3.2f, 0, 1.5f), new Vector3(1.4f, 1, 1.4f), UM_Palette.TealOmni, props); cassa.Coins = coins;

            // --- pads (near the Cassa, 01 §3)
            var padRow = banPos + new Vector3(-1.5f, 0, 4.5f);
            Pad("Pad_AssumiNonno", UpgradeKind.NonnoHire, padRow, (float)AssetDatabase.LoadAssetAtPath<ChapterDataAsset>(ChapterDataImporter.AssetPath).Data.robots.nonno.unlockAtOnboardingSec, props);
            Pad("Pad_NonnoLivello", UpgradeKind.NonnoLevel, padRow, 0, props);
            Pad("Pad_Zaino", UpgradeKind.Zaino, padRow + new Vector3(1.8f, 0, 0), 0, props);
            Pad("Pad_Scarpe", UpgradeKind.Scarpe, padRow + new Vector3(3.6f, 0, 0), 0, props);
            Pad("Pad_Insegna", UpgradeKind.Insegna, padRow + new Vector3(1.8f, 0, 1.8f), 0, props);

            // --- NONNO
            var nonno = new GameObject("NONNO"); nonno.transform.position = arrPos + new Vector3(2.5f, 0, -2);
            Box("Body", nonno.transform, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.8f, 1f), chars, UM_Palette.TealOmni);
            Box("Forks", nonno.transform, new Vector3(0, 0.15f, 0.7f), new Vector3(0.6f, 0.06f, 0.6f), chars, UM_Palette.ScuroCaldo);
            var agent = nonno.AddComponent<NavMeshAgent>(); agent.radius = 0.45f;
            var bot = nonno.AddComponent<NonnoRobot>(); bot.Arrivals = arrivals; bot.Shelves = shelves; bot.Bancone = bancone; bot.Queue = queue;

            // --- NavMesh
            var surf = floor.AddComponent<NavMeshSurface>(); surf.collectObjects = CollectObjects.All; surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.BuildNavMesh();

            // --- camera + lighting + post
            var cam = new GameObject("Main Camera"); cam.tag = "MainCamera"; var camera = cam.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UM_Palette.Cielo;
            var urpCam = cam.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); urpCam.renderPostProcessing = true;
            cam.AddComponent<AudioListener>();
            var rig = new GameObject("CameraRig").AddComponent<CameraRigSetup>(); rig.Target = nina.transform; rig.Build(camera);
            TryRun("UltimoMiglio.VFX.EditorTools.UM_LightingSetup", "Setup");
            TryRun("UltimoMiglio.VFX.EditorTools.UM_PostProcessBuilder", "BuildAll");
            if (!Object.FindAnyObjectByType<Light>()) { var l = new GameObject("Sun").AddComponent<Light>(); l.type = LightType.Directional; l.transform.rotation = Quaternion.Euler(50, -35, 0); l.color = UM_Palette.Sole; }

            // --- blockout reference (05_3D_PIPELINE), hidden
            var glb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Blockout/depot_blockout_v01.glb");
            if (glb) { var b = (GameObject)PrefabUtility.InstantiatePrefab(glb); b.name = "REF_Blockout_v01"; b.SetActive(false); }

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Scene] built " + ScenePath);
        }

        static void Pad(string name, UpgradeKind k, Vector3 pos, float unlock, Material m)
        {
            var pad = Zone<UpgradePad>(name, pos, new Vector3(1.4f, 1, 1.4f), UM_Palette.Crema, m); pad.Kind = k; pad.UnlockAtActiveSec = unlock; pad.Priority = 1;
        }
        static T Zone<T>(string name, Vector3 pos, Vector3 size, Color c, Material m) where T : ProximityZone
        {
            var go = new GameObject(name); go.transform.position = pos;
            var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size; bc.center = new Vector3(0, 0.5f, 0);
            var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; // trigger events with CharacterController
            Box("Pad", go.transform, new Vector3(0, 0.01f, 0), new Vector3(size.x, 0.02f, size.z), m, c);
            Object.DestroyImmediate(go.transform.Find("Pad").GetComponent<Collider>());
            return go.AddComponent<T>();
        }
        static GameObject Box(string n, Transform parent, Vector3 pos, Vector3 scale, Material m, Color c) => Prim(PrimitiveType.Cube, n, parent, pos, scale, m, c);
        static GameObject Prim(PrimitiveType t, string n, Transform parent, Vector3 pos, Vector3 scale, Material m, Color c)
        {
            var go = GameObject.CreatePrimitive(t); go.name = n; if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Tinted(m, c); return go;
        }
        static Material Tinted(Material baseMat, Color c)
        {
            string path = $"Assets/_Game/Art/Materials/Graybox_{ColorUtility.ToHtmlStringRGB(c)}_{baseMat.name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (m) return m;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            m = new Material(baseMat); m.SetTexture("_BaseMap", Texture2D.whiteTexture); m.SetColor("_BaseColor", c); AssetDatabase.CreateAsset(m, path); return m;
        }
        static Material Mat(string vfxName, Color fallback)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Game/VFX/Materials/{vfxName}.mat");
            if (m) return m;
            Debug.LogWarning($"[Scene] {vfxName} missing, fallback URP Lit");
            var sh = Shader.Find("UltimoMiglio/ToonDiorama") ?? Shader.Find("Universal Render Pipeline/Lit");
            return new Material(sh) { name = vfxName, color = fallback };
        }
        static GameObject SavePrefab(GameObject go, string name)
        {
            var p = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab"); Object.DestroyImmediate(go); return p;
        }
        static GameObject FindPrefab(string dir, string contains)
        {
            if (!AssetDatabase.IsValidFolder(dir)) return null;
            return AssetDatabase.FindAssets("t:Prefab", new[] { dir }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileName(p).ToLowerInvariant().Contains(contains)).Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault();
        }
        static void TryRun(string type, string method)
        {
            var t = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(type)).FirstOrDefault(x => x != null);
            var mi = t?.GetMethod(method, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (mi == null) { Debug.LogWarning($"[Scene] {type}.{method} not found"); return; }
            try { mi.Invoke(null, null); } catch (System.Exception e) { Debug.LogWarning($"[Scene] {type}.{method}: {e.InnerException?.Message ?? e.Message}"); }
        }

        static AudioManager BuildAudio(Transform parent)
        {
            var am = new GameObject("AudioManager").AddComponent<AudioManager>(); am.transform.SetParent(parent);
            AudioClip C(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{(n.StartsWith("mus_") ? "Music" : "SFX")}/{n}.ogg");
            AudioClip[] R(string pfx, int n) => Enumerable.Range(1, n).Select(i => C($"{pfx}_{i:00}")).Where(c => c).ToArray();
            am.MusicStems = new[] { C("mus_day_vela_e_figli_base"), C("mus_day_vela_e_figli_perc"), C("mus_day_vela_e_figli_mel") };
            am.MusicFull = C("mus_day_vela_e_figli_full");
            am.Pickup = R("sfx_parcel_pickup", 8); am.Deposit = R("sfx_parcel_deposit", 4); am.CoinDrop = R("sfx_coin_drop", 4); am.CoinPickup = R("sfx_coin_pickup", 4);
            am.NonnoMumble = R("sfx_nonno_mumble", 3); am.Steps = R("sfx_step_stone", 4).Concat(R("sfx_step_wood", 4)).ToArray();
            am.StackMax = C("sfx_stack_max"); am.Bell = C("sfx_customer_bell"); am.Served = C("sfx_customer_served");
            am.UpgradeLoop = C("sfx_upgrade_tick_loop"); am.UpgradeComplete = C("sfx_upgrade_complete");
            am.ComboX15 = C("sfx_combo_x1_5"); am.ComboX2 = C("sfx_combo_x2"); am.NonnoStart = C("sfx_nonno_start");
            return am;
        }
    }

    static class PrefabExt
    {
        public static T AddComponentPrefab<T>(this GameObject prefab) where T : Component
        {
            var path = AssetDatabase.GetAssetPath(prefab); var root = PrefabUtility.LoadPrefabContents(path);
            root.AddComponent<T>(); PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<T>();
        }
    }
}
