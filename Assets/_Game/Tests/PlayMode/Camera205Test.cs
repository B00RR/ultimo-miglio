using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UltimoMiglio.Gameplay;
using UltimoMiglio.UI;
using UltimoMiglio.VFX;

/// <summary>#205 telecamera regolabile: joystick relativo allo schermo con la camera ruotata, limiti del tutorial,
/// frecce/anelli/freccia a bordo schermo corretti con la camera ruotata, catture portrait (solo con UM_CATTURA205=&lt;cartella&gt;).</summary>
public class Camera205Test
{
    static void ClearCamPrefs() { foreach (var k in new[] { CameraView.PrefZoom, CameraView.PrefRot, CameraView.PrefTilt, CameraView.PrefVer, CameraView.LegacyPrefZoom }) PlayerPrefs.DeleteKey(k); PlayerPrefs.Save(); }

    static IEnumerator Load(bool freePlay)
    {
        AnimFacingTest.ResetGameKeys(); Time.timeScale = 1f;
        TutorialManager.TestSenzaComparse = true; TutorialManager.TestAccelera = true;
        if (freePlay) { PlayerPrefs.SetInt(TutorialManager.PrefKey, (int)TutorialManager.Step.FreePlay); PlayerPrefs.SetInt(TutorialManager.PrefVer, TutorialManager.Versione); PlayerPrefs.Save(); }
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 120 && (!GameRoot.Ready || !GameRoot.I.Nina || TutorialManager.I == null || !Object.FindAnyObjectByType<DepotCameraFit>()); i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
        yield return null; yield return null;
    }

    static void Cleanup()
    {
        DepotCameraFit.TestIgnoraTutorial = false; DepotCameraFit.TestScreenW = DepotCameraFit.TestScreenH = 0;
        TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false;
        var nina = GameRoot.I ? GameRoot.I.Nina : null; if (nina && nina.Joystick) nina.Joystick.TestValue = null;
        CameraView.Tutorial = false; CameraView.Pinching = false; CameraView.SetForTest(1f, 0f, CameraView.DefaultTilt); ClearCamPrefs(); Time.timeScale = 1f;
    }

    static void Teleport(NinaController nina, Vector3 p)
    {
        var cc = nina.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
        nina.transform.position = p; if (cc) cc.enabled = true; Physics.SyncTransforms();
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }

