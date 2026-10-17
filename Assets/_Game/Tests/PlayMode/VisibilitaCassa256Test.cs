using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;
using UltimoMiglio.UI;

/// <summary>#256 (condizione GD, review #270): con Kaia sull'anello della Cassa e la pila in mano, a 390x844 l'insegna dell'arco del Bancone
/// (e l'architrave dell'arco) può coprire al massimo una parte della testa: torso, braccia, pila in mano e anello della Cassa restano visibili. Raggi dalla camera di gioco
/// ai punti del corpo/pila/anello, contro i bounds (conservativi) dei renderer dell'insegna. Con UM_CATTURA256=&lt;cartella&gt; salva anche le catture.</summary>
public class VisibilitaCassa256Test
{
    static void Teleport(NinaController nina, Vector3 p) { var cc = nina.GetComponent<CharacterController>(); cc.enabled = false; nina.transform.position = p; cc.enabled = true; Physics.SyncTransforms(); }

    [UnityTest, Timeout(180000)]
    public IEnumerator SullaCassa_InsegnaNonCopreTorsoPilaEAnello()
    {
        AnimFacingTest.ResetGameKeys(); Time.timeScale = 1f; TutorialManager.TestSenzaComparse = true;
        PlayerPrefs.SetInt(TutorialManager.PrefKey, (int)TutorialManager.Step.FreePlay); PlayerPrefs.SetInt(TutorialManager.PrefVer, TutorialManager.Versione); PlayerPrefs.Save();
        DepotCameraFit.TestScreenW = 390; DepotCameraFit.TestScreenH = 844;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int k = 0; k < 120 && (!GameRoot.Ready || !GameRoot.I.Nina || !Object.FindAnyObjectByType<DepotCameraFit>()); k++) yield return null;
            var nina = GameRoot.I.Nina; var fit = Object.FindAnyObjectByType<DepotCameraFit>(); var cam = Camera.main;
            CameraView.SetForTest(1f, 0f, CameraView.DefaultTilt);
            var cassa = Object.FindAnyObjectByType<CassaZone>(); Assert.IsNotNull(cassa, "Zona_Cassa mancante");
            var c0 = cassa.transform.position;
            Teleport(nina, new Vector3(c0.x, nina.transform.position.y, c0.z)); fit.SnapView();
            for (int i = 0; i < 3; i++) nina.Stack.TryPush((ParcelColor)(i % 3));
            float t0 = Time.time; while (Time.time - t0 < 1.5f) yield return null;
            Assert.Greater(nina.Stack.Count, 0, "pila vuota");

