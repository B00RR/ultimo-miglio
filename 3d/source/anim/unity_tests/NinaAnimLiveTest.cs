using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

// Verifica in Play Mode, nella scena di gioco vera, che le animazioni di Nina siano vive:
// controller e avatar assegnati, ossa che cambiano tra i frame, Speed>0 -> Walk/Run, piede d'appoggio che va indietro.
// Non cambia il cullingMode: deve funzionare come nel gioco (camera vera della scena).
public class NinaAnimLiveTest
{
    [UnityTest, Timeout(300000)]
    public IEnumerator Nina_AnimazioniVive_NelGioco()
    {
        PlayerPrefs.DeleteAll();
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        while (!GameRoot.Ready || !GameRoot.I.Nina) yield return null;
        var n = GameRoot.I.Nina; var an = n.Animator;
        Assert.IsNotNull(an, "NinaController.Animator non assegnato");
        Assert.IsNotNull(an.runtimeAnimatorController, "controller non assegnato");
        Assert.IsNotNull(an.avatar, "avatar non assegnato"); Assert.IsTrue(an.avatar.isValid && an.avatar.isHuman, "avatar non valido/non humanoid");
        Debug.Log($"[NinaLive] controller={an.runtimeAnimatorController.name} avatar={an.avatar.name} culling={an.cullingMode} enabled={an.enabled}");
        foreach (var r in an.GetComponentsInChildren<Renderer>(true)) Debug.Log($"[NinaLive] renderer {r.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} visible={r.isVisible} bounds={r.bounds.center}/{r.bounds.size} cams={Camera.allCamerasCount} main={(Camera.main ? Camera.main.name : "none")}");
        if (System.Environment.GetEnvironmentVariable("UM_ALWAYS") == "1") an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var lu = an.GetBoneTransform(HumanBodyBones.LeftUpperLeg); var ru = an.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        var hips = an.GetBoneTransform(HumanBodyBones.Hips); var lf = an.GetBoneTransform(HumanBodyBones.LeftFoot); var rf = an.GetBoneTransform(HumanBodyBones.RightFoot);
        foreach (var (mag, expect) in new[] { (0.4f, "Walk"), (1f, "Run") })
        {
            var dir = Vector3.forward;
            var cc = n.GetComponent<CharacterController>(); cc.enabled = false; n.transform.position = new Vector3(0, 0.05f, -4f); cc.enabled = true;
            n.ScriptedInput = dir * mag; float t = 0; while (t < 1.0f) { t += Time.deltaTime; yield return null; }
            var q0 = lu.localRotation; float maxAng = 0, footSum = 0; int k = 0; Vector3 p0 = n.transform.position;
            Vector3 pl = lf.position - hips.position, pr = rf.position - hips.position;
            while (t < 2.0f)
            {
                yield return null; t += Time.deltaTime; k++;
                maxAng = Mathf.Max(maxAng, Quaternion.Angle(q0, lu.localRotation));
                var nl = lf.position - hips.position; var nr = rf.position - hips.position;
                var vel = (n.transform.position - p0); vel.y = 0;
                var stance = lf.position.y < rf.position.y ? (nl - pl) : (nr - pr);
                footSum += Vector3.Dot(stance, dir); pl = nl; pr = nr;
            }
            var moved = n.transform.position - p0;
            var clips = an.GetCurrentAnimatorClipInfo(0); var top = clips.OrderByDescending(c => c.weight).FirstOrDefault();
            Debug.Log($"[NinaLive] input={mag} speed={an.GetFloat("Speed"):0.00} moved={moved.z:0.00}m clip={top.clip?.name}({top.weight:0.00}) legSwing={maxAng:0.0}deg stanceFootAlongDir={footSum:0.000}");
            Assert.Greater(an.GetFloat("Speed"), 0.5f, "Speed non aggiornato");
            Assert.Greater(moved.z, 0.5f, "Nina non avanza su +Z");
            Assert.Greater(maxAng, 10f, "ossa ferme: animazione non viva");
            StringAssert.Contains(expect, top.clip.name, $"clip dominante attesa {expect}");
            Assert.Less(footSum, 0f, "piede d'appoggio non va indietro rispetto al moto");
        }
        n.ScriptedInput = Vector3.zero;
    }
}
