using System.Collections.Generic;
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
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath)) TryRun("UltimoMiglio.UI.EditorTools.HudSetup", "BuildAll"); // may open its demo scene: run before NewScene
            try { FontSetup.Apply(); } catch (System.Exception ex) { Debug.LogWarning("[Scene] fonts: " + ex.Message); }
            Directory.CreateDirectory(PrefabDir); Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var zones = AssetDatabase.LoadAssetAtPath<ChapterDataAsset>(ChapterDataImporter.AssetPath).Data.zones;
            float dArr = (float)zones.distancesM.arriviToShelves, dBan = (float)zones.distancesM.shelvesToBancone;

            var world = Mat("M_Diorama_Atlas", UM_Palette.Pavimento); var props = Mat("M_Props_Interactive", UM_Palette.Cartone); var chars = Mat("M_Char_Atlas", UM_Palette.AranceNina);

            // --- floor + walls
            var floor = Box("Floor", null, new Vector3(0, -0.05f, 0), new Vector3(18, 0.1f, 22), world, UM_Palette.Pavimento);
            floor.isStatic = true;
            // --- invisible boundary on all 4 sides (Nina can never leave the map)
            foreach (var (n, c, sz) in new[] { ("Bound_N", new Vector3(0, 1.5f, 11.15f), new Vector3(18.6f, 3, 0.3f)), ("Bound_S", new Vector3(0, 1.5f, -11.15f), new Vector3(18.6f, 3, 0.3f)),
                                               ("Bound_E", new Vector3(9.15f, 1.5f, 0), new Vector3(0.3f, 3, 22.6f)), ("Bound_W", new Vector3(-9.15f, 1.5f, 0), new Vector3(0.3f, 3, 22.6f)) })
            { var b = new GameObject(n); b.transform.position = c; b.AddComponent<BoxCollider>().size = sz; b.isStatic = true; }
            var kitOk = DepotKit.Build(world);
            if (kitOk) floor.GetComponent<Renderer>().enabled = false;

            // --- GameRoot + audio + FX
            var root = new GameObject("GameRoot").AddComponent<GameRoot>();
            var audio = BuildAudio(root.transform);
            var fxGo = new GameObject("UM_FX"); fxGo.transform.SetParent(root.transform);
            var fx = fxGo.AddComponent<UM_FxPlayer>();
            GameObject VP(string n) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/VFX/Prefabs/{n}.prefab");
            fx.packagePop = VP("fx_package_pop"); fx.coins = VP("fx_coins"); fx.upgrade = VP("fx_upgrade");
            fx.packagePickup = VP("fx_package_pickup"); fx.packageDeliver = VP("fx_package_deliver"); fx.upgradeComplete = VP("fx_upgrade_complete");

            // --- prefabs
            var parcelFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Props/prop_parcel_box.fbx");
            GameObject parcelGo;
            if (parcelFbx)
            {
                parcelGo = new GameObject("Parcel"); var pm = (GameObject)PrefabUtility.InstantiatePrefab(parcelFbx); pm.transform.SetParent(parcelGo.transform, false);
                var pb = pm.GetComponentsInChildren<Renderer>(); var bb = pb[0].bounds; foreach (var r in pb) bb.Encapsulate(r.bounds);
                float k = 0.32f / Mathf.Max(bb.size.y, 0.01f); pm.transform.localScale *= Mathf.Min(k, 0.45f / Mathf.Max(bb.size.x, bb.size.z, 0.01f));
                pm.transform.localPosition = new Vector3(0, -0.16f, 0);
                foreach (var r in pm.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, _ => props);
                foreach (var c in pm.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                PrefabUtility.UnpackPrefabInstance(pm, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            else parcelGo = Box("Parcel", null, Vector3.zero, Vector3.one * 0.3f, props, UM_Palette.Cartone);
            var parcel = SavePrefab(parcelGo, "Parcel");
            var coinMesh = new[] { "Assets/_Game/Art/FX/fx_coin.fbx", "Assets/_Game/Art/FX/fx_coin_lira.fbx" }.Select(AssetDatabase.LoadAssetAtPath<GameObject>).FirstOrDefault(g => g);
            var coinGo = coinMesh ? (GameObject)PrefabUtility.InstantiatePrefab(coinMesh) : Prim(PrimitiveType.Cylinder, "Coin", null, Vector3.zero, new Vector3(0.25f, 0.02f, 0.25f), props, UM_Palette.GialloSole);
            coinGo.name = "Coin"; foreach (var c in coinGo.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var coin = SavePrefab(coinGo, "Coin");
            // #2: i 6 clienti riggati (Humanoid di Nina, controller chr_cliente) al posto delle capsule; un prefab per variante.
            var custVariants = new List<Customer>();
            {
                const string cd = "Assets/_Game/Art/Characters/Clienti/";
                var cctl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(cd + "chr_cliente.controller");
                var cmat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/VFX/Materials/M_Char_Atlas.mat");
                foreach (var v in new[] { "base", "anziana", "residente", "rider", "turista", "vip" })
                {
                    var path = $"{cd}chr_cliente_{v}.fbx"; var cfbx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (!cctl || !cfbx) { Debug.LogWarning($"[Scene] cliente riggato mancante: {v}"); continue; }
                    var go = new GameObject("Customer");
                    var m = (GameObject)PrefabUtility.InstantiatePrefab(cfbx, go.transform); m.transform.localPosition = Vector3.zero; m.transform.localRotation = Quaternion.identity; // fronte +Z come Nina
                    if (cmat) foreach (var r in m.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, _ => cmat);
                    var ca = m.GetComponent<Animator>() ?? m.AddComponent<Animator>();
                    var av = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault(); if (av) ca.avatar = av;
                    ca.runtimeAnimatorController = cctl; ca.applyRootMotion = false; ca.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    AddBody(go); // #111: corpo solido
                    var pc = SavePrefab(go, v == "base" ? "Customer" : $"Customer_{v}").AddComponentPrefab<Customer>();
                    NonnoRobot.StripNavMeshAgents(pc.gameObject); custVariants.Add(pc);
                }
            }
            if (custVariants.Count == 0)
            {
                Debug.LogWarning("[Scene] clienti riggati mancanti: capsula");
                var capGo = Prim(PrimitiveType.Capsule, "Customer", null, Vector3.zero, new Vector3(0.5f, 0.8f, 0.5f), chars, UM_Palette.Menta); Object.DestroyImmediate(capGo.GetComponent<Collider>());
                AddBody(capGo); custVariants.Add(SavePrefab(capGo, "Customer").AddComponentPrefab<Customer>());
            }
            var cust = custVariants[0];

            // --- Nina
            var nina = new GameObject("Nina"); nina.transform.position = new Vector3(0, 0, -1f); // #60: spawn sicuro nell'area giocabile
            var cc = nina.AddComponent<CharacterController>(); cc.center = new Vector3(0, 0.8f, 0); cc.height = 1.6f; cc.radius = 0.3f;
            var ninaAnim = Rig(nina.transform, "chr_nina_vela", 0f);
            if (!ninaAnim)
            {
                Prim(PrimitiveType.Capsule, "Body", nina.transform, new Vector3(0, 0.8f, 0), new Vector3(0.6f, 0.8f, 0.6f), chars, UM_Palette.AranceNina);
                Prim(PrimitiveType.Sphere, "Helmet", nina.transform, new Vector3(0, 1.65f, 0.02f), Vector3.one * 0.42f, chars, UM_Palette.Terracotta);
            }
            var anchor = new GameObject("StackAnchor").transform; anchor.SetParent(nina.transform, false); anchor.localPosition = new Vector3(0, 0.9f, 0.38f);
            var shadowPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/VFX/Prefabs/fx_contact_shadow.prefab");
            if (shadowPf) { var sh = (GameObject)PrefabUtility.InstantiatePrefab(shadowPf); sh.transform.SetParent(nina.transform, false); sh.transform.localPosition = new Vector3(0, 0.02f, 0); }
            var stack = nina.AddComponent<PackageStack>(); stack.Anchor = anchor; stack.ParcelPrefab = parcel;
            var ctrl = nina.AddComponent<NinaController>(); ctrl.Stack = stack; ctrl.Animator = ninaAnim; root.Nina = ctrl;
            // HUD (UI/UX, UI Toolkit): HUD.prefab owns the FloatingJoystick (overlay exclusion + graphics)
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject hudGo = hudPrefab ? (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab) : null;
            var hud = hudGo ? hudGo.GetComponentInChildren<UltimoMiglio.UI.HudController>(true) : null;
            var joy = hudGo ? hudGo.GetComponentInChildren<UltimoMiglio.UI.FloatingJoystick>(true) : null;
            if (!joy) { Debug.LogWarning("[Scene] HUD.prefab missing: bare joystick"); joy = new GameObject("FloatingJoystick").AddComponent<UltimoMiglio.UI.FloatingJoystick>(); }
            ctrl.Joystick = joy;

            // --- zones
            var arrPos = new Vector3(-5, 0, 6);
            var arrivals = Zone<ArrivalsZone>("Zona_Arrivi", arrPos, new Vector3(2.6f, 1, 2.6f), UM_Palette.GialloSole, props);
            var shelves = new Shelf[3]; var cols = new[] { UM_Palette.Terracotta, UM_Palette.Menta, UM_Palette.Limone };
            for (int i = 0; i < 3; i++)
            {
                var p = arrPos + new Vector3((i - 1) * 2.2f, 0, -dArr);
                var body = Box($"Scaffale_{(char)('A' + i)}", null, p + new Vector3(0, 0.9f, -0.6f), new Vector3(1.8f, 1.8f, 0.5f), world, UM_Palette.Legno);
                body.AddComponent<NavMeshObstacle>().carving = true;
                // #58: BoxCollider semplice (il CharacterController scivola sui box, B5)
                // #47: scaffale caricato se presente
                if (kitOk && DepotKit.Place(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Environment/Kit/prop_shelf_2m_loaded.fbx") ? "Kit/prop_shelf_2m_loaded" : "Kit/prop_shelf_2m", null, body.transform.position - new Vector3(0, 0.9f, 0), 0, false)) body.GetComponent<Renderer>().enabled = false;
                Box("Code", body.transform, new Vector3(0, 0.55f, 0.52f), new Vector3(0.9f, 0.08f, 0.1f), world, cols[i]);
                shelves[i] = Zone<Shelf>($"Zona_Scaffale_{(char)('A' + i)}", p + new Vector3(0, 0, 0.6f), new Vector3(1.8f, 1, 1.2f), cols[i], props);
                shelves[i].Color = (ParcelColor)i; shelves[i].Count = 4;
                var pick = Zone<ShelfPickup>($"Zona_Prendi_{(char)('A' + i)}", p + new Vector3(0, 0, 1.6f), new Vector3(1.8f, 1, 0.7f), UM_Palette.Crema, props); pick.Shelf = shelves[i];
            }
            var shelfC = arrPos + new Vector3(0, 0, -dArr);
            var banPos = shelfC + new Vector3(dBan * 0.8f, 0, -dBan * 0.6f); // 9.4 m
            if (kitOk) DepotKit.DressGameplay(arrPos, shelfC, banPos); // #47 set dressing denso
            var banMesh = Box("Bancone_Mesh", null, banPos + new Vector3(0, 0.5f, -0.9f), new Vector3(2.4f, 1f, 0.7f), world, UM_Palette.Terracotta); banMesh.AddComponent<NavMeshObstacle>().carving = true;
           // #58: BoxCollider solido
            // #47: counter + Carico0 + rug
            if (kitOk && DepotKit.Place("Kit/prop_counter_2m", null, banPos + new Vector3(0, 0, -0.9f), 0, false)) { banMesh.GetComponent<Renderer>().enabled = false; DepotKit.Place("Kit/prop_register", null, banPos + new Vector3(0.6f, 1.0f, -0.9f), 0, false, true); DepotKit.Place("Kit/prop_carico0", null, banPos + new Vector3(-0.15f, 1.0f, -0.9f), 8, false, true); DepotKit.Place("ThirdParty/Props/prop_tp_lemon", null, banPos + new Vector3(-0.8f, 1.0f, -0.9f), 0, false, true); DepotKit.Place("Kit/prop_rug_bancone", null, banPos + new Vector3(0, 0.005f, 0.2f), 0, false); }
            var bancone = Zone<Bancone>("Zona_Bancone", banPos, new Vector3(2.4f, 1, 1.2f), UM_Palette.Terracotta, props);
            var coins = new GameObject("Coins").AddComponent<CoinSpawner>(); coins.CoinPrefab = coin; bancone.Coins = coins;
            var queue = new GameObject("CustomerQueue").AddComponent<CustomerQueue>(); queue.Prefab = cust; queue.Variants = custVariants.ToArray();
            queue.CounterSlot = new GameObject("CounterSlot").transform; queue.CounterSlot.position = banPos + new Vector3(0, 0, -1.8f);
            queue.Door = new GameObject("Door").transform; queue.Door.position = new Vector3(8.5f, 0, -10.5f); queue.LineStep = new Vector3(0.7f, 0, -0.7f);
            bancone.Queue = queue;
            var cassa = Zone<CassaZone>("Zona_Cassa", banPos + new Vector3(3.2f, 0, 1.5f), new Vector3(1.4f, 1, 1.4f), UM_Palette.TealOmni, props); cassa.Coins = coins;

            // --- pads (near the Cassa, 01 §3)
            var padRow = banPos + new Vector3(-1.5f, 0, 4.5f);
            Pad("Pad_Zaino", UpgradeKind.Zaino, padRow, 0, props);
            // #101: passo padNuovi (onboarding.json: gate padPurchasedAny su scarpe/insegna). Senza questi pad il tutorial si bloccava al passo 11.
            Pad("Pad_Scarpe", UpgradeKind.Scarpe, padRow + new Vector3(1.8f, 0, 0), 0, props);
            Pad("Pad_Insegna", UpgradeKind.Insegna, padRow + new Vector3(3.6f, 0, 0), 0, props);
            Pad("Pad_AssumiNonno", UpgradeKind.NonnoHire, arrPos + new Vector3(3.2f, 0, 0.5f), 0, props); // onboarding: last step

            // --- NONNO
            var nonno = new GameObject("NONNO"); nonno.transform.position = arrPos + new Vector3(2.5f, 0, -2);
            { var col = nonno.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 0.5f, 0); col.height = 1.0f; col.radius = NonnoRobot.BodyRadius; } // #111
            var nonnoAnim = Rig(nonno.transform, "bot_nonno", 0f) /* #59: forche su +Z, il 180° lo faceva camminare all'indietro */;
            if (!nonnoAnim)
            {
                Box("Body", nonno.transform, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.8f, 1f), chars, UM_Palette.TealOmni);
                Box("Forks", nonno.transform, new Vector3(0, 0.15f, 0.7f), new Vector3(0.6f, 0.06f, 0.6f), chars, UM_Palette.ScuroCaldo);
            }
            // No NavMeshAgent: WebGL bake is missing at runtime ("Failed to create agent..."). NONNO walks with Transform.
            var bot = nonno.AddComponent<NonnoRobot>(); bot.Arrivals = arrivals; bot.Shelves = shelves; bot.Bancone = bancone; bot.Queue = queue; bot.Animator = nonnoAnim;
            NonnoRobot.StripNavMeshAgents(nonno); // editor+runtime: mai NavMeshAgent su NONNO
            if (shadowPf) { var nsh = (GameObject)PrefabUtility.InstantiatePrefab(shadowPf); nsh.transform.SetParent(nonno.transform, false); nsh.transform.localPosition = new Vector3(0, 0.02f, 0); nsh.transform.localScale = Vector3.one * 1.1f; }

            // --- NavMesh
            var surf = floor.AddComponent<NavMeshSurface>(); surf.collectObjects = CollectObjects.All; surf.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surf.BuildNavMesh();

            // --- camera + lighting + post
            var cam = new GameObject("Main Camera"); cam.tag = "MainCamera"; var camera = cam.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = UM_Palette.Cielo;
            var urpCam = cam.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); urpCam.renderPostProcessing = false;
            cam.AddComponent<AudioListener>();
            var rig = new GameObject("CameraRig").AddComponent<CameraRigSetup>(); rig.Target = nina.transform; rig.Build(camera);
            WireHud(hudGo, hud, joy, camera, nina.transform, cassa.transform);
            TryRun("UltimoMiglio.VFX.EditorTools.UM_LightingSetup", "Setup");
            TryRun("UltimoMiglio.VFX.EditorTools.UM_PostProcessBuilder", "BuildAll");
            TryRun("UltimoMiglio.VFX.EditorTools.UM_VfxSceneSetup", "Setup");
            if (!Object.FindAnyObjectByType<Light>()) { var l = new GameObject("Sun").AddComponent<Light>(); l.type = LightType.Directional; l.transform.rotation = Quaternion.Euler(50, -35, 0); l.color = UM_Palette.Sole; }

            // --- blockout reference (05_3D_PIPELINE), hidden
            var glb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Art/Blockout/depot_blockout_v01.glb");
            if (glb) { var b = (GameObject)PrefabUtility.InstantiatePrefab(glb); b.name = "REF_Blockout_v01"; b.SetActive(false); }

            // --- onboarding (Nonna Ada)
            var tut = new GameObject("Tutorial").AddComponent<TutorialManager>(); tut.transform.SetParent(root.transform);
            var tj = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Game/Data/ch1/onboarding_testi.json");
            if (tj) { var td = JsonUtility.FromJson<TutTexts>(tj.text); tut.Speaker = td.speaker; tut.Texts = TutorialManager.TextsById(td.steps.Select(x => (x.id, x.text))); tut.EndText = td.fine; tut.TestiJson = tj; }
            tut.Arrivi = arrivals.transform; tut.Scaffale = shelves[0].transform; tut.Bancone = bancone.transform;
            tut.Monete = new GameObject("Tut_Monete").transform; tut.Monete.position = banPos + Vector3.right * 0.8f;
            foreach (var pz in Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)) { if (pz.Kind == UpgradeKind.Zaino) tut.PadZaino = pz.transform; if (pz.Kind == UpgradeKind.NonnoHire) tut.PadNonno = pz.transform; }
            AdaScenePlacement.Place(banPos, root.transform); // #118: Nonna Ada 3D a fianco del Bancone (null-safe)
            TryRun("UltimoMiglio.VFX.EditorTools.UM_TutorialMarkerSetup", "Setup"); // #4: marker tutorial con materiali assegnati (in build Shader.Find non trova shader non referenziati)

            TryRun("UltimoMiglio.VFX.EditorTools.UM_FxPrefabBuilder", "BuildAll");
            // --- punti di interazione stile My Little Universe (pad a terra + chip billboard)
            {
                Texture2D I(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Game/UI/Icons/{n}.png");
                var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/UI/Fonts/Fredoka-Bold.ttf");
                var ringPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/VFX/Prefabs/fx_pad_fill_ring.prefab");
                foreach (var z in Object.FindObjectsByType<ProximityZone>(FindObjectsSortMode.None))
                {
                    var ip = z.gameObject.AddComponent<InteractionPoint>(); ip.LabelFont = font;
                    var bc = z.GetComponent<BoxCollider>(); ip.Radius = bc ? Mathf.Clamp(Mathf.Min(bc.size.x, bc.size.z) * 0.5f, 0.45f, 1.2f) : 0.9f;
                    switch (z)
                    {
                        case ArrivalsZone _: ip.Kind = PoiKind.Prendi; ip.Label = "Pacchi"; ip.Icon = I("ic_box"); break;
                        case ShelfPickup _: ip.Kind = PoiKind.Prendi; ip.Label = "Prendi"; ip.Icon = I("ic_box"); break;
                        case Shelf _: ip.Kind = PoiKind.Consegna; ip.Label = "Scaffale"; ip.Icon = I("ic_down"); break;
                        case Bancone _: ip.Kind = PoiKind.Consegna; ip.Label = "Bancone"; ip.Icon = I("ic_customer"); break;
                        case CassaZone _: ip.Kind = PoiKind.Prendi; ip.Label = "Monete"; ip.Icon = I("ic_coin"); ip.HasCard = true; break;
                        case UpgradePad up:
                            ip.Kind = PoiKind.Acquisto; ip.Label = "0"; ip.Icon = I("ic_coin"); ip.HasCard = true; ip.Pad = up; ip.Radius = 0.95f;
                            if (ringPf) { var rg = (GameObject)PrefabUtility.InstantiatePrefab(ringPf); rg.transform.SetParent(up.transform, false); rg.transform.localPosition = new Vector3(0, 0.1f, 0); up.FillRing = rg.GetComponent<UltimoMiglio.VFX.UM_PadFillRing>(); }
                            break;
                        default: ip.Kind = PoiKind.Prendi; ip.Label = z.name.Replace("Zona_", ""); break;
                    }
                    if (ringPf && !(z is UpgradePad))
                    {
                        var rg = (GameObject)PrefabUtility.InstantiatePrefab(ringPf); rg.transform.SetParent(z.transform, false); rg.transform.localPosition = new Vector3(0, 0.1f, 0);
                        rg.transform.localScale = Vector3.one * (ip.Radius / 0.95f);
                        var fr = rg.GetComponent<UltimoMiglio.VFX.UM_PadFillRing>(); if (fr) { fr.playCompleteBurst = false; fr.kind = ip.Kind == PoiKind.Consegna ? UltimoMiglio.VFX.UM_PadFillRing.Kind.Delivery : UltimoMiglio.VFX.UM_PadFillRing.Kind.Pickup; ip.Ring = fr; }
                    }
                    // QA #41: no PadTag / floating chips — ground pads only (InteractionPoint discs)
                    var padIcon = z.gameObject.AddComponent<UltimoMiglio.VFX.UM_PadIcon>();
                    padIcon.kindFromRing = true;
                    if (ip.Ring) padIcon.SetKind(ip.Ring.kind);
                    else padIcon.SetKind(ip.Kind == PoiKind.Prendi ? UltimoMiglio.VFX.UM_PadFillRing.Kind.Pickup : ip.Kind == PoiKind.Consegna ? UltimoMiglio.VFX.UM_PadFillRing.Kind.Delivery : UltimoMiglio.VFX.UM_PadFillRing.Kind.Purchase);
                    padIcon.AssignDefaultIcon();
                    padIcon.SetVisible(false);
                    ip.PadIcon = padIcon;
                    var oldPad = z.transform.Find("Pad"); if (oldPad) { var r = oldPad.GetComponent<Renderer>(); if (r) r.enabled = false; }
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Scene] built " + ScenePath);
        }

        const string HudPrefabPath = "Assets/_Game/UI/Prefabs/HUD.prefab", UiRoot = "Assets/_Game/UI";

        static void WireHud(GameObject hudGo, UltimoMiglio.UI.HudController hud, UltimoMiglio.UI.FloatingJoystick joy, Camera cam, Transform nina, Transform cassa)
        {
            if (!hud) return;
            Set(joy, "cameraTransform", cam.transform);
            var arrow = hudGo.GetComponentInChildren<UltimoMiglio.UI.EdgeArrow>(true);
            if (arrow) { Set(arrow, "cam", cam); Set(arrow, "nina", nina); var aso = new SerializedObject(arrow); var mp = aso.FindProperty("marginDp"); if (mp != null) mp.floatValue = 110f; var sm = aso.FindProperty("showMarkerWhenVisible"); if (sm != null) sm.boolValue = false; aso.ApplyModifiedPropertiesWithoutUndo(); }
            var sc = World<UltimoMiglio.UI.World.StackCounter>(nina.gameObject, hud, cam, "StackCounter.uxml", new Vector3(0, 2.2f, 0));
            UltimoMiglio.UI.World.CassaCard cc = null; // demo: niente schede grandi
            const string ringPath = "Assets/_Game/Art/Materials/M_ZoneRing.mat";
            var ringMat = AssetDatabase.LoadAssetAtPath<Material>(ringPath);
            if (!ringMat) { Directory.CreateDirectory(Path.GetDirectoryName(ringPath)); ringMat = new Material(Shader.Find("UltimoMiglio/UI/ZoneRing") ?? Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(ringMat, ringPath); }
            foreach (var z in Object.FindObjectsByType<ProximityZone>(FindObjectsSortMode.None))
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = "Ring"; quad.transform.SetParent(z.transform, false); quad.transform.localPosition = new Vector3(0, 0.03f, 0);
                quad.transform.localRotation = Quaternion.Euler(90, 0, 0); var bc = z.GetComponent<BoxCollider>(); quad.transform.localScale = new Vector3(bc.size.x, bc.size.z, 1);
                quad.GetComponent<Renderer>().sharedMaterial = ringMat;
                var ring = quad.AddComponent<UltimoMiglio.UI.ZoneFillRing>(); Set(ring, "ringRenderer", quad.GetComponent<Renderer>()); ring.SetFill(0.35f); z.Ring = ring;
                // demo: niente UpgradePadCard
            }
            var bridge = hudGo.AddComponent<UltimoMiglio.UI.Bridge.HudGameBridge>();
            bridge.Hud = hud; bridge.Phone = hudGo.GetComponentInChildren<UltimoMiglio.UI.PhoneController>(true); bridge.Joystick = joy;
            bridge.StackCounter = sc; bridge.CassaCard = cc; bridge.EdgeArrow = arrow; bridge.Cassa = cassa;
        }
        static string PadTitle(UpgradeKind k) => k switch { UpgradeKind.NonnoHire => "ASSUMI OLDO", UpgradeKind.NonnoLevel => "OLDO +1", UpgradeKind.Zaino => "ZAINO", UpgradeKind.Scarpe => "SCARPE", _ => "INSEGNA" };
        static T World<T>(GameObject host, UltimoMiglio.UI.HudController hud, Camera cam, string uxml, Vector3 offset) where T : UltimoMiglio.UI.World.WorldAnchoredElement
        {
            var c = host.AddComponent<T>(); var so = new SerializedObject(c);
            so.FindProperty("hud").objectReferenceValue = hud; so.FindProperty("cam").objectReferenceValue = cam; so.FindProperty("anchor").objectReferenceValue = host.transform;
            so.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>($"{UiRoot}/UXML/{uxml}");
            so.FindProperty("worldOffset").vector3Value = offset; so.ApplyModifiedPropertiesWithoutUndo(); return c;
        }
        static void Set(Object o, string field, Object value)
        {
            if (!o) return; var so = new SerializedObject(o); var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Scene] {o.GetType().Name}.{field} not found"); return; }
            p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Animator delivery (Art/Characters): model is -Z forward → rotated 180° Y; no root motion.</summary>
        static Animator Rig(Transform parent, string name, float yaw = 180f)
        {
            const string dir = "Assets/_Game/Art/Characters";
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{dir}/{name}.fbx");
            var ctl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{dir}/{name}.controller");
            if (!fbx || !ctl) { Debug.LogWarning($"[Scene] rig {name} missing (fbx={(bool)fbx}, controller={(bool)ctl}): graybox"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parent);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/VFX/Materials/M_Char_Atlas.mat");
            if (mat) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, _ => mat);
            var an = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctl; an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return an;
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
        /// <summary>#111: CapsuleCollider del cliente (Nina e gli altri clienti non lo attraversano).</summary>
        static void AddBody(GameObject go) { var col = go.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 0.8f, 0); col.height = 1.6f; col.radius = Customer.BodyRadius; }
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
            WireAudioClips(am); return am;
        }
        /// <summary>Assegna tutte le clip all'AudioManager (usato dal builder e da Audio/WireScene, che non rigenera la scena).</summary>
        public static void WireAudioClips(AudioManager am)
        {
            AudioClip C(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Game/Audio/{n}.ogg") ?? AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{(n.StartsWith("mus_") ? "Music" : "SFX")}/{n}.ogg");
            AudioClip[] R(string pfx, int n) => Enumerable.Range(1, n).Select(i => C($"{pfx}_{i:00}")).Where(c => c).ToArray();
            am.MusicStems = new[] { C("mus_day_vela_e_figli_base"), C("mus_day_vela_e_figli_perc"), C("mus_day_vela_e_figli_mel") };
            am.MusicFull = C("mus_day_vela_e_figli_full");
            am.Pickup = R("sfx_parcel_pickup", 8); am.Deposit = R("sfx_parcel_deposit", 4); am.CoinDrop = R("sfx_coin_drop", 4); am.CoinPickup = R("sfx_coin_pickup", 4);
            am.NonnoMumble = R("sfx_nonno_mumble", 3); am.Steps = R("sfx_step_stone", 4); // pavimento a lastre: solo pietra (wood per pedane future)
            am.UiTap = R("sfx_ui_tap", 2); am.TutorialArrow = C("sfx_ui_arrow_appear"); am.TutorialStep = C("sfx_ui_step_complete");
            am.StackMax = C("sfx_stack_max"); am.Bell = C("sfx_customer_bell"); am.Served = C("sfx_customer_served");
            am.UpgradeLoop = C("sfx_upgrade_tick_loop"); am.UpgradeComplete = C("sfx_upgrade_complete");
            am.ComboX15 = C("sfx_combo_x1_5"); am.ComboX2 = C("sfx_combo_x2"); am.NonnoStart = C("sfx_nonno_start");
        }
    }

    [System.Serializable] class TutStep { public string id, text; }
    [System.Serializable] class TutTexts { public string speaker, fine; public TutStep[] steps; }
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
