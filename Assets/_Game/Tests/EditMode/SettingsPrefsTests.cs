using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.Gameplay;

public class SettingsPrefsTests
{
    const string PrefMusic = "vol_music", PrefSfx = "vol_sfx";

    [TearDown] public void TearDown()
    {
        PlayerPrefs.DeleteKey(UltimoMiglio.UI.CameraView.PrefZoom);
        PlayerPrefs.DeleteKey(PrefMusic);
        PlayerPrefs.DeleteKey(PrefSfx);
        PlayerPrefs.Save();
    }

    [Test] public void CamZoom_RoundTripsThroughPlayerPrefs()
    {
        PlayerPrefs.SetFloat(UltimoMiglio.UI.CameraView.PrefZoom, 14.5f); PlayerPrefs.Save();
        Assert.AreEqual(14.5f, PlayerPrefs.GetFloat(UltimoMiglio.UI.CameraView.PrefZoom, 0f), 1e-4f);
    }

    [Test] public void Volumes_RoundTripAndDefaults()
    {
        Assert.AreEqual(0.6f, PlayerPrefs.GetFloat(PrefMusic, 0.6f), 1e-4f);
        PlayerPrefs.SetFloat(PrefMusic, 0.3f);
        PlayerPrefs.SetFloat(PrefSfx, 0.8f); PlayerPrefs.Save();
        Assert.AreEqual(0.3f, PlayerPrefs.GetFloat(PrefMusic), 1e-4f);
        Assert.AreEqual(0.8f, PlayerPrefs.GetFloat(PrefSfx), 1e-4f);
    }
}
