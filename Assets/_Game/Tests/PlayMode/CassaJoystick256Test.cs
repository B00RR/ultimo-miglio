using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;
using UltimoMiglio.UI;

/// <summary>#256 (QA LIVE 8e33a40, #66): scena reale Ch1_Deposito, partita nuova, tutorial graduale. Kaia parte dallo spawn e va alla Cassa
/// (passo cassaPass) solo con il joystick: <see cref="FloatingJoystick.TestValue"/> → WorldDirection (yaw della camera) → NinaController →
/// CharacterController reale. Niente teletrasporto: le tappe sono solo bersagli dello stick. Percorso del QA: varco ovest, lato Bancone,
/// arco a destra del Bancone (env_wall_arch_4m), poi a nord e a sinistra verso Zona_Cassa. Se Kaia resta ferma il test fallisce
/// con la posizione di blocco, la distanza dal centro di Zona_Cassa e i collider che la toccano.</summary>
public class CassaJoystick256Test
{
    static readonly FieldInfo FCam = typeof(FloatingJoystick).GetField("cameraTransform", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo FYaw = typeof(FloatingJoystick).GetField("fixedYaw", BindingFlags.NonPublic | BindingFlags.Instance);
    const float FermaSec = 2.5f, FermaMin = 0.1f; // blocco: meno di 10 cm in 2,5 s con lo stick a fondo
    readonly Collider[] _tocchi = new Collider[16]; // preallocato, usato solo al blocco

    // Tappe del QA aggiornate al layout #256/#262 (Cassa a nord dell'arco da 4 m del Bancone, Zona_Cassa ≈ (3,42; −2,84)).
    // Lo stick punta verso ciascuna, la fisica decide dove arriva Kaia. Su 8e33a40 la rotta QA (TappeArcoEst) si incastrava a (6,35; −4,13).
    // Rotta del QA: varco ovest, lato Bancone, arco a destra del Bancone, poi a nord e a ovest verso la Cassa.
    static readonly Vector3[] TappeArcoEst =
    {
        new Vector3(-0.6f, 0, -2.0f),  // dallo spawn verso il varco ovest
        new Vector3(-0.6f, 0, -5.6f),  // lato Bancone
        new Vector3(1.3f, 0, -5.6f),
        new Vector3(3.4f, 0, -5.12f),  // fra il set e Oma
        new Vector3(6.34f, 0, -5.12f), // attraverso l'arco a destra del Bancone (env_wall_arch_4m)
        new Vector3(6.34f, 0, -3.3f),  // a nord, nella striscia est (ora libera: 1,39 m)
    };
    // Rotta nuova (quella indicata dalla guida): lato Bancone, poi dritto a nord sotto l'arco da 4 m.
    static readonly Vector3[] TappeArcoBancone =
    {
        new Vector3(-0.6f, 0, -2.0f),
        new Vector3(-0.6f, 0, -5.6f),
        new Vector3(1.3f, 0, -5.6f),
        new Vector3(3.42f, 0, -5.0f),  // davanti all'arco del Bancone
        new Vector3(3.42f, 0, -4.19f), // sotto l'arco (Varco_ArcoBancone, 2,4 m)
    };

    [UnityTest, Timeout(180000)]
    public IEnumerator DalloSpawnAllaCassa_ColJoystick_PassoCassaPass() { yield return Corsa(TappeArcoEst); }

    [UnityTest, Timeout(180000)]
    public IEnumerator DalloSpawnAllaCassa_SottoArcoBancone_ColJoystick() { yield return Corsa(TappeArcoBancone); }

    IEnumerator Corsa(Vector3[] Tappe)
    {
        PlayerPrefs.DeleteAll(); PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 1); PlayerPrefs.Save();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = false; TutorialManager.TestBloccaPassi = true; TutorialManager.TestAccelera = true; Time.timeScale = 1f;
        FloatingJoystick joy = null;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 300 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            yield return null;
            var tut = TutorialManager.I; var nina = GameRoot.I.Nina;
            joy = nina.Joystick; Assert.IsNotNull(joy, "Kaia senza FloatingJoystick");
            Assert.IsNotNull(nina.GetComponent<CharacterController>(), "Kaia senza CharacterController");
            nina.ScriptedInput = Vector3.zero; // solo il percorso del joystick
            var cassa = GameObject.Find("Zona_Cassa"); Assert.IsNotNull(cassa, "Zona_Cassa non in scena");
            var centro = cassa.transform.position;
            for (var s = tut.Current + 1; s <= TutorialManager.Step.CassaPass; s++) { tut.TestEntraNelPasso(s); yield return null; }
            yield return Attendi(0.5f);
            TutorialManager.TestBloccaPassi = false;
            Debug.Log($"[#256] partenza kaia={P(nina.transform.position)} cassa={P(centro)} passo={tut.Current}");

            var camT = FCam?.GetValue(joy) as Transform; float yawFisso = FYaw != null ? (float)FYaw.GetValue(joy) : 45f;
            for (int i = 0; i <= Tappe.Length && tut.Current == TutorialManager.Step.CassaPass; i++)
            {
                bool ultima = i == Tappe.Length; var meta = ultima ? centro : Tappe[i];
                string nome = ultima ? "centro di Zona_Cassa" : "tappa " + (i + 1) + " " + P(meta);
                float tFermo = Time.time; Vector3 pFermo = nina.transform.position; float t0 = Time.time;
                while (tut.Current == TutorialManager.Step.CassaPass)
                {
                    var d = meta - nina.transform.position; d.y = 0;
                    if (!ultima && d.magnitude < 0.2f) break;
                    float yaw = camT ? camT.eulerAngles.y : yawFisso;
                    var loc = Quaternion.Euler(0, -yaw, 0) * d.normalized; // inverso di WorldDirection
                    joy.TestValue = ultima && d.magnitude < 0.15f ? Vector2.zero : new Vector2(loc.x, loc.z);
                    yield return null;
                    var p = nina.transform.position;
                    if ((p - pFermo).sqrMagnitude > FermaMin * FermaMin) { pFermo = p; tFermo = Time.time; }
                    else if (Time.time - tFermo > FermaSec) Assert.Fail(Blocco(nina, centro, nome));
                    if (Time.time - t0 > 30f) Assert.Fail("[#256] tempo scaduto verso " + nome + ". " + Blocco(nina, centro, nome));
                }
            }
            joy.TestValue = Vector2.zero;
            for (int i = 0; i < 60 && tut.Current == TutorialManager.Step.CassaPass; i++) yield return null;
            Assert.AreEqual(TutorialManager.Step.DebtCard, tut.Current, "[#256] Kaia sulla Cassa ma cassaPass non chiuso. " + Blocco(nina, centro, "centro di Zona_Cassa"));
        }
        finally
        {
            if (joy) joy.TestValue = null;
            TutorialManager.TestBloccaPassi = false; TutorialManager.TestAccelera = false; Time.timeScale = 1f;
        }
    }

    string Blocco(NinaController nina, Vector3 centro, string verso)
    {
        var p = nina.transform.position; var d = centro - p; d.y = 0;
        var cc = nina.GetComponent<CharacterController>();
        float r = cc.radius + cc.skinWidth + 0.05f;
        int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * r, p + Vector3.up * (cc.height - r), r, _tocchi, ~0, QueryTriggerInteraction.Ignore);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            var c = _tocchi[i]; if (c.transform == nina.transform) continue;
            var b = c.bounds; sb.Append(sb.Length > 0 ? ", " : "").Append(c.name).Append(" [x ").Append(b.min.x.ToString("0.00")).Append("..").Append(b.max.x.ToString("0.00"))
                .Append(", z ").Append(b.min.z.ToString("0.00")).Append("..").Append(b.max.z.ToString("0.00")).Append("]");
        }
        return $"[#256] Kaia bloccata in {P(p)} andando verso {verso}: distanza dal centro di Zona_Cassa {P(centro)} = {d.magnitude:0.00} m. Collider a contatto: {(sb.Length > 0 ? sb.ToString() : "nessuno")}.";
    }

    static string P(Vector3 v) => $"(x {v.x:0.00}, z {v.z:0.00})";
    static IEnumerator Attendi(float s) { float t = Time.time; while (Time.time - t < s) yield return null; }
}
