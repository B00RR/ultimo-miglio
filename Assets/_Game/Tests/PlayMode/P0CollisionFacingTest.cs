using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>P0 #58/#59: Nina non attraversa muri/oggetti/bordi; NONNO, clienti e Nina guardano dove camminano.</summary>
public class P0CollisionFacingTest
{
    static IEnumerator Load()
    {
        PlayerPrefs.DeleteKey(UltimoMiglio.Gameplay.TutorialManager.PrefKey); PlayerPrefs.DeleteKey("um_save_v1"); PlayerPrefs.DeleteKey(UltimoMiglio.Gameplay.DepotCameraFit.PrefZoom); // solo chiavi di gioco: vol_music e vol_sfx restano (come #88)
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 90 && !GameRoot.Ready; i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot not ready");
    }

    static void Teleport(NinaController n, Vector3 p)
    {
        var cc = n.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
        n.transform.position = p; if (cc) cc.enabled = true;
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator Nina_SpawnAfterReset_InsidePlayableArea()
    {
        yield return Load();
        var nina = GameRoot.I.Nina; var p = nina.transform.position;
        Assert.IsTrue(p.z < 7.0f && p.z > -10.2f && p.x > -8.6f && p.x < 7.0f, "#60 spawn fuori area: " + p);
        Teleport(nina, new Vector3(0, 0.05f, 9.6f)); nina.EnsurePlayableSpawn(); yield return null;
        Assert.Less(nina.transform.position.z, 7.0f, "#60 Nina resta dietro il muro N");
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator Nina_PushedAgainstWallsObjectsAndEdges_DoesNotPassThrough()
    {
        yield return Load();
        var nina = GameRoot.I.Nina; Assert.IsNotNull(nina);
        Assert.IsNotNull(nina.GetComponent<CharacterController>(), "Nina deve usare la fisica (CharacterController)");
        var targets = new List<Collider>();
        foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (c.isTrigger || c is CharacterController || !c.enabled) continue;
            var b = c.bounds; if (b.size.y < 0.25f || b.min.y > 0.5f) continue;
            if (c.GetComponentInParent<NinaController>() || c.GetComponentInParent<NonnoRobot>() || c.GetComponentInParent<Customer>()) continue;
            var n = c.name.ToLowerInvariant();
            if (n.StartsWith("bound_") || n.StartsWith("scaffale_") || n.Contains("bancone") || n.Contains("wall") || n.Contains("arch") || n.Contains("muro") || n.Contains("pot") || n.Contains("vaso") || n.Contains("plant"))
                targets.Add(c);
        }
        int bounds = targets.FindAll(c => c.name.StartsWith("Bound_")).Count;
        Assert.AreEqual(4, bounds, "servono i 4 bordi mappa");
        Assert.GreaterOrEqual(targets.Count, 8, "pochi collider solidi trovati: " + targets.Count);
        int tested = 0; var fails = new List<string>();
        foreach (var c in targets)
        {
            var b = c.bounds;
            // asse sottile, verso il centro della mappa (0,0,-2)
            Vector3 axis = b.size.x < b.size.z ? Vector3.right : Vector3.forward;
            float ext = Vector3.Scale(b.extents, axis).magnitude;
            var toC = new Vector3(0, 0, -2) - b.center; toC.y = 0;
            if (Vector3.Dot(toC, axis) < 0) axis = -axis;
            // punto sul lato lungo vicino al centro mappa, clampato nel pavimento
            var along = Vector3.Cross(Vector3.up, axis);
            float half = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(along.x), 0, Mathf.Abs(along.z))));
            float s = Mathf.Clamp(Vector3.Dot(new Vector3(0, 0, -2) - b.center, along), -half * 0.6f, half * 0.6f);
            var start = b.center + along * s + axis * (ext + 0.9f); start.y = 0.05f;
            bool isBound = c.name.StartsWith("Bound_");
            if (!isBound && (start.x < -8.6f || start.x > 7.0f || start.z > 7.0f || start.z < -10.2f)) continue; // fuori dall'area giocabile (clamp)
            if (Physics.CheckCapsule(start + Vector3.up * 0.35f, start + Vector3.up * 1.3f, 0.32f, ~0, QueryTriggerInteraction.Ignore)) continue;
            Teleport(nina, start); yield return null;
            nina.ScriptedInput = -axis;
            float minPen = 99f;
            for (float t = 0; t < 2f; t += Time.deltaTime)
            {
                yield return null; var cp = nina.transform.position; var cq = b.ClosestPoint(new Vector3(cp.x, b.center.y, cp.z)); cq.y = cp.y;
                minPen = Mathf.Min(minPen, Vector3.Distance(cq, cp));
            }
            bool big = Mathf.Max(b.size.x, b.size.z) > 1.2f; // i piccoli (vasi) si possono aggirare scivolando: conta solo la compenetrazione
            nina.ScriptedInput = Vector3.zero;
            float d = Vector3.Dot(nina.transform.position - b.center, axis);
            float pen = minPen; // distanza minima dal box durante la spinta: < 0.2 = Nina dentro l'oggetto
            if ((big && d < -ext) || pen < 0.2f) fails.Add($"{c.name} d={d:F2} ext={ext:F2} dist={pen:F2}");
            tested++;
        }
        Assert.GreaterOrEqual(tested, 6, "troppo pochi ostacoli testati: " + tested);
        Assert.IsEmpty(fails, "Nina attraversa: " + string.Join(" | ", fails));
    }

    static void Sample(Transform root, Transform model, ref Vector3 last, ref Vector3 lastDir, List<float> dots)
    {
        var d = root.position - last; d.y = 0; last = root.position;
        if (d.sqrMagnitude < 1e-6f) { lastDir = Vector3.zero; return; }
        var dir = d.normalized;
        if (lastDir != Vector3.zero && Vector3.Dot(dir, lastDir) > 0.995f) // tratto rettilineo (no curve in corso)
        { var f = model.forward; f.y = 0; dots.Add(Vector3.Dot(f.normalized, dir)); }
        lastDir = dir;
    }

    static void AssertFacing(string who, List<float> dots)
    {
        Assert.GreaterOrEqual(dots.Count, 10, who + ": pochi campioni in cammino");
        dots.Sort(); float p10 = dots[dots.Count / 10];
        Assert.Greater(p10, 0.9f, $"{who} non guarda dove cammina: p10 dot={p10:F2} min={dots[0]:F2}");
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator Walkers_FaceMovementDirection()
    {
        yield return Load();
        // Nina
        var nina = GameRoot.I.Nina; Teleport(nina, new Vector3(0, 0.05f, -2)); yield return null;
        var nm = nina.Animator ? nina.Animator.transform : nina.transform;
        var nd = new List<float>(); var last = nina.transform.position; var ld = Vector3.zero;
        foreach (var dir in new[] { Vector3.right, Vector3.back, Vector3.left, Vector3.forward })
        {
            nina.ScriptedInput = dir;
            for (float t = 0; t < 1f; t += Time.deltaTime) { yield return null; Sample(nina.transform, nm, ref last, ref ld, nd); }
        }
        nina.ScriptedInput = Vector3.zero; Teleport(nina, new Vector3(6, 0.05f, -9));
        AssertFacing("Nina", nd);

        // NONNO
        var eco = GameRoot.I.Eco; eco.S.Wallet = 100;
        bool done = false; for (int i = 0; i < 500 && !done; i++) done = eco.PadTick(UpgradeKind.NonnoHire, i * 0.1);
        Assert.IsTrue(done, "hire NONNO");
        var bot = Object.FindAnyObjectByType<NonnoRobot>(); Assert.IsNotNull(bot);
        if (bot.Arrivals) bot.Arrivals.Stock = 30;
        var bm = bot.Animator ? bot.Animator.transform : bot.transform;
        var bd = new List<float>(); var bl = bot.transform.position; var bld = Vector3.zero;
        var cd = new List<float>(); var cl = new Dictionary<Customer, (Vector3, Vector3)>();
        for (float t = 0; t < 40f && (bd.Count < 60 || cd.Count < 30); t += Time.deltaTime)
        {
            yield return null;
            Sample(bot.transform, bm, ref bl, ref bld, bd);
            foreach (var c in Object.FindObjectsByType<Customer>(FindObjectsSortMode.None))
            {
                if (!cl.TryGetValue(c, out var st)) { cl[c] = (c.transform.position, Vector3.zero); continue; }
                var (p, dd) = st; var an = c.GetComponentInChildren<Animator>();
                Sample(c.transform, an ? an.transform : c.transform, ref p, ref dd, cd); cl[c] = (p, dd);
            }
        }
        AssertFacing("NONNO", bd);
        AssertFacing("Clienti", cd);
    }

    // ---------- #82 ----------
    static Bounds RendBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true); var b = new Bounds(go.transform.position, Vector3.zero); bool first = true;
        foreach (var r in rs) { if (r.name == "SignFaceDecal") continue; if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        return b;
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator Kit_CollidersInsideMeshBounds_NoDoubleColliders()
    {
        yield return Load();
        var kit = GameObject.Find("DepotKit"); Assert.IsNotNull(kit, "DepotKit assente");
        var fails = new List<string>(); int n = 0;
        foreach (var bc in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            var t = bc.transform; if (!t.name.StartsWith("env_") && !t.name.StartsWith("prop_")) continue;
            // tolleranza 10 cm per lato: l'AABB dei renderer figli ruotati non coincide al cm con l'OBB, ma il gonfiaggio da yaw (#82) era del 20-35%
            var rb = RendBounds(t.gameObject); rb.Expand(0.2f); var cb = bc.bounds; n++;
            if (!rb.Contains(cb.min) || !rb.Contains(cb.max)) fails.Add($"{t.name} col={cb.size} mesh={rb.size}");
        }
        Assert.GreaterOrEqual(n, 10, "pochi collider kit: " + n);
        Assert.IsEmpty(fails, "collider fuori dai bounds della mesh: " + string.Join(" | ", fails));
        var ban = GameObject.Find("Bancone_Mesh"); Assert.IsNotNull(ban);
        Assert.IsNotNull(ban.GetComponent<Collider>(), "Bancone_Mesh: il secondo collider resta (regressione #58)");
        Assert.IsNotNull(ban.GetComponent<NavMeshObstacle>(), "Bancone_Mesh: NavMeshObstacle da mantenere");
        foreach (var name in new[] { "Scaffale_A", "Scaffale_B" })
        {
            var sc = GameObject.Find(name); if (!sc) continue;
            Assert.IsNotNull(sc.GetComponent<Collider>(), name + ": il secondo collider resta (regressione #58)");
            Assert.IsNotNull(sc.GetComponent<NavMeshObstacle>(), name + ": NavMeshObstacle da mantenere");
        }
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator Nina_PassesThroughArches()
    {
        yield return Load();
        var nina = GameRoot.I.Nina; Teleport(nina, new Vector3(6, 0.05f, -9)); yield return null;
        int arches = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (!t.name.StartsWith("env_wall_arch_4m")) continue;
            var cols = t.GetComponents<BoxCollider>(); Assert.AreEqual(3, cols.Length, t.name + ": servono 3 BoxCollider (pilastri + architrave)");
            var rb = RendBounds(t.gameObject); bool alongX = rb.size.x >= rb.size.z; arches++;
            var dir = alongX ? Vector3.forward : Vector3.right; float th = alongX ? rb.extents.z : rb.extents.x;
            var start = new Vector3(rb.center.x, 0.05f, rb.center.z) - dir * (th + 1.0f);
            var hits = Physics.CapsuleCastAll(start + Vector3.up * 0.35f, start + Vector3.up * 1.4f, 0.3f, dir, 2f * (th + 1.0f), ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits) Assert.IsFalse(h.collider.transform == t, $"{t.name} @ {rb.center}: Nina (r=0.3) non passa sotto l'arco");
        }
        Assert.GreaterOrEqual(arches, 1, "nessun arco in scena");
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator Nina_ReachesZonaBancone_NoPocketBehind()
    {
        yield return Load();
        var zone = GameObject.Find("Zona_Bancone"); Assert.IsNotNull(zone); var ban = zone.transform.position;
        // faccia del muro dietro il Bancone a >= +1.06 (Nina r=0.3 entra nella zona) e a ridosso (nessuna sacca oltre)
        float face = 99f;
        foreach (var bc in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if (!bc.name.StartsWith("env_wall_2m")) continue; var b = bc.bounds;
            if (b.max.x < ban.x - 1.2f || b.min.x > ban.x + 1.2f || b.center.z < ban.z || b.center.z > ban.z + 4f) continue;
            face = Mathf.Min(face, b.min.z - ban.z);
        }
        Assert.GreaterOrEqual(face, 1.15f, "muro dietro il Bancone troppo vicino");
        Assert.LessOrEqual(face, 1.3f, "sacca dietro il Bancone: muro a " + face);
        // Nina arriva in Zona_Bancone seguendo il NavMesh
        var nina = GameRoot.I.Nina; Teleport(nina, ban + new Vector3(2.2f, 0.05f, -2.4f)); yield return null; var from = nina.transform.position;
        var path = new NavMeshPath();
        bool okA = NavMesh.SamplePosition(from, out var a, 2f, NavMesh.AllAreas), okZ = NavMesh.SamplePosition(ban, out var z, 1f, NavMesh.AllAreas);
        Assert.IsTrue(okA && okZ, "NavMesh assente");
        Assert.IsTrue(NavMesh.CalculatePath(a.position, z.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete, "Zona_Bancone non raggiungibile: " + path.status);
        // poi a piedi (CharacterController r=0.3): dal corridoio tra Bancone e arco del set, passando tra Bancone e vaso
        var wps = new[] { ban + new Vector3(2.2f, 0, -0.1f), ban };
        int k = 0;
        for (float t = 0; t < 15f && k < wps.Length; t += Time.deltaTime)
        {
            var d = wps[k] - nina.transform.position; d.y = 0;
            if (d.magnitude < 0.15f) { k++; continue; }
            nina.ScriptedInput = d.normalized; yield return null;
        }
        nina.ScriptedInput = Vector3.zero;
        var p = nina.transform.position;
        Assert.Less(new Vector2(p.x - ban.x, p.z - ban.z).magnitude, 0.6f, "Nina bloccata prima di Zona_Bancone: " + p + " da " + from + " waypoint " + k);
    }

    // #97: varco ovest del Bancone (tra vaso di limoni e pila di pacchi / Bancone): chiuso (<0.6, Nina non entra) oppure aperto >= 0.8 m
    [UnityTest, Timeout(180000)]
    public IEnumerator BanconeWestGap_ClosedOrWide_NinaFromWest()
    {
        yield return Load();
        var ban = GameObject.Find("Zona_Bancone").transform.position;
        Bounds? lemon = null, stack = null, counter = null;
        foreach (var bc in Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            var b = bc.bounds; var r = b.center - ban; if (r.x > -0.9f || r.x < -2.6f || r.z < -2f || r.z > 1.5f) { if (bc.name.StartsWith("prop_counter_2m") && Mathf.Abs(r.x) < 0.5f) counter = b; continue; }
            if (bc.name.StartsWith("prop_lemontree_pot")) lemon = b; else if (bc.name.StartsWith("prop_parcel_stack_a")) stack = b;
        }
        Assert.IsTrue(lemon.HasValue && stack.HasValue && counter.HasValue, "vaso/pila/Bancone non trovati");
        float gapZ = stack.Value.min.z - lemon.Value.max.z;           // vaso <-> pila
        float gapX = lemon.Value.min.z < counter.Value.max.z ? counter.Value.min.x - lemon.Value.max.x : 99f; // vaso <-> Bancone
        float gap = Mathf.Min(gapZ, gapX);
        Debug.Log($"[#97] varco ovest: vaso-pila {gapZ:F2} vaso-Bancone {gapX:F2}");
        Assert.IsTrue(gap < 0.6f || gap >= 0.8f, $"varco ovest ambiguo {gap:F2} m: chiuso (<0.6) o aperto (>=0.8)");
        var nina = GameRoot.I.Nina;
        if (gap < 0.6f)
        {
            // chiuso davvero: Nina spinta da ovest non entra nella zona
            Teleport(nina, ban + new Vector3(-3.0f, 0.05f, -0.15f)); yield return null;
            nina.ScriptedInput = Vector3.right; for (float t = 0; t < 2.5f; t += Time.deltaTime) yield return null; nina.ScriptedInput = Vector3.zero;
            Assert.Less(nina.transform.position.x - ban.x, -1.2f, "varco ovest dichiarato chiuso ma Nina passa");
            yield break;
        }
        Teleport(nina, ban + new Vector3(-3.0f, 0.05f, (lemon.Value.max.z + stack.Value.min.z) * 0.5f - ban.z)); yield return null;
        nina.ScriptedInput = Vector3.right; for (float t = 0; t < 4f && nina.transform.position.x < ban.x; t += Time.deltaTime) yield return null; nina.ScriptedInput = Vector3.zero;
        Assert.Greater(nina.transform.position.x - ban.x, -0.6f, "Nina non entra da ovest: " + nina.transform.position);
    }
}
