using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltimoMiglio.Gameplay;
using Debug = UnityEngine.Debug;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Milestone capture in batchmode (no -quit; the script exits the editor itself):
    ///   Unity -batchmode -projectPath . -executeMethod UltimoMiglio.EditorTools.CaptureScript.Video  [-captureSeconds 20] [-captureName m1]
    ///   Unity -batchmode -projectPath . -executeMethod UltimoMiglio.EditorTools.CaptureScript.Still  -quit
    /// Video: enters Play Mode, fixed 30 fps (Time.captureFramerate), Nina follows a scripted tour (Arrivi → scaffali → Bancone → pad),
    /// renders Camera.main to a 1080x1920 RT each frame → Captures/<name>/f_00000.png → ffmpeg H.264 mp4 (Captures/<name>.mp4).
    /// Needs a GPU-less renderer? Run under xvfb-run without -nographics (rendering is required).</summary>
    [InitializeOnLoad]
    public static class CaptureScript
    {
        const string Key = "UM_CAPTURE"; const int W = 1080, H = 1920, Fps = 30;
        static int _frame, _total; static string _dir; static RenderTexture _rt; static Texture2D _tex;
        static readonly Vector3[] Tour = { new(-5, 0, 6), new(-5, 0, 1.6f), new(-5, 0, 1.2f), new(2.5f, 0, -5.6f), new(2.5f, 0, -5.6f), new(1, 0, -1.1f) };

        static CaptureScript() { EditorApplication.playModeStateChanged += OnPlayMode; }

        static string Arg(string name, string def)
        {
            var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : def;
        }

        public static void Video()
        {
            if (!File.Exists(SliceSceneBuilder.ScenePath)) SliceSceneBuilder.Build();
            EditorSceneManager.OpenScene(SliceSceneBuilder.ScenePath);
            string name = Arg("-captureName", "capture_" + DateTime.Now.ToString("yyyyMMdd_HHmm"));
            SessionState.SetString(Key, name + "|" + Arg("-captureSeconds", "20"));
            EditorApplication.EnterPlaymode();
        }

        public static void Still()
        {
            if (!File.Exists(SliceSceneBuilder.ScenePath)) SliceSceneBuilder.Build();
            EditorSceneManager.OpenScene(SliceSceneBuilder.ScenePath);
            var cam = Camera.main; var rig = UnityEngine.Object.FindAnyObjectByType<CameraRigSetup>();
            // edit mode: place camera like the follow cam would (pitch 35.3 yaw 45 ortho)
            cam.orthographic = true; cam.orthographicSize = rig ? rig.OrthoSize : 7.5f;
            cam.transform.rotation = Quaternion.Euler(35.3f, 45f, 0); cam.transform.position = (rig && rig.Target ? rig.Target.position : Vector3.zero) - cam.transform.forward * 30f;
            Directory.CreateDirectory("Captures");
            string path = Path.Combine("Captures", Arg("-captureName", "still_" + DateTime.Now.ToString("yyyyMMdd_HHmm")) + ".png");
            Grab(cam, path); Debug.Log("[Capture] still " + Path.GetFullPath(path));
        }

        static void OnPlayMode(PlayModeStateChange s)
        {
            var v = SessionState.GetString(Key, ""); if (string.IsNullOrEmpty(v)) return;
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                var p = v.Split('|'); _dir = Path.Combine("Captures", p[0]); _total = int.Parse(p[1]) * Fps; _frame = 0;
                Directory.CreateDirectory(_dir); Time.captureFramerate = Fps;
                if (GameRoot.I) { GameRoot.I.Eco.S.Wallet = 120; } // show the NONNO pad being bought in the tour
                EditorApplication.update += Step;
            }
            else if (s == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(Key);
                int code = Encode(_dir);
                if (Application.isBatchMode) EditorApplication.Exit(code);
            }
        }

        static void Step()
        {
            var nina = GameRoot.I ? GameRoot.I.Nina : null;
            if (nina)
            {
                float t = _frame / (float)_total * (Tour.Length - 1); int i = Mathf.Min((int)t, Tour.Length - 2);
                var to = Tour[i + 1] - nina.transform.position; to.y = 0;
                nina.ScriptedInput = to.magnitude > 0.3f ? to.normalized : Vector3.zero;
            }
            Grab(Camera.main, Path.Combine(_dir, $"f_{_frame:00000}.png"));
            if (++_frame >= _total) { EditorApplication.update -= Step; EditorApplication.ExitPlaymode(); }
        }

        static void Grab(Camera cam, string path)
        {
            if (!_rt) { _rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); _tex = new Texture2D(W, H, TextureFormat.RGB24, false); }
            var prev = cam.targetTexture; cam.targetTexture = _rt; cam.Render(); cam.targetTexture = prev;
            RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); _tex.Apply(); RenderTexture.active = null;
            File.WriteAllBytes(path, _tex.EncodeToPNG());
        }

        static int Encode(string dir)
        {
            string mp4 = dir + ".mp4";
            var psi = new ProcessStartInfo("ffmpeg", $"-y -framerate {Fps} -i \"{dir}/f_%05d.png\" -c:v libx264 -pix_fmt yuv420p -crf 18 -movflags +faststart \"{mp4}\"") { UseShellExecute = false, RedirectStandardError = true };
            try { using var p = Process.Start(psi); p.StandardError.ReadToEnd(); p.WaitForExit(); Debug.Log($"[Capture] {Path.GetFullPath(mp4)} exit={p.ExitCode}"); return p.ExitCode; }
            catch (Exception e) { Debug.LogError("[Capture] ffmpeg failed: " + e.Message); return 1; }
        }
    }
}
