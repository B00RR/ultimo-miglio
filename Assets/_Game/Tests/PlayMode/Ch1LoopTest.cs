using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

public class Ch1LoopTest
{
    [UnityTest, Timeout(600000)]
    public IEnumerator Chapter1Loop_PickStackDeliverServeBuyZaino()
    {
        PlayerPrefs.DeleteAll();
        var save = System.IO.Path.Combine(Application.persistentDataPath, "save_v1.json"); if (System.IO.File.Exists(save)) System.IO.File.Delete(save);
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        Time.timeScale = 3f;
        new GameObject("LoopAutoTest").AddComponent<LoopAutoTest>();
        float t = 0; while (LoopAutoTest.Result == null && t < 500) { t += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = 1f;
        Assert.AreEqual("PASS", LoopAutoTest.Result);
    }
}
