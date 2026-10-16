using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>P0 #58/#59: Nina non attraversa muri/oggetti/bordi; NONNO, clienti e Nina guardano dove camminano.</summary>
public class P0CollisionFacingTest
{
    static IEnumerator Load()
    {
        PlayerPrefs.DeleteAll();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        PlayerPrefs.SetInt(UltimoMiglio.Gameplay.TutorialManager.PrefGraduale, 0); // #94: test non di onboarding → niente rivelazione graduale
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
}
