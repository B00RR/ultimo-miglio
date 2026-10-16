using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Core;
using UltimoMiglio.Gameplay;

/// <summary>#59 (checklist A01/A03/A05): nessun personaggio cammina all'indietro nel gioco vero.
/// Non basta transform.forward: misura la posa VISIVA (forche/muso di NONNO, bacino Humanoid),
/// il verso di rotolamento delle ruote e lo scivolamento del piede d'appoggio (deve andare all'indietro).</summary>
public class AnimFacingTest
{
    sealed class Probe
    {
        public readonly List<float> Fwd = new(), Visual = new(), Spin = new(), Foot = new();
        public string Report(string who) =>
            $"{who}: n={Fwd.Count} fwd·v={Avg(Fwd):F2} visivo·v={Avg(Visual):F2} ruote/piede={(Spin.Count > 0 ? Avg(Spin) : Avg(Foot)):F3}";
        public static float Avg(List<float> l) => l.Count == 0 ? float.NaN : l.Average();
    }

    /// <summary>Cancella solo le chiavi di gioco (salvataggio + tutorial), non volumi/zoom delle impostazioni (#36).</summary>
    internal static void ResetGameKeys() { PlayerPrefs.DeleteKey("um_save_v1" /* GameRoot.SaveKey */); PlayerPrefs.DeleteKey(TutorialManager.PrefKey); PlayerPrefs.Save(); }
    static IEnumerator Load()
    {
        ResetGameKeys(); Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 60 && !GameRoot.Ready; i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot non pronto");
    }