            // insegna dell'arco del Bancone: la più vicina alla Cassa
            Transform insegna = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name.StartsWith("prop_sign_sunn_depot") && (!insegna || Flat(t.position - c0) < Flat(insegna.position - c0))) insegna = t;
            Assert.IsNotNull(insegna, "insegna del Bancone non trovata");
            var rends = insegna.GetComponentsInChildren<Renderer>();
            var log = new StringBuilder($"insegna {insegna.name} a ({insegna.position.x:0.00}; {insegna.position.y:0.00}; {insegna.position.z:0.00})");
            foreach (var r in rends) log.Append($" [{r.name} min {r.bounds.min:F2} max {r.bounds.max:F2}]");
            log.Append($"; camera {cam.transform.position:F2} rot {cam.transform.eulerAngles:F1}");

            var punti = new List<(string, Vector3)>();
            var p = nina.transform.position;
            foreach (var h in new[] { 0.3f, 0.7f, 1.0f, 1.25f }) punti.Add(($"corpo {h:0.00} m", p + Vector3.up * h)); // 1,25 = spalle: la copertura non scende sotto la testa
            var fwd = nina.transform.forward; fwd.y = 0; fwd.Normalize();
            punti.Add(("braccio sx", p + Vector3.up * 1.0f - nina.transform.right * 0.25f)); punti.Add(("braccio dx", p + Vector3.up * 1.0f + nina.transform.right * 0.25f));
            var anchor = nina.Stack.Anchor ? nina.Stack.Anchor : nina.transform;
            for (int i = 0; i < nina.Stack.Count; i++) punti.Add(($"pacco {i + 1}", anchor.position + Vector3.up * (nina.Stack.Height * (i + 0.5f))));
            var ring = new Vector3(c0.x, 0.05f, c0.z);
            punti.Add(("anello centro", ring));
            for (int a = 0; a < 8; a++) punti.Add(($"anello bordo {a}", ring + Quaternion.Euler(0, a * 45f, 0) * Vector3.forward * 0.5f));

            var coperti = new List<string>();
            foreach (var (nome, q) in punti)
            {
                var o = cam.transform.position; var d = q - o; float len = d.magnitude; var ray = new Ray(o, d / len);
                string da = null;
                foreach (var r in rends) if (r.enabled && r.bounds.IntersectRay(ray, out float dist) && dist < len) { da = r.name; break; }
                // anche gli altri solidi (architrave dell'arco, muri): collider veri, esclusi Kaia, la sua pila e i trigger
                if (da == null)
                    foreach (var h in Physics.RaycastAll(ray, len - 0.05f, ~0, QueryTriggerInteraction.Ignore))
                        if (!h.collider.transform.IsChildOf(nina.transform) && !(nina.Stack && h.collider.transform.IsChildOf(nina.Stack.transform))) { da = h.collider.name; break; }
                if (da != null) coperti.Add($"{nome} ({q.x:0.00}; {q.y:0.00}; {q.z:0.00}) da {da}");
                var sp = cam.WorldToScreenPoint(q); log.Append($"\n  {nome}: schermo ({sp.x / cam.pixelWidth * 390f:0}; {844f - sp.y / cam.pixelHeight * 844f:0})");
            }
            Debug.Log("[#256-visibilità] " + log);

            var dir = System.Environment.GetEnvironmentVariable("UM_CATTURA256");
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                const int S = 2; var rt = new RenderTexture(390 * S, 844 * S, 24); var tex = new Texture2D(390 * S, 844 * S, TextureFormat.RGB24, false);
                Shot(cam, rt, tex, Path.Combine(dir, "256_cassa_390x844.png"));
                var go = new GameObject("CamTop"); var top = go.AddComponent<Camera>(); top.orthographic = true; top.orthographicSize = 10.5f; top.transform.position = new Vector3(-0.5f, 30, -1f); top.transform.rotation = Quaternion.Euler(90, 0, 0);
                top.clearFlags = CameraClearFlags.SolidColor; top.backgroundColor = Color.black; top.farClipPlane = 60;
                var rt2 = new RenderTexture(1600, 1600, 24); var tex2 = new Texture2D(1600, 1600, TextureFormat.RGB24, false);
                Shot(top, rt2, tex2, Path.Combine(dir, "256_dallalto.png")); Object.Destroy(go);
                File.WriteAllText(Path.Combine(dir, "visibilita.txt"), log + "\ncoperti: " + string.Join("; ", coperti));
                // altre catture di revisione (stessa camera di gioco)
                var ban = GameObject.Find("Zona_Bancone").transform.position; var ada = GameObject.Find("NonnaAda").transform.position; var safe = GameObject.Find(TutorialManager.SafeObjectName).transform.position;
                foreach (var (n, q) in new (string, Vector3)[] { ("bancone", ban), ("arco_bancone", new Vector3(3.42f, 0, -4.19f)), ("ada", ada + new Vector3(0.9f, 0, 0)), ("striscia_est", new Vector3(6.665f, 0, -4.34f)), ("cassaforte", safe + new Vector3(-1.2f, 0, 0)) })
                {
                    Teleport(nina, q + Vector3.up * 0.05f); fit.SnapView(); float t1 = Time.time; while (Time.time - t1 < 1.5f) yield return null;
                    Shot(cam, rt, tex, Path.Combine(dir, $"256_{n}_390x844.png"));
                }
            }
            Assert.IsEmpty(coperti, "insegna o arco coprono più della testa di Kaia sulla Cassa: " + string.Join("; ", coperti));
        }
        finally { DepotCameraFit.TestScreenW = DepotCameraFit.TestScreenH = 0; TutorialManager.TestSenzaComparse = false; }
    }

    static float Flat(Vector3 v) { v.y = 0; return v.magnitude; }
    static void Shot(Camera cam, RenderTexture rt, Texture2D tex, string path)
    {
        var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply(); RenderTexture.active = null; cam.targetTexture = prev;
        File.WriteAllBytes(path, tex.EncodeToPNG());
    }
}
