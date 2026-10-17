using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#256 / #262 / #224: Kaia guidata col JOYSTICK SIMULATO (FloatingJoystick.TestValue → NinaController → CharacterController vero),
/// partendo dallo spawn, raggiunge di fila: Cassa (entra nel trigger), Nonna Ada, Cassaforte e i pad Zaino, Scarpe, Insegna (si attivano).
/// Lo sterzo è un controllore semplice verso il punto successivo di un percorso calcolato sui collider veri (griglia 10 cm, capsula r 0,3 = raggio di Kaia, come un giocatore che prova anche i varchi stretti):
/// niente linee rette e niente teletrasporti fra un bersaglio e l'altro. Se Kaia non avanza per 3 s (pilastro, varco stretto) il test FALLISCE.
/// Controlla anche i varchi segnati in scena (VarcoMarker): larghezza misurata con raycast sui collider veri ≥ 1,2 m.</summary>
public class PercorsoJoystick256Test
{
    const float H = 0.1f, X0 = -9f, Z0 = -11f, RGriglia = 0.3f, Minimo = 1.2f; const int NX = 181, NZ = 221;
    bool[,] _free;

    static bool Mobile(Collider c) => c is CharacterController || c.GetComponentInParent<NinaController>() || c.GetComponentInParent<NonnoRobot>()
        || (c.attachedRigidbody && !c.attachedRigidbody.isKinematic) || c.GetComponentInParent<UnityEngine.AI.NavMeshAgent>();