    static Transform Find(Transform r, string n) => r.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n);
    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

    // Humanoid: bacino (visivo) + piede più basso in spazio personaggio: in appoggio deve scivolare verso -Z locale.
    static void SampleHumanoid(Transform root, Animator an, Vector3 vel, Probe p, ref Vector3? lastFootLocal, ref int lastFoot)
    {
        var v = Flat(vel).normalized;
        p.Fwd.Add(Vector3.Dot(Flat(root.forward).normalized, v));
        var hips = an.GetBoneTransform(HumanBodyBones.Hips); var lf = an.GetBoneTransform(HumanBodyBones.LeftFoot); var rf = an.GetBoneTransform(HumanBodyBones.RightFoot);
        var lu = an.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var ru = an.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        if (lu && ru) p.Visual.Add(Vector3.Dot(Flat(Vector3.Cross(ru.position - lu.position, Vector3.up)).normalized, v)); // bacino: destra x su = avanti
        if (!lf || !rf || !hips) return;
        int f = lf.position.y <= rf.position.y ? 0 : 1; var foot = f == 0 ? lf : rf;
        var local = root.InverseTransformPoint(foot.position);
        if (lastFootLocal.HasValue && lastFoot == f && Mathf.Abs(foot.position.y - Mathf.Min(lf.position.y, rf.position.y)) < 1e-4f)
            p.Foot.Add(local.z - lastFootLocal.Value.z);
        lastFootLocal = local; lastFoot = f;
    }

    [UnityTest] public IEnumerator Nina_CamminaInAvanti()
    {
        yield return Load();
        var n = Object.FindAnyObjectByType<NinaController>(); Assert.IsNotNull(n); Assert.IsNotNull(n.Animator, "Nina senza Animator (graybox)");
        foreach (var dir in new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
        {
            var p = new Probe(); Vector3? lf = null; int lfi = -1;
            n.ScriptedInput = dir; var last = n.transform.position; float t = 0;
            while (t < 1.6f)
            {
                yield return null; t += Time.deltaTime;
                var vel = (n.transform.position - last) / Mathf.Max(Time.deltaTime, 1e-4f); last = n.transform.position;
                if (t > 0.5f && Flat(vel).magnitude > 0.5f) SampleHumanoid(n.transform, n.Animator, vel, p, ref lf, ref lfi);
            }
            Debug.Log("[AnimFacing] " + p.Report($"Nina {dir}"));
            Assert.Greater(p.Fwd.Count, 10, $"Nina {dir}: non si muove");
            Assert.Greater(Probe.Avg(p.Fwd), 0.9f, p.Report($"Nina {dir}"));
            Assert.Greater(Probe.Avg(p.Visual), 0.7f, "posa visiva all'indietro " + p.Report($"Nina {dir}"));
            if (p.Foot.Count > 5) Assert.Less(Probe.Avg(p.Foot), 0f, "piede d'appoggio scivola in avanti (ciclo al contrario) " + p.Report($"Nina {dir}"));
        }
        n.ScriptedInput = Vector3.zero;
    }

    [UnityTest] public IEnumerator Clienti_CamminanoInAvanti()
    {
        yield return Load();
        var q = Object.FindAnyObjectByType<CustomerQueue>();
        Debug.Log($"[AnimFacing] coda={(bool)q} prefab={(q ? (bool)q.Prefab : false)} door={(q ? (bool)q.Door : false)} slot={(q ? (bool)q.CounterSlot : false)} active={(q ? q.isActiveAndEnabled : false)}");
        Assert.IsNotNull(q, "CustomerQueue mancante");
        var p = new Probe(); var last = new Dictionary<Customer, Vector3>(); var lf = new Dictionary<Customer, (Vector3?, int)>();
        float t = 0;
        while (t < 20f && p.Fwd.Count < 60)
        {
            yield return null; t += Time.deltaTime;
            foreach (var c in Object.FindObjectsByType<Customer>(FindObjectsSortMode.None))
            {
                var an = c.GetComponentInChildren<Animator>(); var pos = c.transform.position;
                if (last.TryGetValue(c, out var lp) && an && an.isHuman)
                {
                    var vel = (pos - lp) / Mathf.Max(Time.deltaTime, 1e-4f);
                    if (Flat(vel).magnitude > 0.5f) { lf.TryGetValue(c, out var s); var a = s.Item1; var b = s.Item2; SampleHumanoid(c.transform, an, vel, p, ref a, ref b); lf[c] = (a, b); }
                }
                last[c] = pos;
            }
        }
        var all = Object.FindObjectsByType<Customer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[AnimFacing] clienti={all.Length} humanoid={all.Count(c => c.GetComponentInChildren<Animator>() is { isHuman: true })} " + p.Report("Clienti"));
        Assert.Greater(p.Fwd.Count, 10, "nessun cliente Humanoid in cammino (capsule?)");
        Assert.Greater(Probe.Avg(p.Fwd), 0.9f, p.Report("Clienti"));
        Assert.Greater(Probe.Avg(p.Visual), 0.7f, "posa visiva all'indietro " + p.Report("Clienti"));
        if (p.Foot.Count > 5) Assert.Less(Probe.Avg(p.Foot), 0f, "piede scivola in avanti " + p.Report("Clienti"));
    }

    [UnityTest] public IEnumerator Nonno_ForcheEMusoVersoLaMarcia()
    {
        yield return Load();
        var eco = GameRoot.I.Eco; eco.S.Wallet = 100;
        bool done = eco.S.NonnoLevel >= 0; for (int i = 0; i < 500 && !done; i++) done = eco.PadTick(UpgradeKind.NonnoHire, i * 0.1);
        Assert.IsTrue(done, "assunzione NONNO fallita");
        var bot = Object.FindAnyObjectByType<NonnoRobot>(); Assert.IsNotNull(bot); Assert.IsNotNull(bot.Animator, "NONNO senza rig");
        if (bot.Arrivals) bot.Arrivals.Stock = 20;
        var root = bot.Animator.transform; var forks = Find(root, "forks"); var body = Find(root, "body") ?? root; var wheel = Find(root, "wheels_front");
        Assert.IsNotNull(forks, "osso forks mancante");
        var p = new Probe(); var last = bot.transform.position; Vector3? wl = null; Vector3 wPrevOff = Vector3.zero; float t = 0;
        while (t < 20f && p.Fwd.Count < 120)
        {
            yield return null; t += Time.deltaTime;
            var vel = (bot.transform.position - last) / Mathf.Max(Time.deltaTime, 1e-4f); last = bot.transform.position;
            var v = Flat(vel).normalized; bool moving = Flat(vel).magnitude > 1f;
            if (wheel)
            {
                // verso di rotolamento: con la ruota che avanza, Cross(v_prima, v_dopo) di un raggio punta a +right
                var ray = Vector3.ProjectOnPlane(wheel.TransformDirection(wl ?? (Vector3)(wl = wheel.InverseTransformDirection(Vector3.up))), bot.transform.right);
                if (moving && wPrevOff != Vector3.zero) { float w = Vector3.Dot(Vector3.Cross(wPrevOff, ray), bot.transform.right); if (Mathf.Abs(w) > 1e-4f) p.Spin.Add(Mathf.Sign(w)); }
                wPrevOff = ray;
            }
            if (!moving) continue;
            if (p.Fwd.Count % 15 == 0) { var ci = bot.Animator.GetCurrentAnimatorClipInfo(0); Debug.Log($"[AnimFacing] NONNO stato={bot.State} clip={(ci.Length > 0 ? ci[0].clip.name : "-")} ruota={(wheel ? wheel.localEulerAngles.ToString() : "-")} speed={bot.Animator.GetFloat("Speed")}"); }
            p.Fwd.Add(Vector3.Dot(Flat(bot.transform.forward).normalized, v));
            p.Visual.Add(Vector3.Dot(Flat(forks.position - body.position).normalized, v));
        }
        Debug.Log("[AnimFacing] " + p.Report("NONNO"));
        Assert.Greater(p.Fwd.Count, 20, "NONNO non ha camminato abbastanza (strip <20 frame)");
        Assert.Greater(Probe.Avg(p.Fwd), 0.9f, p.Report("NONNO"));
        Assert.Greater(Probe.Avg(p.Visual), 0.7f, "forche dietro rispetto alla marcia (retromarcia) " + p.Report("NONNO"));
        Assert.Greater(p.Spin.Count, 20, "ruote ferme mentre NONNO guida (Drive non in riproduzione) " + p.Report("NONNO"));
        Assert.Greater(Probe.Avg(p.Spin), 0.5f, "ruote girano al contrario o ferme mentre guida " + p.Report("NONNO"));
    }
}

