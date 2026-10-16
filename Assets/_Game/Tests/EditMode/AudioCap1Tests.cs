using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UltimoMiglio.Gameplay;

/// <summary>Audit audio cap.1: clip cablate nella scena e volumi Impostazioni applicati.</summary>
public class AudioCap1Tests
{
    [Test] public void Scene_AudioManager_HasAllClips()
    {
        LogAssert.ignoreFailingMessages = true; // OnValidate di altri componenti della scena loggano errori non audio all'apertura
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Ch1_Deposito.unity", OpenSceneMode.Single);
        var am = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        Assert.IsNotNull(am, "AudioManager assente nella scena");
        foreach (var c in am.MusicStems) Assert.IsNotNull(c, "stem musicale");
        Assert.IsNotNull(am.MusicFull);
        Assert.AreEqual(8, am.Pickup.Length); foreach (var c in am.Pickup) Assert.IsNotNull(c);
        Assert.GreaterOrEqual(am.Steps.Length, 4, "passi"); Assert.GreaterOrEqual(am.Deposit.Length, 4, "deposito");
        Assert.GreaterOrEqual(am.CoinDrop.Length, 4); Assert.GreaterOrEqual(am.CoinPickup.Length, 4);
        Assert.IsNotNull(am.Served, "Bancone"); Assert.IsNotNull(am.Bell);
        Assert.GreaterOrEqual(am.UiTap.Length, 1, "UI tap"); Assert.IsNotNull(am.TutorialArrow); Assert.IsNotNull(am.TutorialStep);
    }

    [Test] public void Volumes_ClampAndApply()
    {
        var go = new GameObject("am"); var am = go.AddComponent<AudioManager>();
        am.SetMusicVolume(1.7f); Assert.AreEqual(1f, am.MusicVolume, 1e-4f);
        am.SetMusicVolume(0.25f); Assert.AreEqual(0.25f, am.MusicVolume, 1e-4f);
        am.SetSfxVolume(-1f); Assert.AreEqual(0f, am.SfxVolume, 1e-4f);
        am.SetSfxVolume(0.4f); Assert.AreEqual(0.4f, am.SfxVolume, 1e-4f);
        Object.DestroyImmediate(go);
    }

    [Test] public void PrefKeys_MatchSettingsPanel()
    {
        Assert.AreEqual("vol_music", AudioManager.PrefMusic); Assert.AreEqual("vol_sfx", AudioManager.PrefSfx);
    }
}
