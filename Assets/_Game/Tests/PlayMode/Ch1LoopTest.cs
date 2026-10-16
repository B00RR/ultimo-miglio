using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

public class Ch1LoopTest
{
    [UnityTest, Timeout(1000000)]
    public IEnumerator Chapter1Loop_PickStackDeliverServeBuyZaino()
    {
        PlayerPrefs.DeleteKey(UltimoMiglio.Gameplay.TutorialManager.PrefKey); PlayerPrefs.DeleteKey("um_save_v1"); PlayerPrefs.DeleteKey(UltimoMiglio.Gameplay.DepotCameraFit.PrefZoom); // solo chiavi di gioco: vol_music e vol_sfx restano (come #88)
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        Time.timeScale = 3f;
        new GameObject("LoopAutoTest").AddComponent<LoopAutoTest>();
        float t = 0; while (LoopAutoTest.Result == null && t < 900) { t += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = 1f;
        Assert.AreEqual("PASS", LoopAutoTest.Result);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, LoopAutoTest.TutSteps, "tutorial steps out of order: " + string.Join(",", LoopAutoTest.TutSteps));
        Assert.AreEqual(1, PlayerPrefs.GetInt(TutorialManager.PrefKey) == (int)TutorialManager.Step.Chiuso ? 1 : 0, "tutorial state not saved");
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator Boundary_NinaCannotLeaveMap()
    {
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        Time.timeScale = 3f;
        while (!GameRoot.Ready || !GameRoot.I.Nina) yield return null;
        var n = GameRoot.I.Nina;
        var dirs = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left, new Vector3(1,0,1).normalized, new Vector3(-1,0,-1).normalized, new Vector3(1,0,-1).normalized, new Vector3(-1,0,1).normalized };
        foreach (var d in dirs)
        {
            n.ScriptedInput = d; float t = 0; while (t < 10f) { t += Time.deltaTime; yield return null; }
            var p = n.transform.position; Debug.Log($"[BoundTest] dir={d} pos={p}");
            Assert.IsTrue(Mathf.Abs(p.x) < 9.2f && Mathf.Abs(p.z) < 11.2f && p.y > -0.5f, $"Nina outside map: {p}");
        }
        n.ScriptedInput = Vector3.zero; Time.timeScale = 1f;
    }
}