/// <summary>#2: in scena i clienti sono i 6 personaggi riggati (non capsule), Humanoid valido, AlwaysAnimate, senza root motion.</summary>
public class ClientiRigTest
{
    [UnityTest] public IEnumerator Clienti_SonoIRiggatiNonCapsule()
    {
        // Solo le chiavi del gioco: i volumi (vol_music/vol_sfx, #36) restano intatti
        PlayerPrefs.DeleteKey(TutorialManager.PrefKey); PlayerPrefs.DeleteKey("um_save_v1"); PlayerPrefs.DeleteKey(DepotCameraFit.PrefZoom);
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 60 && !GameRoot.Ready; i++) yield return null;
        var q = Object.FindAnyObjectByType<CustomerQueue>(); Assert.IsNotNull(q); Assert.IsNotNull(q.Prefab, "CustomerQueue.Prefab rotto");
        Assert.IsNotNull(q.Variants); Assert.AreEqual(6, q.Variants.Length, "servono le 6 varianti");
        foreach (var v in q.Variants)
        {
            Assert.IsNotNull(v, "variante nulla");
            Assert.IsNull(v.GetComponentInChildren<MeshFilter>(), $"{v.name}: è ancora una capsula");
            var an = v.GetComponentInChildren<Animator>(); Assert.IsNotNull(an, v.name);
            Assert.IsTrue(an.avatar && an.avatar.isValid && an.avatar.isHuman, $"{v.name}: avatar Humanoid non valido");
            Assert.AreEqual("chr_cliente", an.runtimeAnimatorController ? an.runtimeAnimatorController.name : "", v.name);
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, an.cullingMode, v.name);
            Assert.IsFalse(an.applyRootMotion, v.name);
        }
    }
}
