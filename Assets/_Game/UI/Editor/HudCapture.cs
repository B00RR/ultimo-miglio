using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.EditorTools
{
    /// <summary>
    /// Batch layout verification: renders the HUD + phone (UI only, transparent background) at several window
    /// sizes into Captures/hud/*.png, with ALL contextual panels shown at once (worst case for overlaps).
    ///   xvfb-run -a Unity -batchmode -projectPath . -executeMethod UltimoMiglio.UI.EditorTools.HudCapture.Run
    /// (no -quit: exits itself). Optional: -hudSizes 1920x1080,1366x768
    /// </summary>
    [InitializeOnLoad]
    public static class HudCapture
    {
        const string Key = "UM_HUD_CAPTURE";
        static readonly Vector2Int[] Default = { new(1920, 1080), new(1366, 768), new(960, 600), new(1170, 2532), new(1080, 1920) };
        static List<(Vector2Int size, bool phone)> shots; static int idx, wait;
        static RenderTexture rt; static string outDir;

        static HudCapture() { EditorApplication.playModeStateChanged += OnPlay; }

        public static void Run()
        {
            HudSetup.BuildAll();
            EditorSceneManager.OpenScene("Assets/_Game/UI/Demo/HudDemo.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlay(PlayModeStateChange s)
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                outDir = Path.GetFullPath("Captures/hud"); Directory.CreateDirectory(outDir);
                var sizes = new List<Vector2Int>(Default);
                var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-hudSizes");
                if (i >= 0 && i + 1 < a.Length) { sizes.Clear(); foreach (var t in a[i + 1].Split(',')) { var p = t.Split('x'); sizes.Add(new Vector2Int(int.Parse(p[0]), int.Parse(p[1]))); } }
                shots = new List<(Vector2Int, bool)>();
                foreach (var z in sizes) { shots.Add((z, false)); shots.Add((z, true)); }
                idx = 0; wait = -1;
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
            var phone = UnityEngine.Object.FindFirstObjectByType<PhoneController>();
            if (!hud || !phone || !hud.IsBound) return;
            if (idx >= shots.Count) { EditorApplication.update -= Step; ResponsivePanel.SizeOverride = null; EditorApplication.ExitPlaymode(); return; }
            var (size, withPhone) = shots[idx];
            if (wait < 0)
            {
                if (rt) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                rt = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
                ResponsivePanel.SizeOverride = size;
                foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                {
                    var ps = doc.panelSettings; ps.targetTexture = rt;
                    bool isHud = doc.GetComponent<HudController>();
                    ps.clearColor = isHud; ps.colorClearValue = new Color(0, 0, 0, 0);
                }
                var wl = hud.Root.Q("world-layer"); if (wl != null) wl.style.display = DisplayStyle.None; // demo world cards depend on the demo camera, not on layout
                // worst case: every contextual element at once
                hud.SetCurrencies(1240, 12); hud.SetPhoneBadge(2);
                hud.ShowPedaggio(0.15f); hud.ShowToast("La Cassa ha 240 LN", 60f); hud.ShowRoomLabel("Magazzino");
                hud.ShowSafeCard(new SafeCardData { chapter = 1, trancheTotal = 8400, tranchePaid = 2100, day = 4, daysPerYear = 365, installmentIndex = 1, installmentAmount = 8400, installmentDueDay = 24, tollActive = true, tollFraction = 0.15f });
                if (withPhone) phone.OpenPhone(); else phone.Close();
                wait = 45; return;
            }
            if (--wait > 0) return;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            if (SystemInfo.graphicsUVStartsAtTop) FlipY(tex);
            string file = Path.Combine(outDir, $"hud_{size.x}x{size.y}{(withPhone ? "_phone" : "")}.png");
            File.WriteAllBytes(file, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[HudCapture] " + file);
            idx++; wait = -1;
        }

        static void FlipY(Texture2D t)
        {
            var px = t.GetPixels32(); int w = t.width, h = t.height; var o = new Color32[px.Length];
            for (int y = 0; y < h; y++) Array.Copy(px, y * w, o, (h - 1 - y) * w, w);
            t.SetPixels32(o); t.Apply();
        }
    }
}
