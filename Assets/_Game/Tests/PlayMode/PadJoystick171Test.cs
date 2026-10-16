using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;
using UltimoMiglio.UI;

/// <summary>#171 (QA LIVE 8e33a40): scena reale Ch1_Deposito, partita nuova, tutorial graduale. Kaia parte dallo spawn e si muove solo col joystick
/// (<see cref="FloatingJoystick.TestValue"/> → WorldDirection → NinaController → CharacterController reale), come in CassaJoystick256Test.
/// Per ogni pad del rapporto QA (OLDO da sud-est, Zaino da sud, Scarpe da sud) Kaia entra dal lato indicato, lo stick si rilascia quando i piedi
/// sono al 95% del raggio del disco e il pad deve caricare entro 1,5 s. Se non carica il messaggio dice di chi è il problema:
/// layout (Kaia fermata da un collider prima del disco), UpgradePad (piedi nel disco ma pad non occupato), ProximityZone (occupato ma un'altra zona attiva).</summary>
public class PadJoystick171Test
{
    static readonly FieldInfo FCam = typeof(FloatingJoystick).GetField("cameraTransform", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo FYaw = typeof(FloatingJoystick).GetField("fixedYaw", BindingFlags.NonPublic | BindingFlags.Instance);
    const float Bordo = 0.95f, FermaSec = 2.5f, FermaMin = 0.1f;
    readonly Collider[] _tocchi = new Collider[16];
    readonly System.Text.StringBuilder _errori = new System.Text.StringBuilder();
    FloatingJoystick _joy; Transform _camT; float _yaw;

    [UnityTest, Timeout(240000)]
    public IEnumerator PadSuiBordiSudESudEst_ColJoystick_SiAttivano()
    {
        PlayerPrefs.DeleteAll(); PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 1); PlayerPrefs.Save();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = false; TutorialManager.TestBloccaPassi = true; TutorialManager.TestAccelera = true; Time.timeScale = 1f;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 300 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            yield return null;
            var nina = GameRoot.I.Nina; _joy = nina.Joystick; Assert.IsNotNull(_joy, "Kaia senza FloatingJoystick");
            Assert.IsNotNull(nina.GetComponent<CharacterController>(), "Kaia senza CharacterController");
            nina.ScriptedInput = Vector3.zero;
            _camT = FCam?.GetValue(_joy) as Transform; _yaw = FYaw != null ? (float)FYaw.GetValue(_joy) : 45f;
            Assert.Less(GameRoot.I.Eco.S.NonnoLevel, 0, "partita non nuova: NONNO già assunto");

            yield return Porta(TutorialManager.Step.HireNonno); // Zaino e OLDO visibili
            yield return Prova(Pad("Pad_AssumiNonno"), new Vector3(1, 0, -1).normalized, "SE", null);
            yield return Prova(Pad("Pad_Zaino"), Vector3.back, "S", new[] { new Vector3(0.3f, 0, 3.5f), new Vector3(0.3f, 0, -2.8f) });
            yield return Porta(TutorialManager.Step.PadNuovi); // Scarpe visibile
            yield return Prova(Pad("Pad_Scarpe"), Vector3.back, "S", new[] { new Vector3(2.55f, 0, -2.8f) });

            if (_errori.Length > 0) Assert.Fail(_errori.ToString());
        }
        finally
        {
            if (_joy) _joy.TestValue = null;
            TutorialManager.TestBloccaPassi = false; TutorialManager.TestAccelera = false; Time.timeScale = 1f;
        }
    }

    static UpgradePad Pad(string nome) { var g = GameObject.Find(nome); Assert.IsNotNull(g, nome + " non in scena"); var p = g.GetComponent<UpgradePad>(); Assert.IsNotNull(p, nome + " senza UpgradePad"); return p; }

    static IEnumerator Porta(TutorialManager.Step fino)
    {
        for (var s = TutorialManager.I.Current + 1; s <= fino; s++) { TutorialManager.I.TestEntraNelPasso(s); yield return null; }
        float t = Time.time; while (Time.time - t < 0.6f) yield return null; // comparsa dei pad finita
    }

    IEnumerator Prova(UpgradePad pad, Vector3 lato, string nomeLato, Vector3[] tappe)
    {
        var nina = GameRoot.I.Nina; var eco = GameRoot.I.Eco;
        if (!pad.gameObject.activeInHierarchy || double.IsInfinity(eco.Cost(pad.Kind))) { _errori.AppendLine($"[#171] {pad.name}: pad non disponibile al passo {TutorialManager.I.Current}."); yield break; }
        Vector3 c = pad.DiscCenter; float r = pad.DiscRadius;
        var partenza = c + lato * (r + 0.9f);
        string esito = null;
        if (tappe != null) foreach (var t in tappe) { yield return Vai(nina, t, 0.2f, pad.name + " tappa", v => esito = v); if (esito != null) break; }
        if (esito == null) yield return Vai(nina, partenza, 0.15f, pad.name + " partenza " + nomeLato, v => esito = v);
        if (esito != null) { _errori.AppendLine(esito); yield break; }
        _joy.TestValue = Vector2.zero; yield return Attendi(0.4f);

        eco.S.Wallet = 1e6; eco.S.PadPaid[(int)pad.Kind] = 0; double prima = eco.S.Wallet;
        // entrata dal lato del QA: stick verso il centro, rilasciato con i piedi al 95% del raggio
        yield return Vai(nina, c, r * Bordo, pad.name + " entrata da " + nomeLato, v => esito = v);
        _joy.TestValue = Vector2.zero;
        if (esito != null) { _errori.AppendLine("[#171] LAYOUT (3D Artist): " + esito); yield break; }
        float t0 = Time.time; while (Time.time - t0 < 1.5f && eco.S.Wallet >= prima) yield return null;
        var p = nina.transform.position; float d = Oriz(p - c);
        bool carica = eco.S.Wallet < prima;
        Debug.Log($"[#171] {pad.name} da {nomeLato}: piedi {P(p)} a {d:0.00} m dal centro {P(c)} (r {r:0.00}), carica={carica} occupato={pad.Occupied} attiva={(ProximityZone.Active ? ProximityZone.Active.name : "nessuna")} {Visibile(pad)}");
        if (!carica)
        {
            string chi = d > r ? "ALTRO (Kaia si è fermata fuori dal disco dopo il rilascio)"
                : !pad.Occupied ? "CODICE UpgradePad (Tech Lead): piedi nel disco ma FeetOnDisc falso"
                : ProximityZone.Active != pad ? $"CODICE ProximityZone (Tech Lead): pad occupato ma zona attiva = {(ProximityZone.Active ? ProximityZone.Active.name : "nessuna")}"
                : "CODICE UpgradePad/EconomyCore (Tech Lead): pad occupato e attivo ma non scala LN";
            _errori.AppendLine($"[#171] {pad.name} entrata da {nomeLato}: non carica in 1,5 s con i piedi in {P(p)} a {d:0.00} m dal centro del disco {P(c)} (raggio {r:0.00}). {chi}. {Visibile(pad)}");
        }
        // esce dal lato da cui è entrata, per non tenere il pad occupato
        yield return Vai(nina, partenza, 0.2f, pad.name + " uscita", v => { });
        _joy.TestValue = Vector2.zero; yield return Attendi(0.3f);
    }

    /// <summary>Stick verso <paramref name="meta"/> finché Kaia è entro <paramref name="entro"/> m. Se resta ferma, esito = messaggio di blocco.</summary>
    IEnumerator Vai(NinaController nina, Vector3 meta, float entro, string cosa, System.Action<string> esito)
    {
        float tFermo = Time.time, t0 = Time.time; var pFermo = nina.transform.position;
        while (true)
        {
            var d = meta - nina.transform.position; d.y = 0;
            if (d.magnitude <= entro) yield break;
            float yaw = _camT ? _camT.eulerAngles.y : _yaw;
            var loc = Quaternion.Euler(0, -yaw, 0) * d.normalized;
            _joy.TestValue = new Vector2(loc.x, loc.z);
            yield return null;
            var p = nina.transform.position;
            if ((p - pFermo).sqrMagnitude > FermaMin * FermaMin) { pFermo = p; tFermo = Time.time; }
            else if (Time.time - tFermo > FermaSec || Time.time - t0 > 25f) { _joy.TestValue = Vector2.zero; esito(Blocco(nina, meta, cosa)); yield break; }
        }
    }

    string Blocco(NinaController nina, Vector3 meta, string cosa)
    {
        var p = nina.transform.position; var cc = nina.GetComponent<CharacterController>();
        float r = cc.radius + cc.skinWidth + 0.05f;
        int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * r, p + Vector3.up * (cc.height - r), r, _tocchi, ~0, QueryTriggerInteraction.Ignore);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            var c = _tocchi[i]; if (c.transform == nina.transform) continue; var b = c.bounds;
            sb.Append(sb.Length > 0 ? ", " : "").Append(c.name).Append($" [x {b.min.x:0.00}..{b.max.x:0.00}, z {b.min.z:0.00}..{b.max.z:0.00}]");
        }
        return $"Kaia bloccata in {P(p)} ({cosa}), a {Oriz(meta - p):0.00} m da {P(meta)}. Collider a contatto: {(sb.Length > 0 ? sb.ToString() : "nessuno")}.";
    }

    /// <summary>Raggio dei renderer del disco (figli con Ring/Pad nel nome), per confronto con l'area attiva.</summary>
    static string Visibile(UpgradePad pad)
    {
        float max = 0; string chi = "-";
        foreach (var rd in pad.GetComponentsInChildren<Renderer>())
        {
            if (!rd.enabled) continue; var nm = rd.name;
            if (nm.IndexOf("Ring", System.StringComparison.OrdinalIgnoreCase) < 0 && nm.IndexOf("Pad", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            var s = rd.bounds.size; float rr = 0.5f * Mathf.Max(s.x, s.z); if (rr > max) { max = rr; chi = nm; }
        }
        return $"Disco visibile più grande: {chi} r {max:0.00} m (area attiva r {pad.DiscRadius:0.00}).";
    }

    static float Oriz(Vector3 v) { v.y = 0; return v.magnitude; }
    static string P(Vector3 v) => $"(x {v.x:0.00}, z {v.z:0.00})";
    static IEnumerator Attendi(float s) { float t = Time.time; while (Time.time - t < s) yield return null; }
}
