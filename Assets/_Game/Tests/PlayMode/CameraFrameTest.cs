using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

/// <summary>QA v6: a idle vicino ai bordi Arrivi/Bancone, dopo 0.5 s Nina resta nel 70% centrale dello schermo.</summary>
public class CameraFrameTest
{
    [UnityTest, Timeout(120000)]
    public IEnumerator IdleNearEdges_KeepsNinaInCentralViewport()
    {
        PlayerPrefs.DeleteKey(UltimoMiglio.Gameplay.TutorialManager.PrefKey); PlayerPrefs.DeleteKey("um_save_v1"); PlayerPrefs.DeleteKey(UltimoMiglio.UI.CameraView.PrefZoom); UltimoMiglio.Gameplay.TutorialManager.TestSenzaComparse = true; UltimoMiglio.Gameplay.TutorialManager.TestAccelera = true; /* #101: niente comparse graduali, tempi corti */ // solo chiavi di gioco: vol_music e vol_sfx restano (come #88)
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        PlayerPrefs.SetInt(UltimoMiglio.Gameplay.TutorialManager.PrefGraduale, 0); // #94: test non di onboarding → niente rivelazione graduale
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 90 && !GameRoot.Ready; i++) yield return null;
        Assert.IsTrue(GameRoot.Ready, "GameRoot not ready");
        Assert.IsNotNull(GameRoot.I.Nina, "Nina missing");

        var tut = Object.FindAnyObjectByType<TutorialManager>();
        Assert.IsNotNull(tut?.Arrivi, "Arrivi missing");
        Assert.IsNotNull(tut.Bancone, "Bancone missing");

        var fit = Object.FindAnyObjectByType<DepotCameraFit>();
        Assert.IsNotNull(fit, "DepotCameraFit missing");
        fit.Nina = GameRoot.I.Nina.transform;

        var cam = Camera.main;
        Assert.IsNotNull(cam, "Camera.main missing");

        // Posizioni mondo esplicite (SliceSceneBuilder: arrPos≈(-5,0,6), bancone a sud)
        var arrivi = tut.Arrivi.position;
        if (arrivi.sqrMagnitude < 0.01f)
        {
            var z = GameObject.Find("Zona_Arrivi");
            Assert.IsNotNull(z, "Zona_Arrivi missing");
            arrivi = z.transform.position;
        }
        var bancone = tut.Bancone.position;
        if ((bancone - arrivi).sqrMagnitude < 0.01f)
        {
            var z = GameObject.Find("Zona_Bancone");
            Assert.IsNotNull(z, "Zona_Bancone missing");
            bancone = z.transform.position;
        }

        yield return AssertIdleFramed(GameRoot.I.Nina, arrivi, cam, "Arrivi");
        yield return AssertIdleFramed(GameRoot.I.Nina, bancone, cam, "Bancone");
    }

    static IEnumerator AssertIdleFramed(NinaController nina, Vector3 near, Camera cam, string label)
    {
        nina.ScriptedInput = Vector3.zero;
        var p = near; p.y = Mathf.Max(0.05f, nina.transform.position.y);
        // CharacterController ignora transform.position se abilitato
        var cc = nina.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        nina.transform.position = p;
        if (cc) cc.enabled = true;
        Physics.SyncTransforms();

        yield return null;
        float t = 0f;
        while (t < 0.5f)
        {
            nina.ScriptedInput = Vector3.zero;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Assert.That((nina.transform.position - p).magnitude, Is.LessThan(1.5f),
            $"{label}: Nina non è restata al bordo (want≈{p} got={nina.transform.position})");
        var vp = cam.WorldToViewportPoint(nina.transform.position + Vector3.up * 0.9f);
        Debug.Log($"[CameraFrame] {label} want={p} pos={nina.transform.position} vp={vp}");
        Assert.Greater(vp.z, 0f, $"{label}: dietro la camera");
        Assert.That(vp.x, Is.InRange(0.15f, 0.85f), $"{label}: vp.x={vp.x:F3} fuori [0.15..0.85]");
        Assert.That(vp.y, Is.InRange(0.15f, 0.85f), $"{label}: vp.y={vp.y:F3} fuori [0.15..0.85]");
    }
}
