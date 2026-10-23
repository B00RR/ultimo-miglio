using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;
using UltimoMiglio.UI;
using UltimoMiglio.VFX;

/// <summary>#273: al passo hireNonno la freccia a terra (chevron) e la freccia a bordo schermo devono seguire la stessa tappa della mano
/// (VarcoMarker, #265/#268) e aggirare gli scaffali fino al pad OLDO (Pad_AssumiNonno). Scena reale Ch1_Deposito, varchi del layout (#270).
/// Kaia parte da dove si trova davvero all'inizio del passo (Zona_Bancone, fine della combo) oppure accanto al pad Zaino, sotto gli scaffali (partenza della QA):
/// ogni frame il JOYSTICK SIMULATO (FloatingJoystick.TestValue → NinaController → CharacterController) spinge nella direzione della freccia a terra.
/// Fallisce se Kaia resta ferma (contro gli scaffali) per 3 s, se non arriva sul pad OLDO entro <see cref="TempoMax"/> s, oppure se lungo
/// la strada freccia a terra, mano e freccia a bordo schermo non indicano la stessa direzione sullo schermo (scarto massimo <see cref="Tolleranza"/>°).</summary>
public class GuidaOldo273Test
{
    const float TempoMax = 20f, Tolleranza = 12f;

    [UnityTest, Timeout(180000)]
    public IEnumerator HireNonno_DalBancone_SeguendoLaFrecciaKaiaArrivaAlPadOldo() { yield return Corsa("Bancone"); }

    [UnityTest, Timeout(180000)]
    public IEnumerator HireNonno_DalPadZaino_SeguendoLaFrecciaKaiaArrivaAlPadOldo() { yield return Corsa("Zaino"); }

    IEnumerator Corsa(string partenza)
    {
        PlayerPrefs.DeleteAll(); PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 1); PlayerPrefs.Save();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestBloccaPassi = true; Time.timeScale = 1f;
        NinaController nina = null;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 300 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            var tut = TutorialManager.I; nina = GameRoot.I.Nina; Assert.IsNotNull(nina.Joystick, "joystick mancante");
            var cc = nina.GetComponent<CharacterController>();
            Assert.Greater(VarcoMarker.Tutti.Count, 0, "nessun VarcoMarker in scena (layout #270)");
            for (var s = tut.Current + 1; s <= TutorialManager.Step.HireNonno; s++) { tut.TestEntraNelPasso(s); yield return null; }
            Assert.AreEqual(TutorialManager.Step.HireNonno, tut.Current);
            GameRoot.I.Eco.S.Wallet = 1000; // OLDO pagabile: sotto i 50 LN la freccia torna al giro clienti (ONB-09b)
            var bersaglio = tut.CurrentTarget; Assert.IsNotNull(bersaglio, "nessun bersaglio al passo hireNonno");
            var pad = bersaglio.GetComponent<UpgradePad>(); Assert.IsNotNull(pad, "il bersaglio di hireNonno non è un pad");
            Assert.AreEqual(UpgradeKind.NonnoHire, pad.Kind, "il bersaglio di hireNonno non è il pad OLDO");
            var hints = tut.GetComponent<OnboardingHints>(); Assert.IsNotNull(hints, "OnboardingHints mancante");
            hints.Level1Sec = 0.05f; hints.Level2Sec = 0.1f; hints.Level3Sec = 1e6f; // freccia a bordo / mano subito, niente camera breve

