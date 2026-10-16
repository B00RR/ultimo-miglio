using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

/// <summary>#118: Nonna Ada 3D è in scena, scala 0.88, Animator Humanoid col controller di #93, Idle all'avvio e Talk al cambio passo del tutorial.</summary>
public class AdaSceneTest
{
    [UnityTest] public IEnumerator Ada_InScena_IdleEPoiTalk()
    {
        PlayerPrefs.DeleteKey(TutorialManager.PrefKey);
        PlayerPrefs.SetInt(TutorialManager.PrefGraduale, 0);
        yield return SceneManager.LoadSceneAsync("Ch1_Deposito");
        for (int i = 0; i < 30 && !GameRoot.Ready; i++) yield return null;
        var ada = Object.FindAnyObjectByType<AdaPresence>();
        Assert.IsNotNull(ada, "Nonna Ada (AdaPresence) non è in scena");
        Assert.AreEqual(0.88f, ada.transform.lossyScale.y, 0.01f, "scala root di Ada");
        var an = ada.Animator;
        Assert.IsNotNull(an, "Animator mancante");
        Assert.IsNotNull(an.runtimeAnimatorController, "controller mancante");
        Assert.IsTrue(an.avatar && an.avatar.isHuman, "avatar non Humanoid");
        Assert.AreEqual(0f, an.GetFloat("Speed"), 0.001f, "Ada deve partire ferma (Idle)");
        Assert.IsNotNull(ada.GetComponent<Collider>(), "Ada senza collider");
        TutorialManager.StepChanged?.Invoke(TutorialManager.Step.PickUp) /* #101: ex Arrivi */;
        yield return null; yield return null;
        Assert.IsTrue(an.GetBool("Talk"), "Talk non attivato dal cambio passo del tutorial");
    }
}