    [UnityTest, Timeout(120000)]
    public IEnumerator JoystickConCameraRuotata45_NinaVaNellaDirezioneDelloSchermo()
    {
        ClearCamPrefs();
        try
        {
            yield return Load(freePlay: true);
            var nina = GameRoot.I.Nina; var fit = Object.FindAnyObjectByType<DepotCameraFit>(); var cam = Camera.main;
            Assert.IsNotNull(nina.Joystick, "joystick mancante"); Assert.IsFalse(CameraView.Tutorial, "atteso freePlay");
            CameraView.SetForTest(1f, 45f, CameraView.DefaultTilt); fit.SnapView(); yield return null; yield return null;
            Assert.AreEqual(Mathf.Repeat(fit.BaseYaw + 45f, 360f), Mathf.Repeat(cam.transform.eulerAngles.y, 360f), 0.5f, "la camera non è ruotata di +45°");

            var inputs = new[] { Vector2.up, Vector2.right, Vector2.left, Vector2.down };
            foreach (var input in inputs)
            {
                nina.Joystick.TestValue = Vector2.zero; Teleport(nina, NinaController.SafeSpawn); yield return null; yield return null;
                var p0 = nina.transform.position;
                nina.Joystick.TestValue = input;
                float t0 = Time.time; while (Time.time - t0 < 0.35f) yield return null;
                nina.Joystick.TestValue = Vector2.zero;
                var d = nina.transform.position - p0; d.y = 0;
                var right = Flat(cam.transform.right); var up = Flat(cam.transform.forward); // "su" sullo schermo = avanti della camera proiettato a terra
                var want = (right * input.x + up * input.y).normalized;
                Debug.Log($"[Camera205] input={input} d={d} want={want} yaw={cam.transform.eulerAngles.y:0.0}");
                Assert.Greater(d.magnitude, 0.3f, $"input {input}: Nina non si è mossa");
                Assert.Greater(Vector3.Dot(d.normalized, want), 0.95f, $"input {input}: direzione {d.normalized} diversa da quella dello schermo {want}");
                // controllo anche in spazio schermo: lo spostamento proiettato va nel verso dell'input
                var s0 = cam.WorldToScreenPoint(p0); var s1 = cam.WorldToScreenPoint(p0 + d);
                Assert.Greater(Vector2.Dot(((Vector2)(s1 - s0)).normalized, input), 0.9f, $"input {input}: sullo schermo Nina non va nel verso del joystick");
            }
        }
        finally { Cleanup(); }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator Tutorial_ZoomTra085e12_RotazioneEInclinazioneBloccate()
    {
        ClearCamPrefs();
        PlayerPrefs.SetFloat(CameraView.PrefZoom, 1.4f); PlayerPrefs.SetFloat(CameraView.PrefRot, 45f); PlayerPrefs.SetFloat(CameraView.PrefTilt, 60f); PlayerPrefs.SetInt(CameraView.PrefVer, 1); PlayerPrefs.Save();
        try
        {
            yield return Load(freePlay: false); TutorialManager.TestBloccaPassi = true;
            var tm = TutorialManager.I; var fit = Object.FindAnyObjectByType<DepotCameraFit>(); var cam = Camera.main;
            Assert.Less(tm.Current, TutorialManager.Step.Fine, "atteso tutorial in corso");
            Assert.IsTrue(CameraView.Tutorial);
            float t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < 0.4f) yield return null;
            Assert.AreEqual(1.2f, fit.AppliedZoom, 1e-3f, "zoom nel tutorial oltre 1,2");
            Assert.AreEqual(0f, fit.AppliedRot, 1e-3f, "rotazione non bloccata nel tutorial");
            Assert.AreEqual(CameraView.DefaultTilt, fit.AppliedTilt, 1e-3f, "inclinazione non bloccata nel tutorial");
            Assert.AreEqual(Mathf.Repeat(fit.BaseYaw, 360f), Mathf.Repeat(cam.transform.eulerAngles.y, 360f), 0.5f);
            CameraView.SetRot(-30f); CameraView.SetTilt(35f); CameraView.SetZoom(0.7f);
            Assert.AreEqual(45f, CameraView.Rot, 1e-4f, "lo slider rotazione non deve agire nel tutorial");
            Assert.AreEqual(60f, CameraView.Tilt, 1e-4f, "lo slider inclinazione non deve agire nel tutorial");
            Assert.AreEqual(0.85f, CameraView.Zoom, 1e-4f, "zoom minimo nel tutorial 0,85");
            yield return null; yield return null;
            t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < 0.4f) yield return null;
            Assert.AreEqual(0.85f, fit.AppliedZoom, 1e-3f);

            tm.TestEntraNelPasso(TutorialManager.Step.FreePlay);
            t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < 0.5f) yield return null;
            Assert.IsFalse(CameraView.Tutorial, "dal freePlay tutto si sblocca");
            Assert.AreEqual(45f, fit.AppliedRot, 0.05f, "dal freePlay torna la rotazione del giocatore");
            Assert.AreEqual(60f, fit.AppliedTilt, 0.05f, "dal freePlay torna l'inclinazione del giocatore");
        }
        finally { Cleanup(); }
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator FrecceAnelliEFrecciaABordo_CorretteConCameraRuotata()
    {
        ClearCamPrefs();
        try
        {
            yield return Load(freePlay: false);
            TutorialManager.TestBloccaPassi = true; DepotCameraFit.TestIgnoraTutorial = true;
            var tm = TutorialManager.I; var nina = GameRoot.I.Nina; var fit = Object.FindAnyObjectByType<DepotCameraFit>(); var cam = Camera.main;
            var hud = Object.FindAnyObjectByType<HudController>(); Assert.IsNotNull(hud?.Root, "HUD mancante");
            var edge = hud.Root.Q("edge-arrow"); var head = hud.Root.Q("edge-arrow-head");
            var edgeArrow = Object.FindAnyObjectByType<EdgeArrow>(FindObjectsInactive.Include); Assert.IsNotNull(edgeArrow, "EdgeArrow mancante");
            int frecce = 0, bordo = 0;
            foreach (float rot in new[] { -45f, 45f })
            {
                CameraView.SetForTest(0.7f, rot, rot < 0 ? 35f : 60f); fit.SnapView(); yield return null;
                for (var s = TutorialManager.Step.Move; s <= TutorialManager.Step.WallPad; s++)
                {
                    if (!TutorialManager.HasWorldTarget(s)) continue;
                    tm.TestEntraNelPasso(s);
                    float t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < 0.5f) yield return null;
                    var g = tm.GuidaDi(s); Assert.IsNotNull(g.Target, $"passo {s}: bersaglio nullo");
                    Assert.AreEqual(rot, fit.AppliedRot, 0.05f, "camera non ruotata");
                    Assert.IsTrue(UM_TutorialMarker.Visible, $"rot {rot} passo {s}: anello non visibile");
                    Assert.AreSame(tm.CurrentTarget, UM_TutorialMarker.Target, $"rot {rot} passo {s}: anello non sul bersaglio");
                    var d = g.Target.position - nina.transform.position; d.y = 0;
                    var arrow = UM_TutorialMarker.ArrowTransform;
                    if (g.Freccia && d.magnitude > g.FrecciaOltre + 0.5f && arrow && arrow.gameObject.activeInHierarchy)
                    {
                        var dv = tm.Via() - nina.transform.position; dv.y = 0; if (dv.sqrMagnitude < 1e-4f) dv = d; // #273: verso la tappa della guida
                        Assert.Greater(Vector3.Dot(arrow.forward, dv.normalized), 0.95f, $"rot {rot} passo {s}: freccia a terra non punta alla tappa della guida");
                        frecce++;
                    }
                    // freccia a bordo: Nina lontana dal bersaglio, così il bersaglio esce dallo schermo (zoom 0,7)
                    // #224: dei due angoli si prende il più lontano dal bersaglio (con i pad in colonna a x −2 l'angolo SE lasciava Insegna dentro lo schermo)
                    var se = new Vector3(6f, nina.transform.position.y, -9f); var nw = new Vector3(-8f, nina.transform.position.y, 6f);
                    var far = Vector3.Distance(se, g.Target.position) >= Vector3.Distance(nw, g.Target.position) ? se : nw;
                    Teleport(nina, far); edgeArrow.SetTarget(g.Target, s.ToString());
                    t0 = Time.unscaledTime; while (Time.unscaledTime - t0 < 0.4f) yield return null;
                    if (edge != null && head != null && edge.style.display.value == DisplayStyle.Flex)
                    {
                        // la freccia a bordo deve puntare dove il bersaglio appare sullo schermo con la camera ruotata
                        Vector2 sn = cam.WorldToScreenPoint(nina.transform.position), st = cam.WorldToScreenPoint(g.Target.position);
                        var dir = st - sn; float want = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
                        float got = head.style.rotate.value.angle.ToDegrees();
                        float diff = Mathf.Abs(Mathf.DeltaAngle(want, got));
                        Debug.Log($"[Camera205] rot {rot} passo {s}: freccia a bordo {got:0} attesa {want:0} (Δ {diff:0})");
                        Assert.Less(diff, 35f, $"rot {rot} passo {s}: freccia a bordo {got:0}° ≠ direzione sullo schermo {want:0}°");
                        bordo++;
                    }
                }
            }
            Assert.Greater(frecce, 3, "troppo poche frecce a terra verificate con la camera ruotata");
            Assert.Greater(bordo, 3, "troppo poche frecce a bordo schermo verificate con la camera ruotata");
            Debug.Log($"[Camera205] frecce a terra verificate {frecce}, frecce a bordo {bordo}");
        }
        finally { Cleanup(); }
    }

    /// <summary>Catture PNG portrait 1080x1920 nelle tre configurazioni della #205. Solo se la variabile d'ambiente UM_CATTURA205 indica la cartella (fuori dal repo).</summary>
    [UnityTest, Timeout(120000)]
    public IEnumerator Cattura205_TreConfigurazioni()
    {
        string dir = System.Environment.GetEnvironmentVariable("UM_CATTURA205");
        if (string.IsNullOrEmpty(dir)) { Assert.Ignore("UM_CATTURA205 non impostata: catture saltate"); yield break; }
        ClearCamPrefs(); Directory.CreateDirectory(dir);
        const int W = 1080, H = 1920;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32); var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        try
        {
            DepotCameraFit.TestScreenW = W; DepotCameraFit.TestScreenH = H;
            yield return Load(freePlay: true);
            var fit = Object.FindAnyObjectByType<DepotCameraFit>(); var cam = Camera.main;
            var cfg = new (string name, float z, float r, float t)[] { ("default", 1f, 0f, CameraView.DefaultTilt), ("zoom070_rot-45_tilt35", 0.7f, -45f, 35f), ("zoom140_rot+45_tilt60", 1.4f, 45f, 60f) };
            foreach (var c in cfg)
            {
                CameraView.SetForTest(c.z, c.r, c.t); fit.SnapView();
                for (int i = 0; i < 5; i++) yield return null;
                var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                string path = Path.Combine(dir, $"cattura205_{c.name}_{W}x{H}.png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Debug.Log($"[Camera205] cattura {path} zoom={fit.AppliedZoom:0.00} rot={fit.AppliedRot:0} tilt={fit.AppliedTilt:0} ortho={fit.OrthoSize:0.00}");
                Assert.IsTrue(File.Exists(path));
            }
        }
        finally { Cleanup(); Object.Destroy(rt); Object.Destroy(tex); }
    }
}