            Vector3 start;
            if (partenza == "Bancone") { var b = Object.FindAnyObjectByType<Bancone>(); Assert.IsNotNull(b, "Bancone mancante"); start = b.transform.position; }
            else
            {   // lato ovest del pad Zaino, fuori dal disco, sotto lo Scaffale C (QA #273: "zona Bancone/Zaino": la linea retta verso OLDO taglia gli scaffali)
                UpgradePad zaino = null; foreach (var p in Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)) if (p.Kind == UpgradeKind.Zaino) zaino = p;
                Assert.IsNotNull(zaino, "Pad_Zaino mancante"); start = zaino.transform.position + new Vector3(-1.3f, 0, 0);
            }
            start.y = nina.transform.position.y;
            Assert.IsFalse(Physics.CheckCapsule(start + Vector3.up * 0.35f, start + Vector3.up * 1.3f, cc.radius, ~0, QueryTriggerInteraction.Ignore), $"partenza {start:F2} dentro un solido");
            Assert.IsTrue(VarcoMarker.Chiuso(start, bersaglio.position, bersaglio), $"da {partenza} la linea retta verso OLDO è libera: il test non distinguerebbe");
            cc.enabled = false; nina.transform.position = start; cc.enabled = true; Physics.SyncTransforms();
            float tw = Time.time; while (Time.time - tw < 0.6f) yield return null; // tappa ricalcolata, freccia in fade-in, livello 2 degli aiuti

            var cam = Camera.main; Assert.IsNotNull(cam, "camera mancante");
            var hud = Object.FindAnyObjectByType<HudController>(); Assert.IsNotNull(hud?.Root, "HUD mancante");
            var edge = hud.Root.Q("edge-arrow"); var head = hud.Root.Q("edge-arrow-head");
            var freccia = UM_TutorialMarker.ArrowTransform; Assert.IsNotNull(freccia, "freccia a terra mancante");
            Assert.IsTrue(freccia.gameObject.activeInHierarchy, $"freccia a terra assente ({UM_TutorialMarker.Stato})");

            // 1) stessa direzione sullo schermo: freccia a terra, mano, freccia a bordo schermo (se il pad è fuori schermo)
            var log = new StringBuilder();
            Vector2 dFreccia = Pannello(cam, nina.transform.position, nina.transform.position + freccia.forward);
            float aMano = Vector2.Angle(dFreccia, hints.DirezioneMano);
            log.AppendLine($"[#273] {partenza}: start {start:F2}, tappa {hints.Tappa:F2}, freccia a terra {dFreccia:F2}, mano {hints.DirezioneMano:F2} (Δ {aMano:0}°)");
            var scarti = new StringBuilder(); float scartoMax = aMano;
            if (aMano >= Tolleranza) scarti.Append($"partenza: freccia a terra {dFreccia:F2} ≠ mano {hints.DirezioneMano:F2} (Δ {aMano:0}°, tappa {hints.Tappa:F2}); ");
            if (edge != null && head != null && edge.style.display.value == DisplayStyle.Flex)
            {
                float ang = head.style.rotate.value.angle.ToDegrees() * Mathf.Deg2Rad; var dBordo = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); // pannello: y in basso
                float aBordo = Vector2.Angle(dBordo, hints.DirezioneMano);
                log.AppendLine($"[#273] {partenza}: freccia a bordo {dBordo:F2} (Δ {aBordo:0}° dalla mano)");
                if (aBordo >= Tolleranza) scarti.Append($"partenza: freccia a bordo schermo {dBordo:F2} ≠ mano {hints.DirezioneMano:F2} (Δ {aBordo:0}°); ");
            }
            else log.AppendLine($"[#273] {partenza}: pad OLDO in schermo, freccia a bordo non mostrata");

            // 2) Kaia segue la freccia a terra col joystick fino al pad OLDO
            var joy = nina.Joystick; float t0 = Time.time, best = float.MaxValue, tBest = Time.time, fatta = 0f; var last = nina.transform.position; int controlli = 0;
            while (!(pad.Occupied || GameRoot.I.Eco.S.NonnoLevel >= 0))
            {
                var p = nina.transform.position; fatta += Flat(p - last).magnitude; last = p;
                var verso = Flat(bersaglio.position - p);
                Vector3 dir = freccia.gameObject.activeInHierarchy ? Flat(freccia.forward) : verso; // sotto 1,2 m la freccia sparisce: resta l'anello
                if (freccia.gameObject.activeInHierarchy && Time.frameCount % 10 == 0)
                {
                    var df = Pannello(cam, p, p + freccia.forward); float a = Vector2.Angle(df, hints.DirezioneMano);
                    if (a >= Tolleranza && a > scartoMax) scarti.Append($"a ({p.x:0.00}; {p.z:0.00}) freccia a terra {df:F2} ≠ mano {hints.DirezioneMano:F2} (Δ {a:0}°, tappa {hints.Tappa:F2}); ");
                    scartoMax = Mathf.Max(scartoMax, a); controlli++;
                }
                float d = verso.magnitude; if (d < best - 0.15f) { best = d; tBest = Time.time; }
                if (Time.time - tBest > 3f) { joy.TestValue = Vector2.zero; Assert.Fail($"{partenza}: Kaia BLOCCATA a ({p.x:0.00}; {p.z:0.00}) seguendo la freccia (dir {dir:F2}, tappa della mano {hints.Tappa:F2}), mancano {d:0.00} m. {log}{scarti}"); }
                if (Time.time - t0 > TempoMax) { joy.TestValue = Vector2.zero; Assert.Fail($"{partenza}: pad OLDO non raggiunto in {TempoMax} s (Kaia a ({p.x:0.00}; {p.z:0.00}), {fatta:0.0} m percorsi). {log}{scarti}"); }
                float yaw = cam.transform.eulerAngles.y;
                var local = Quaternion.Euler(0, -yaw, 0) * (dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.zero);
                joy.TestValue = new Vector2(local.x, local.z);
                yield return null;
            }
            joy.TestValue = Vector2.zero;
            log.AppendLine($"[#273] {partenza}: pad OLDO raggiunto in {Time.time - t0:0.0} s, {fatta:0.0} m percorsi, {controlli} controlli freccia/mano, scarto massimo {scartoMax:0}°");
            Debug.Log(log.ToString());
            Assert.Greater(controlli, 3, "troppo pochi controlli freccia/mano durante il percorso");
            Assert.AreEqual(0, scarti.Length, $"{partenza}: freccia a terra, mano e freccia a bordo schermo non seguono la stessa tappa: {scarti}");
        }
        finally
        {
            if (nina && nina.Joystick) nina.Joystick.TestValue = null;
            TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; Time.timeScale = 1f;
        }
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    /// <summary>Direzione sullo schermo da a verso b, in coordinate del pannello (x a destra, y in basso), come OnboardingHints.DirezioneMano.</summary>
    static Vector2 Pannello(Camera cam, Vector3 a, Vector3 b) { Vector3 sa = cam.WorldToScreenPoint(a), sb = cam.WorldToScreenPoint(b); return new Vector2(sb.x - sa.x, -(sb.y - sa.y)).normalized; }
}