    void Griglia()
    {
        _free = new bool[NX, NZ]; var buf = new Collider[16];
        for (int i = 0; i < NX; i++) for (int j = 0; j < NZ; j++)
        {
            var p = new Vector3(X0 + i * H, 0, Z0 + j * H);
            int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * 0.4f, p + Vector3.up * 1.2f, RGriglia, buf, ~0, QueryTriggerInteraction.Ignore);
            bool ok = true; for (int k = 0; k < n; k++) if (!Mobile(buf[k])) { ok = false; break; }
            _free[i, j] = ok;
        }
    }

    (int, int) Ix(Vector3 p) => (Mathf.Clamp(Mathf.RoundToInt((p.x - X0) / H), 0, NX - 1), Mathf.Clamp(Mathf.RoundToInt((p.z - Z0) / H), 0, NZ - 1));
    static Vector3 W((int, int) c) => new Vector3(X0 + c.Item1 * H, 0, Z0 + c.Item2 * H);

    (int, int) Libera((int, int) c)
    {
        if (_free[c.Item1, c.Item2]) return c;
        for (int r = 1; r < 30; r++) for (int di = -r; di <= r; di++) for (int dj = -r; dj <= r; dj++)
        { int x = c.Item1 + di, z = c.Item2 + dj; if (x >= 0 && z >= 0 && x < NX && z < NZ && _free[x, z]) return (x, z); }
        return c;
    }

    List<Vector3> Percorso(Vector3 a, Vector3 b)
    {
        var s = Libera(Ix(a)); var g = Libera(Ix(b));
        var dist = new Dictionary<(int, int), float> { [s] = 0 }; var prev = new Dictionary<(int, int), (int, int)>(); var q = new SortedSet<(float, int, int)> { (0, s.Item1, s.Item2) };
        while (q.Count > 0)
        {
            var (d, i, j) = q.Min; q.Remove(q.Min); if ((i, j) == g) break;
            for (int di = -1; di <= 1; di++) for (int dj = -1; dj <= 1; dj++)
            {
                if (di == 0 && dj == 0) continue; int x = i + di, z = j + dj;
                if (x < 0 || z < 0 || x >= NX || z >= NZ || !_free[x, z]) continue;
                float nd = d + H * Mathf.Sqrt(di * di + dj * dj);
                if (!dist.TryGetValue((x, z), out var od) || nd < od) { if (dist.ContainsKey((x, z))) q.Remove((od, x, z)); dist[(x, z)] = nd; prev[(x, z)] = (i, j); q.Add((nd, x, z)); }
            }
        }
        if (!dist.ContainsKey(g)) return null;
        var pts = new List<Vector3>(); var u = g; while (prev.ContainsKey(u)) { pts.Add(W(u)); u = prev[u]; }
        pts.Reverse(); return pts;
    }

    static float Resto(List<Vector3> pts, int from) { float l = 0; for (int k = from + 1; k < pts.Count; k++) l += Vector3.Distance(pts[k - 1], pts[k]); return l; }

    static string Vicini(Vector3 p)
    {
        var sb = new StringBuilder();
        foreach (var c in Physics.OverlapSphere(p + Vector3.up * 0.8f, 0.9f, ~0, QueryTriggerInteraction.Ignore))
            if (!c.GetComponentInParent<NinaController>() && c.name != "Floor") sb.Append(c.transform.root.name).Append('/').Append(c.name).Append(' ');
        return sb.ToString();
    }

    /// <summary>Kaia col joystick verso <paramref name="meta"/> finché <paramref name="arrivata"/> è vero; fallisce se resta ferma 3 s.</summary>
    IEnumerator Guida(NinaController nina, Vector3 meta, System.Func<bool> arrivata, string cosa, StringBuilder log, Transform ignora = null)
    {
        if (ModoGuida) { yield return SeguiGuida(nina, meta, arrivata, cosa, log, ignora); yield break; }
        var joy = nina.Joystick; var cam = Camera.main;
        var pts = Percorso(nina.transform.position, meta); Assert.IsNotNull(pts, $"{cosa}: nessun percorso sui collider veri");
        float lung = Resto(pts, 0), best = float.MaxValue, tBest = Time.time, t0 = Time.time, replan = Time.time; int idx = 0; float fatta = 0; var last = nina.transform.position;
        while (!arrivata())
        {
            var p = nina.transform.position; p.y = 0; fatta += Vector3.Distance(new Vector3(last.x, 0, last.z), p); last = p; ControllaPad(p, cosa);
            if (Time.time - replan > 1f) { var np = Percorso(p, meta); if (np != null) { pts = np; idx = 0; } replan = Time.time; }
            // indice più vicino in avanti, poi punto a 0,5 m
            int bi = idx; float bd = float.MaxValue;
            for (int k = idx; k < Mathf.Min(pts.Count, idx + 40); k++) { float d = Vector3.Distance(p, pts[k]); if (d < bd) { bd = d; bi = k; } }
            idx = bi; int ti = Mathf.Min(pts.Count - 1, idx + 5);
            var target = ti >= pts.Count - 1 ? new Vector3(meta.x, 0, meta.z) : pts[ti];
            var dir = target - p; dir.y = 0;
            float resto = Resto(pts, idx) + bd;
            if (resto < best - 0.15f) { best = resto; tBest = Time.time; }
            if (Time.time - tBest > 3f)
            { joy.TestValue = Vector2.zero; Assert.Fail($"{cosa}: Kaia BLOCCATA a ({p.x:0.00}; {p.z:0.00}), mancano {resto:0.00} m. Vicino: {Vicini(nina.transform.position)}"); }
            if (Time.time - t0 > 60f) { joy.TestValue = Vector2.zero; Assert.Fail($"{cosa}: tempo scaduto a ({p.x:0.00}; {p.z:0.00})"); }
            float yaw = cam ? cam.transform.eulerAngles.y : 45f;
            var local = Quaternion.Euler(0, -yaw, 0) * (dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.zero);
            joy.TestValue = new Vector2(local.x, local.z) * (dir.magnitude < 0.3f ? 0.5f : 1f);
            yield return null;
        }
        joy.TestValue = Vector2.zero;
        log.AppendLine($"{cosa}: OK in {Time.time - t0:0.0} s, {fatta:0.00} m percorsi (percorso minimo {lung:0.00} m)");
        float tf = Time.time; while (Time.time - tf < 0.2f) yield return null;
    }

    static bool ModoGuida;

    /// <summary>Segmento dritto libero per Kaia (capsula r 0,3, solo collider statici; gli ultimi <paramref name="margine"/> m non contano:
    /// il bersaglio stesso, es. la Cassaforte, ha un collider).</summary>
    /// <summary>Tappe intermedie in campo aperto (non varchi fra due solidi): solo larghezza ≤ spazio libero.</summary>
    static readonly HashSet<string> TappeIntermedie = new() { "Varco_UscitaOvest", "Varco_ScaffaliEst", "Varco_AngoloCassa" };
    static float Larg(VarcoMarker v) => v.larghezza;
    static int Ord(VarcoMarker v) => v.ordine;

    // ---- #266 (Animator) + condizione GD: nessun percorso principale dentro l'area estesa di un pad che non è la meta ----
    /// <summary>Area d'attivazione di #266 nel caso peggiore (review Tech Lead #270): disco 0,70 m; verso la camera fino a +0,30 m di tolleranza
    /// +0,05 m di isteresi d'uscita (× cos dell'angolo dalla direzione della camera, −forward proiettato su XZ), con la camera ruotata di ±45°
    /// (rotazione #205): l'angolo conta solo oltre i 45°. Il rilevamento di #266 è sul pivot di Kaia (FeetOnDisc): niente raggio di Kaia in più.</summary>
    const float TolleranzaCamera266 = 0.30f, Isteresi266 = 0.05f, RotazioneCamera266 = 45f;
    static float RaggioArea(UpgradePad pad, Vector3 q)
    {
        var c = pad.DiscCenter; float dx = q.x - c.x, dz = q.z - c.z, d = Mathf.Sqrt(dx * dx + dz * dz); float extra = TolleranzaCamera266 + Isteresi266;
        if (d < 1e-4f) return pad.DiscRadius + extra;
        var cam = Camera.main; var f = cam ? cam.transform.forward : Vector3.forward; f.y = 0; if (f.sqrMagnitude < 1e-4f) f = Vector3.forward; f.Normalize();
        float ang = Vector3.Angle(new Vector3(dx / d, 0, dz / d), -f); // 0 = dritto verso la camera
        float a = Mathf.Max(0f, ang - RotazioneCamera266);
        return pad.DiscRadius + (a < 90f ? extra * Mathf.Cos(a * Mathf.Deg2Rad) : 0f);
    }
    static UpgradePad[] _pads; static Component _metaAttuale, _partenza;
    static void Meta(Component m) { _partenza = _metaAttuale; _metaAttuale = m; _uscitaCassa = false; } static string _modo = "";
    /// <summary>pad → (margine minimo = distanza − raggio area, percorso, punto)</summary>
    static readonly Dictionary<string, (float margine, string rotta, Vector3 punto)> _minPad = new();
    // ---- condizione GD (#270, 6228063616): fuori dalle tappe verso la Cassa, il bordo del CC di Kaia (r 0,3) e i clienti (collider vero)
    //      restano ad almeno 0,30 m dal trigger di Zona_Cassa ----
    const float DistanzaCassaMin = 0.30f;
    static Bounds? _cassaB; static Component _cassaZ; static bool _uscitaCassa; static int _puntiCoda;
    static readonly HashSet<string> _tappeDallaCassa = new();
    static readonly Dictionary<string, (float d, Vector3 p)> _minCassa = new();
    static float DistRett(Vector3 p, Bounds b) { float dx = Mathf.Max(b.min.x - p.x, 0f, p.x - b.max.x), dz = Mathf.Max(b.min.z - p.z, 0f, p.z - b.max.z); return Mathf.Sqrt(dx * dx + dz * dz); }
    static void Registra(string chiave, float d, Vector3 p) { if (!_minCassa.TryGetValue(chiave, out var x) || d < x.d) _minCassa[chiave] = (d, p); }
    static void ControllaCassa(Vector3 p, string cosa)
    {
        if (_cassaB == null) return;
        // review #270 (6228339511): la tappa VERSO la Cassa non conta; quella che PARTE dalla Cassa conta da quando il bordo di Kaia
        // è uscito per la prima volta oltre 0,30 m dal trigger (poi non deve più rientrare sotto 0,30 m)
        if (_metaAttuale != _cassaZ)
        {
            float d = DistRett(p, _cassaB.Value) - 0.3f;
            if (_partenza == _cassaZ && !_uscitaCassa) { _tappeDallaCassa.Add(_modo + cosa); if (d >= DistanzaCassaMin) _uscitaCassa = true; }
            if (_partenza != _cassaZ || _uscitaCassa) Registra(_modo + cosa, d, p);
        }
        foreach (var c in Object.FindObjectsByType<Customer>(FindObjectsSortMode.None))
        {
            var col = c.GetComponent<CapsuleCollider>(); float r = col ? col.radius * Mathf.Max(c.transform.lossyScale.x, c.transform.lossyScale.z) : Customer.BodyRadius;
            Registra("clienti in scena (collider vero)", DistRett(c.transform.position, _cassaB.Value) - r, c.transform.position);
        }
    }
    static bool TappaPad(string cosa) => cosa.Contains("Pad ");
    static void ControllaPad(Vector3 p, string cosa)
    {
        ControllaCassa(p, cosa);
        if (_pads == null) return;
        foreach (var pad in _pads)
        {
            if (!pad || pad == _metaAttuale || pad == _partenza) continue; // la meta e il pad da cui si parte non contano
            var c = pad.DiscCenter; float d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)); float m = d - RaggioArea(pad, p);
            var key = pad.name + (TappaPad(cosa) ? " (tappe da/verso i pad)" : " (percorsi principali)");
            if (!_minPad.TryGetValue(key, out var x) || m < x.margine) _minPad[key] = (m, _modo + cosa, p);
        }
    }
    static string _ultimoOstacolo = "";
    /// <summary>Ultimo tratto verso la meta che non conta: il bersaglio stesso (Cassaforte, Nonna Ada) ha un collider di ~0,35 m + Kaia r 0,3.</summary>
    const float MargineMeta = 1.0f;
    static bool Libero(Vector3 a, Vector3 b, float margine)
    {
        a.y = b.y = 0; var d = b - a; float len = d.magnitude - margine; if (len <= 0.05f) return true; d /= d.magnitude;
        const float r = 0.3f;
        foreach (var h in Physics.CapsuleCastAll(a + Vector3.up * (r + 0.1f), a + Vector3.up * 1.4f, r, d, len, ~0, QueryTriggerInteraction.Ignore))
            if (!Mobile(h.collider) && !h.collider.attachedRigidbody) { if (h.distance <= 0f) { var cp = h.collider.ClosestPoint(a + Vector3.up * 0.8f) - a; cp.y = 0; if (Vector3.Dot(cp, d) <= 0f) continue; } var hp = h.point; _ultimoOstacolo = $"{h.collider.name}@({hp.x:0.00};{hp.z:0.00})"; return false; }
        return true;
    }

    /// <summary>Rotta della guida, senza NavMesh (regola del Tech Lead, #265): dritto se il segmento è libero; altrimenti di varco in varco
    /// seguendo l'ordine dei varchi (crescente o decrescente) da un varco d'ingresso visibile a uno d'uscita da cui la meta è dritta.
    /// Ogni segmento (Kaia → primo varco, varco → varco consecutivo, ultimo varco → meta) deve essere libero per Kaia (r 0,3).</summary>
    static List<VarcoMarker> Rotta(Vector3 p, Vector3 meta, string cosa)
    {
        var r = new List<VarcoMarker>();
        if (Libero(p, meta, MargineMeta)) return r;
        var v = new List<VarcoMarker>(Object.FindObjectsByType<VarcoMarker>(FindObjectsSortMode.None)); v.Sort((x, y) => Ord(x).CompareTo(Ord(y)));
        if (v.Count == 0) return r; // senza marker (main 8e33a40) la guida resta in linea retta: decide la fisica
        // tutte le sequenze di varchi con ordine strettamente crescente o decrescente (si possono saltare varchi): la più corta con ogni segmento libero
        float best = float.MaxValue; List<VarcoMarker> migliore = null; int n = Mathf.Min(v.Count, 12);
        for (int mask = 1; mask < (1 << n); mask++)
            for (int dir = 0; dir < 2; dir++)
            {
                var seq = new List<VarcoMarker>(); for (int k = 0; k < n; k++) if ((mask & (1 << k)) != 0) seq.Add(v[k]); if (dir == 1) seq.Reverse();
                var prev = Flat(p); float c = 0; bool ok = true;
                foreach (var m in seq) { var q = Flat(m.transform.position); if (!Libero(prev, q, 0f)) { ok = false; break; } c += Vector3.Distance(prev, q); prev = q; }
                if (!ok || !Libero(prev, meta, MargineMeta)) continue; c += Vector3.Distance(prev, Flat(meta));
                if (c < best) { best = c; migliore = seq; }
            }
        if (migliore == null) { Assert.Fail($"[segui la guida] {cosa}: da ({p.x:0.00}; {p.z:0.00}) nessuna catena di varchi con segmenti liberi verso la meta ({v.Count} varchi). " + Diagnosi(p, meta, v)); return r; }
        r.AddRange(migliore);
        return r;
    }
    static Vector3 Flat(Vector3 a) { a.y = 0; return a; }
    static string Diagnosi(Vector3 p, Vector3 meta, List<VarcoMarker> v)
    {
        var sb = new StringBuilder();
        foreach (var m in v) sb.Append($"{m.name}: Kaia→varco {(Libero(p, m.transform.position, 0f) ? "libero" : "CHIUSO da " + _ultimoOstacolo)}, varco→meta {(Libero(m.transform.position, meta, MargineMeta) ? "libero" : "CHIUSO da " + _ultimoOstacolo)}; ");
        for (int k = 0; k < v.Count; k++) for (int j = k + 1; j < v.Count; j++) sb.Append($"{v[k].name}→{v[j].name} {(Libero(v[k].transform.position, v[j].transform.position, 0f) ? "libero" : "CHIUSO da " + _ultimoOstacolo)}; ");
        return sb.ToString();
    }

    /// <summary>Modo (A) "segui la guida": come un giocatore vero. Joystick dritto dove punta la mano del tutorial
    /// (<see cref="VarcoMarker.ProssimaTappa"/>, la stessa funzione di OnboardingHints). Nessun pathfinding. Senza marker (main 8e33a40)
    /// è la linea retta verso il bersaglio. In più, alla partenza di ogni tappa, <see cref="Rotta"/> verifica la regola del layout.</summary>
    IEnumerator SeguiGuida(NinaController nina, Vector3 meta, System.Func<bool> arrivata, string cosa, StringBuilder log, Transform ignora)
    {
        var joy = nina.Joystick; var cam = Camera.main; meta.y = 0;
        // regola del layout: dalla partenza esiste una catena di varchi (in ordine) con segmenti dritti liberi per Kaia
        var rotta = Rotta(nina.transform.position, meta, cosa);
        string tappe = ""; foreach (var m in rotta) tappe += m.name + " → ";
        int passati = 0; VarcoMarker prima = null; string guida = "";
        float t0 = Time.time, best = float.MaxValue, tBest = Time.time, fatta = 0; var last = nina.transform.position;
        while (!arrivata())
        {
            var p = nina.transform.position; p.y = 0; fatta += Vector3.Distance(new Vector3(last.x, 0, last.z), p); last = p; ControllaPad(p, cosa);
            float dm = Vector3.Distance(p, meta);
            // la stessa funzione della mano del tutorial (OnboardingHints → VarcoMarker.ProssimaTappa, #265/#268)
            var target = Flat(VarcoMarker.ProssimaTappa(nina.transform.position, meta, ignora, ref passati, out var cur));
            if (cur != prima) { prima = cur; best = float.MaxValue; tBest = Time.time; guida += (cur ? cur.name : "meta") + $"@({p.x:0.0};{p.z:0.0}) → "; }
            var dir = target - p;
            float resto = dir.magnitude;
            if (resto < best - 0.15f) { best = resto; tBest = Time.time; }
            if (Time.time - tBest > 3f)
            { joy.TestValue = Vector2.zero; Assert.Fail($"[segui la guida] {cosa}: Kaia BLOCCATA a ({p.x:0.00}; {p.z:0.00}) puntando verso {(cur ? cur.name : "la meta")} ({target.x:0.00}; {target.z:0.00}), mancano {dm:0.00} m in linea d'aria. Vicino: {Vicini(nina.transform.position)}. Mano: {guida}; catena del layout: {tappe}meta; passati=0x{passati:X}; linea retta chiusa={VarcoMarker.Chiuso(nina.transform.position, meta, ignora)}"); }
            if (Time.time - t0 > 60f) { joy.TestValue = Vector2.zero; Assert.Fail($"[segui la guida] {cosa}: tempo scaduto a ({p.x:0.00}; {p.z:0.00})"); }
            float yaw = cam ? cam.transform.eulerAngles.y : 45f;
            var local = Quaternion.Euler(0, -yaw, 0) * (dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.zero);
            joy.TestValue = new Vector2(local.x, local.z) * (dir.magnitude < 0.3f && !cur ? 0.5f : 1f);
            yield return null;
        }
        joy.TestValue = Vector2.zero;
        log.AppendLine($"[segui la guida] {cosa}: OK in {Time.time - t0:0.0} s, {fatta:0.00} m; mano: {guida}meta; catena del layout: {tappe}meta");
        float tf = Time.time; while (Time.time - tf < 0.2f) yield return null;
    }

    /// <summary>(A) Segui la guida: dritto verso varchi/meta. È quello che fa il giocatore (QA #262).</summary>
    [UnityTest, Timeout(600000)]
    public IEnumerator SeguiLaGuida_DalloSpawn_BanconeCassaAdaCassaforteEPad() { ModoGuida = true; _modo = "[segui la guida] "; try { yield return Corsa(); } finally { ModoGuida = false; } }

    /// <summary>(B) Percorso calcolato sui collider veri: copertura di regressione.</summary>
    [UnityTest, Timeout(600000)]
    public IEnumerator Joystick_DalloSpawn_BanconeCassaAdaCassaforteEPad() { ModoGuida = false; _modo = "[percorso calcolato] "; yield return Corsa(); }

    IEnumerator Corsa()
    {
        PlayerPrefs.DeleteAll(); PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 1); PlayerPrefs.Save();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        UltimoMiglio.UI.HudReveal.ResetAll(); TutorialManager.TestSenzaComparse = true; TutorialManager.TestBloccaPassi = true; TutorialManager.TestAccelera = true; Time.timeScale = 1f;
        NinaController nina = null;
        try
        {
            yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
            for (int i = 0; i < 300 && (!GameRoot.Ready || !GameRoot.I.Nina || !TutorialManager.I); i++) yield return null;
            Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
            var tut = TutorialManager.I; nina = GameRoot.I.Nina; Assert.IsNotNull(nina.Joystick, "joystick mancante");
            for (var s = tut.Current + 1; s <= TutorialManager.Step.PadNuovi; s++) { tut.TestEntraNelPasso(s); yield return null; }
            GameRoot.I.Eco.S.Wallet = 0;
            float tw = Time.time; while (Time.time - tw < 0.5f) yield return null;
            var cc = nina.GetComponent<CharacterController>(); cc.enabled = false; nina.transform.position = NinaController.SafeSpawn; cc.enabled = true; Physics.SyncTransforms();
            Griglia();
            var log = new StringBuilder();

            // varchi segnati: larghezza reale ≥ 1,2 m
            var varchi = Object.FindObjectsByType<VarcoMarker>(FindObjectsSortMode.None);
            foreach (var v in varchi)
            {
                var o = v.transform.position + Vector3.up * 0.5f; var r = v.transform.right;
                float dl = 3f, dr = 3f;
                foreach (var h in Physics.RaycastAll(o, r, 3f, ~0, QueryTriggerInteraction.Ignore)) if (!Mobile(h.collider)) dr = Mathf.Min(dr, h.distance);
                foreach (var h in Physics.RaycastAll(o, -r, 3f, ~0, QueryTriggerInteraction.Ignore)) if (!Mobile(h.collider)) dl = Mathf.Min(dl, h.distance);
                log.AppendLine($"varco {v.name} (ordine {Ord(v)}): nominale {Larg(v):0.00} m, misurato {dl + dr:0.00} m (sx {dl:0.00}, dx {dr:0.00})");
                Assert.GreaterOrEqual(dl + dr, Minimo, $"varco {v.name}: {dl + dr:0.00} m < {Minimo} m");
                if (TappeIntermedie.Contains(v.name))
                    Assert.LessOrEqual(Larg(v), dl + dr + 0.05f, $"tappa {v.name}: larghezza {Larg(v):0.00} m oltre lo spazio libero {dl + dr:0.00} m");
                else
                {   // review Tech Lead #270: marker al centro reale dell'apertura e larghezza = misura (tolleranza 5 cm)
                    Assert.Less(dl, 2.99f, $"varco {v.name}: nessun collider a sinistra entro 3 m"); Assert.Less(dr, 2.99f, $"varco {v.name}: nessun collider a destra entro 3 m");
                    Assert.LessOrEqual(Mathf.Abs(dr - dl) * 0.5f, 0.05f, $"varco {v.name}: fuori centro di {(dr - dl) * 0.5f:0.00} m (sx {dl:0.00}, dx {dr:0.00})");
                    Assert.AreEqual(dl + dr, Larg(v), 0.05f, $"varco {v.name}: larghezza {Larg(v):0.00} m ≠ misurata {dl + dr:0.00} m");
                }
            }

            var cassa = Object.FindAnyObjectByType<CassaZone>(); Assert.IsNotNull(cassa, "Cassa mancante");
            var ada = GameObject.Find("NonnaAda"); Assert.IsNotNull(ada, "Nonna Ada mancante");
            var safe = GameObject.Find(TutorialManager.SafeObjectName); Assert.IsNotNull(safe, "Cassaforte mancante");
            UpgradePad zaino = null, scarpe = null, insegna = null;
            foreach (var p in Object.FindObjectsByType<UpgradePad>(FindObjectsSortMode.None)) { if (p.Kind == UpgradeKind.Zaino) zaino = p; if (p.Kind == UpgradeKind.Scarpe) scarpe = p; if (p.Kind == UpgradeKind.Insegna) insegna = p; }
            float Oriz(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }

            var bancone = Object.FindAnyObjectByType<Bancone>(); Assert.IsNotNull(bancone, "Bancone mancante");
            _minPad.Clear(); _minCassa.Clear(); _tappeDallaCassa.Clear(); _puntiCoda = 0; _uscitaCassa = false; _cassaZ = cassa; var cassaCol = cassa.GetComponent<Collider>(); Assert.IsNotNull(cassaCol, "Zona_Cassa senza trigger"); _cassaB = cassaCol.bounds; _metaAttuale = _partenza = null; _pads = Object.FindObjectsByType<UpgradePad>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var arrivi = Object.FindAnyObjectByType<ArrivalsZone>(); Assert.IsNotNull(arrivi, "Zona_Arrivi mancante");
            ProximityZone Z(string n) { var g = GameObject.Find(n); Assert.IsNotNull(g, n + " mancante"); return g.GetComponent<ProximityZone>(); }
            var scaffA = Z("Zona_Scaffale_A"); var prendiA = Z("Zona_Prendi_A"); var scaffC = Z("Zona_Scaffale_C"); var prendiC = Z("Zona_Prendi_C");
            Meta(bancone); yield return Guida(nina, bancone.transform.position, () => bancone.Occupied, "spawn → Bancone", log, bancone.transform);
            Meta(cassa); yield return Guida(nina, cassa.transform.position, () => cassa.Occupied, "Bancone → Cassa", log, cassa.transform);
            Meta(null); yield return Guida(nina, ada.transform.position, () => Oriz(nina.transform.position, ada.transform.position) <= 1.0f, "Cassa → Nonna Ada", log, ada.transform);
            Meta(null); yield return Guida(nina, safe.transform.position, () => TutorialManager.VicinoAllaCassaforte(nina.transform.position, safe.transform.position), "Ada → Cassaforte", log, safe.transform);
            Meta(zaino); yield return Guida(nina, zaino.transform.position, () => zaino.Occupied, "Cassaforte → Pad Zaino", log, zaino.transform);
            Meta(scarpe); yield return Guida(nina, scarpe.transform.position, () => scarpe.Occupied, "Zaino → Pad Scarpe", log, scarpe.transform);
            Meta(insegna); yield return Guida(nina, insegna.transform.position, () => insegna.Occupied, "Scarpe → Pad Insegna", log, insegna.transform);
            Meta(bancone); yield return Guida(nina, bancone.transform.position, () => bancone.Occupied, "Pad Insegna → Bancone", log, bancone.transform);
            Meta(arrivi); yield return Guida(nina, arrivi.transform.position, () => arrivi.Occupied, "Bancone → Arrivi", log, arrivi.transform);
            Meta(scaffC); yield return Guida(nina, scaffC.transform.position, () => scaffC.Occupied, "Arrivi → Scaffale C", log, scaffC.transform);
            Meta(prendiC); yield return Guida(nina, prendiC.transform.position, () => prendiC.Occupied, "Scaffale C → Prendi C", log, prendiC.transform);
            Meta(bancone); yield return Guida(nina, bancone.transform.position, () => bancone.Occupied, "Prendi C → varco → Bancone", log, bancone.transform);
            Meta(scaffA); yield return Guida(nina, scaffA.transform.position, () => scaffA.Occupied, "Bancone → Scaffale A", log, scaffA.transform);
            Meta(prendiA); yield return Guida(nina, prendiA.transform.position, () => prendiA.Occupied, "Scaffale A → Prendi A", log, prendiA.transform);
            Meta(bancone); yield return Guida(nina, bancone.transform.position, () => bancone.Occupied, "Prendi A → varco → Bancone", log, bancone.transform);
            var monete = bancone.transform.position + Vector3.right * 0.8f; // dove Kaia raccoglie le monete dei clienti (come LoopAutoTest)
            Meta(null); yield return Guida(nina, monete, () => Oriz(nina.transform.position, monete) <= 0.35f, "Bancone → monete della coda", log);
            Meta(null);
            // coda clienti (informativo: i pad li attiva solo Kaia): margine degli slot e del tratto porta → coda dall'area estesa dei pad
            var coda = Object.FindAnyObjectByType<CustomerQueue>();
            Assert.IsNotNull(coda, "CustomerQueue mancante nella scena: impossibile verificare la coda dei clienti"); // review #270: niente salto silenzioso
            foreach (var pad in _pads)
                {
                    float mm = float.MaxValue; Vector3 pp = Vector3.zero;
                    var pts = new List<Vector3> { coda.SpawnPoint }; for (int i = 0; i < coda.MaxLine; i++) pts.Add(coda.Slot(i));
                    for (int i = 0; i + 1 < pts.Count; i++) for (float t = 0; t <= 1f; t += 0.05f) { var q = Vector3.Lerp(pts[i], pts[i + 1], t); var c = pad.DiscCenter; float m = Vector2.Distance(new Vector2(q.x, q.z), new Vector2(c.x, c.z)) - RaggioArea(pad, q); if (m < mm) { mm = m; pp = q; } }
                    log.AppendLine($"coda {pad.name}: margine minimo {mm:0.00} m dall'area estesa (a ({pp.x:0.00}; {pp.z:0.00}))");
                }
            // coda clienti con il collider vero del prefab: porta → slot
            string chiaveCoda = null;
            {
                var pref = coda.Prefab ? coda.Prefab.GetComponent<CapsuleCollider>() : null; float rc = pref ? pref.radius * Mathf.Max(coda.Prefab.transform.localScale.x, coda.Prefab.transform.localScale.z) : Customer.BodyRadius;
                var pts = new List<Vector3> { coda.SpawnPoint }; for (int i = 0; i < coda.MaxLine; i++) pts.Add(coda.Slot(i));
                for (int i = 0; i + 1 < pts.Count; i++) for (float t = 0; t <= 1f; t += 0.05f) { var q = Vector3.Lerp(pts[i], pts[i + 1], t); chiaveCoda = $"coda clienti (porta → slot, r {rc:0.00})"; Registra(chiaveCoda, DistRett(q, _cassaB.Value) - rc, q); _puntiCoda++; }
            }
            Assert.Greater(_puntiCoda, 0, "coda clienti: nessun punto del tratto porta → slot misurato dal trigger della Cassa");
            Assert.IsTrue(chiaveCoda != null && _minCassa.ContainsKey(chiaveCoda), "coda clienti: la distanza dal trigger della Cassa non è stata registrata");
            var vicini = new List<string>();
            // review #270 (Code Reviewer + Tech Lead): sulle tappe che partono dalla Cassa il controllo deve essersi armato
            Assert.IsNotEmpty(_tappeDallaCassa, "nessuna tappa in partenza dalla Cassa: la Cassa → Nonna Ada non è stata controllata");
            foreach (var t in _tappeDallaCassa) Assert.IsTrue(_minCassa.ContainsKey(t), $"{t}: controllo della Cassa mai armato (il bordo di Kaia non è mai arrivato a {DistanzaCassaMin:0.00} m dal trigger)");
            foreach (var kv in _minCassa)
            {
                log.AppendLine($"Cassa: {kv.Key}: distanza minima {kv.Value.d:0.00} m dal trigger (a ({kv.Value.p.x:0.00}; {kv.Value.p.z:0.00}))");
                if (kv.Value.d < DistanzaCassaMin) vicini.Add($"{kv.Key}: {kv.Value.d:0.00} m a ({kv.Value.p.x:0.00}; {kv.Value.p.z:0.00})");
            }
            // condizione GD (#266): nessun percorso principale nell'area estesa di un pad che non è la meta della tappa
            var fuori = new List<string>();
            foreach (var kv in _minPad)
            {
                log.AppendLine($"pad {kv.Key}: margine minimo {kv.Value.margine:0.00} m dall'area estesa (percorso {kv.Value.rotta}, Kaia a ({kv.Value.punto.x:0.00}; {kv.Value.punto.z:0.00}))");
                if (kv.Value.margine < 0f) fuori.Add($"{kv.Key}: {kv.Value.rotta} ci entra di {-kv.Value.margine:0.00} m a ({kv.Value.punto.x:0.00}; {kv.Value.punto.z:0.00})");
            }
            Debug.Log("[#256-joystick]\n" + log);
            Assert.IsEmpty(fuori, "percorsi principali dentro l'area estesa (#266) di pad non bersaglio: " + string.Join("; ", fuori));
            Assert.IsEmpty(vicini, $"condizione GD: a meno di {DistanzaCassaMin:0.00} m dal trigger della Cassa senza andarci: " + string.Join("; ", vicini));
            Assert.GreaterOrEqual(varchi.Length, 4, "attesi almeno 4 varchi segnati (VarcoMarker) sui percorsi del Bancone");
        }
        finally
        {
            if (nina && nina.Joystick) nina.Joystick.TestValue = null;
            TutorialManager.TestBloccaPassi = false; TutorialManager.TestSenzaComparse = false; TutorialManager.TestAccelera = false; Time.timeScale = 1f;
            _pads = null; _metaAttuale = null; _cassaB = null; _cassaZ = null;
        }
    }
}
