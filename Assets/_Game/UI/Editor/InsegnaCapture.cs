using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.EditorTools
{
    /// <summary>Catture HUD direzione C a 1 px = 1 dp (come i mockup), senza il diorama.
    ///   Unity -batchmode -projectPath . -executeMethod UltimoMiglio.UI.EditorTools.InsegnaCapture.Run -logFile -
    /// Scrive Captures/hud/insegna_*.png (390×844 ordine aperto, 360×800 icona, più le varianti).</summary>
    public static class InsegnaCapture
    {
        const string Key = "UM_INSEGNA_CAPTURE";
        struct Shot { public int w, h; public bool open; public string name; }
        static readonly Shot[] Shots =
        {
            new Shot { w = 390, h = 844, open = true, name = "insegna_390x844.png" },
            new Shot { w = 390, h = 844, open = false, name = "insegna_390x844_icona.png" },
            new Shot { w = 360, h = 800, open = false, name = "insegna_360x800.png" },
            new Shot { w = 360, h = 800, open = true, name = "insegna_360x800_aperto.png" },
        };
        static int idx, wait, spins;
        static double readyAt;
        static RenderTexture rt;
        static string outDir;

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Game/UI/Demo/HudDemo.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        static void Hook() { EditorApplication.playModeStateChanged += OnPlay; }

        static void OnPlay(PlayModeStateChange s)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                outDir = Path.GetFullPath("Captures/hud");
                Directory.CreateDirectory(outDir);
                idx = 0; wait = -1; spins = 0;
                EditorApplication.update += Step;
            }
            else if (s == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseBool(Key);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        static void Step()
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<HudController>();
            if (!hud || !hud.IsBound)
            {
                if (++spins > 900) { Debug.LogError("[InsegnaCapture] HUD non pronto"); EditorApplication.Exit(1); }
                return;
            }
            if (idx >= Shots.Length)
            {
                EditorApplication.update -= Step;
                ResponsivePanel.SizeOverride = null;
                CopyToDocs();
                EditorApplication.ExitPlaymode();
                return;
            }
            var shot = Shots[idx];
            if (wait < 0)
            {
                foreach (var rp in UnityEngine.Object.FindObjectsByType<ResponsivePanel>(FindObjectsSortMode.None)) rp.enabled = false;
                ResponsivePanel.SizeOverride = new Vector2Int(shot.w, shot.h);
                if (rt) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                rt = new RenderTexture(shot.w, shot.h, 24, RenderTextureFormat.ARGB32);
                var doc = hud.GetComponent<UIDocument>();
                var ps = doc.panelSettings;
                ps.scaleMode = PanelScaleMode.ConstantPixelSize;
                ps.scale = 1f;
                ps.targetTexture = rt;
                ps.clearColor = true;
                ps.colorClearValue = new Color(0.957f, 0.910f, 0.816f, 1f);
                var wl = hud.Root.Q("world-layer"); if (wl != null) wl.style.display = DisplayStyle.None;
                if (hud.SafeRoot != null) SafeAreaUtil.ApplyPadding(hud.SafeRoot);
                hud.SetCurrencies(1240, 0);
                hud.SetDay(3, 365, false);
                var obj = hud.Root.Q<Label>("tutorial-objective");
                if (obj != null) obj.text = "Porta 3 pacchi al Bancone";
                Set(hud, "order-count", "0/3");
                Set(hud, "order-chip-count", "0/3");
                Set(hud, "cargo-label", "CARICO 2/4");
                var motion = hud.GetComponent<InsegnaMotion>() ?? hud.gameObject.AddComponent<InsegnaMotion>();
                motion.SetOrderOpen(shot.open);
                readyAt = EditorApplication.timeSinceStartup + 1.1;
                wait = 1;
                return;
            }
            if (EditorApplication.timeSinceStartup < readyAt) return;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(shot.w, shot.h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, shot.w, shot.h), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            if (SystemInfo.graphicsUVStartsAtTop) FlipY(tex);
            string file = Path.Combine(outDir, shot.name);
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[InsegnaCapture] " + file);
            idx++; wait = -1;
        }

        static void Set(HudController hud, string name, string text)
        {
            var l = hud.Root.Q<Label>(name);
            if (l != null) l.text = text;
        }

        static void CopyToDocs()
        {
            string docs = Path.GetFullPath("docs/qa/hud_insegna_c");
            Directory.CreateDirectory(docs);
            foreach (var shot in Shots)
            {
                string from = Path.Combine(outDir, shot.name);
                if (!File.Exists(from)) continue;
                string to = Path.Combine(docs, shot.name.Replace("insegna_", "hud_"));
                File.Copy(from, to, true);
                Debug.Log("[InsegnaCapture] docs " + to);
            }
        }

        static void FlipY(Texture2D t)
        {
            var px = t.GetPixels32(); int w = t.width, h = t.height; var o = new Color32[px.Length];
            for (int y = 0; y < h; y++) Array.Copy(px, y * w, o, (h - 1 - y) * w, w);
            t.SetPixels32(o); t.Apply();
        }
    }
}
