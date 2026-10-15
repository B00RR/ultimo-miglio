using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.UI.Demo;
using UltimoMiglio.UI.World;

namespace UltimoMiglio.UI.EditorTools
{
    /// <summary>
    /// Menu "Ultimo Miglio/UI/...": creates PanelSettings, the HUD prefab and a demo scene. Batchmode-friendly:
    /// Unity -batchmode -quit -projectPath unity -executeMethod UltimoMiglio.UI.EditorTools.HudSetup.BuildAll
    /// </summary>
    public static class HudSetup
    {
        const string Root = "Assets/_Game/UI";
        const string PanelPath = Root + "/Settings/HudPanelSettings.asset";
        const string PhonePanelPath = Root + "/Settings/PhonePanelSettings.asset";
        const string PrefabPath = Root + "/Prefabs/HUD.prefab";
        const string ScenePath = Root + "/Demo/HudDemo.unity";

        [MenuItem("Ultimo Miglio/UI/Build HUD (PanelSettings + Prefab + Demo)")]
        public static void BuildAll()
        {
            var hudPanel = EnsurePanel(PanelPath, 0);
            var phonePanel = EnsurePanel(PhonePanelPath, 10); // phone draws above the HUD
            var prefab = BuildPrefab(hudPanel, phonePanel);
            BuildDemoScene(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("[HUD] built: " + PrefabPath + ", " + ScenePath);
        }

        static PanelSettings EnsurePanel(string path, float sortOrder)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (!ps) { ps = ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(ps, path); }
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);  // landscape web build
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = sortOrder;
            if (!ps.themeStyleSheet)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:ThemeStyleSheet"))
                { ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(guid)); break; }
            }
            EditorUtility.SetDirty(ps);
            return ps;
        }

        static GameObject BuildPrefab(PanelSettings hudPanel, PanelSettings phonePanel)
        {
            System.IO.Directory.CreateDirectory(Root + "/Prefabs");
            var go = new GameObject("HUD");
            var hudGo = new GameObject("HudDocument"); hudGo.transform.SetParent(go.transform);
            var hudDoc = hudGo.AddComponent<UIDocument>();
            hudDoc.panelSettings = hudPanel;
            hudDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Root + "/UXML/Hud.uxml");
            var hud = hudGo.AddComponent<HudController>();

            var phoneGo = new GameObject("PhoneDocument"); phoneGo.transform.SetParent(go.transform);
            var phoneDoc = phoneGo.AddComponent<UIDocument>();
            phoneDoc.panelSettings = phonePanel;
            phoneDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Root + "/UXML/Phone.uxml");
            var phone = phoneGo.AddComponent<PhoneController>();

            var joyGo = new GameObject("FloatingJoystick"); joyGo.transform.SetParent(go.transform);
            var joy = joyGo.AddComponent<FloatingJoystick>();
            var so = new SerializedObject(joy);
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("phone").objectReferenceValue = phone;
            so.ApplyModifiedPropertiesWithoutUndo();

            var arrowGo = new GameObject("EdgeArrow"); arrowGo.transform.SetParent(go.transform);
            var arrow = arrowGo.AddComponent<EdgeArrow>();
            var sa = new SerializedObject(arrow); sa.FindProperty("hud").objectReferenceValue = hud; sa.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void BuildDemoScene(GameObject prefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var demo = new GameObject("HudDemo").AddComponent<HudDemo>();
            var sd = new SerializedObject(demo);
            sd.FindProperty("hud").objectReferenceValue = inst.GetComponentInChildren<HudController>();
            sd.FindProperty("phone").objectReferenceValue = inst.GetComponentInChildren<PhoneController>();
            sd.ApplyModifiedPropertiesWithoutUndo();

            // world indicator samples
            var cam = Camera.main; cam.orthographic = true; cam.transform.SetPositionAndRotation(new Vector3(-12, 14, -12), Quaternion.Euler(35.3f, 45f, 0));
            var pad = GameObject.CreatePrimitive(PrimitiveType.Quad); pad.name = "SamplePadRing"; pad.transform.rotation = Quaternion.Euler(90, 0, 0); pad.transform.localScale = Vector3.one * 2.5f;
            var sh = Shader.Find("UltimoMiglio/UI/ZoneRing"); if (sh) pad.GetComponent<Renderer>().sharedMaterial = new Material(sh);
            var ring = pad.AddComponent<ZoneFillRing>(); ring.SetFill(0.4f);
            var card = pad.AddComponent<UpgradePadCard>();
            var sc = new SerializedObject(card);
            sc.FindProperty("hud").objectReferenceValue = inst.GetComponentInChildren<HudController>();
            sc.FindProperty("template").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Root + "/UXML/PadCard.uxml");
            sc.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
