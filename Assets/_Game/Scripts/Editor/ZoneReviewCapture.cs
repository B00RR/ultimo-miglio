using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltimoMiglio.Gameplay;

namespace UltimoMiglio.EditorTools
{
    /// <summary>#187: screenshot di revisione delle zone a terra (vista dall'alto ortografica + 3/4 di gioco), in edit mode.
    ///   xvfb-run Unity -batchmode -projectPath . -executeMethod UltimoMiglio.EditorTools.ZoneReviewCapture.Capture -quit [-captureDir docs/review/187]</summary>
    public static class ZoneReviewCapture
    {
        public static void Capture()
        {
            var args = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(args, "-captureDir");
            string dir = i >= 0 && i + 1 < args.Length ? args[i + 1] : "docs/review/187";
            Directory.CreateDirectory(dir);
            EditorSceneManager.OpenScene(SliceSceneBuilder.ScenePath);
            // come InteractionPoint.Start in gioco: i quadrati legacy delle ProximityZone restano invisibili
            foreach (var z in Object.FindObjectsByType<ProximityZone>(FindObjectsSortMode.None))
                if (z.Ring) { var r = z.Ring.GetComponent<Renderer>(); if (r) r.enabled = false; }
            foreach (var zr in Object.FindObjectsByType<UltimoMiglio.UI.ZoneFillRing>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var r = zr.GetComponent<Renderer>(); if (r) r.enabled = false; }
            var cam = Camera.main;
            cam.orthographic = true;
            // 1) dall'alto, tutto il deposito
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); cam.transform.position = new Vector3(-0.5f, 30f, -1.5f); cam.orthographicSize = 10.5f;
            Grab(cam, Path.Combine(dir, "zone_187_dallalto.png"), 1600, 1600);
            Grab(cam, Path.Combine(dir, "zone_187_dallalto.png"), 1600, 1600); // il primo frame batch può uscire sporco
            // 2) vista di gioco 3/4 (come CameraRig) centrata sui pad
            cam.transform.rotation = Quaternion.Euler(35.3f, 45f, 0f); cam.transform.position = new Vector3(0f, 0f, -1f) - cam.transform.forward * 30f; cam.orthographicSize = 7.5f;
            Grab(cam, Path.Combine(dir, "zone_187_vista_gioco.png"), 1080, 1920);
            Debug.Log("[#187] screenshot in " + Path.GetFullPath(dir));
        }

        static void Grab(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32); var prev = cam.targetTexture;
            cam.targetTexture = rt; cam.aspect = (float)w / h; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = prev; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }
}
