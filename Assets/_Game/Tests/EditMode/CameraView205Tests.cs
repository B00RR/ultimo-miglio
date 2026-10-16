using NUnit.Framework;
using UnityEngine;
using UltimoMiglio.UI;

/// <summary>#205 telecamera regolabile: clamp, scatti, salvataggio e caricamento (cam_ver diversa → predefiniti), blocchi del tutorial.</summary>
public class CameraView205Tests
{
    static void Clear()
    {
        foreach (var k in new[] { CameraView.PrefZoom, CameraView.PrefRot, CameraView.PrefTilt, CameraView.PrefVer, CameraView.LegacyPrefZoom }) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
    }

    [SetUp] public void SetUp() { Clear(); CameraView.Tutorial = false; CameraView.Pinching = false; CameraView.SetForTest(1f, 0f, CameraView.DefaultTilt); }
    [TearDown] public void TearDown() { Clear(); CameraView.Tutorial = false; CameraView.Pinching = false; CameraView.SetForTest(1f, 0f, CameraView.DefaultTilt); }

    [Test] public void Zoom_ClampAndSnap()
    {
        CameraView.SetZoom(0.2f); Assert.AreEqual(0.7f, CameraView.Zoom, 1e-4f);
        CameraView.SetZoom(3f); Assert.AreEqual(1.4f, CameraView.Zoom, 1e-4f);
        CameraView.SetZoom(1.02f); Assert.AreEqual(1.0f, CameraView.Zoom, 1e-4f);
        CameraView.SetZoom(1.03f); Assert.AreEqual(1.05f, CameraView.Zoom, 1e-4f);
    }

    [Test] public void Rotation_ClampAndSnap15()
    {
        CameraView.SetRot(-90f); Assert.AreEqual(-45f, CameraView.Rot, 1e-4f);
        CameraView.SetRot(90f); Assert.AreEqual(45f, CameraView.Rot, 1e-4f);
        CameraView.SetRot(8f); Assert.AreEqual(15f, CameraView.Rot, 1e-4f);
        CameraView.SetRot(-7f); Assert.AreEqual(0f, CameraView.Rot, 1e-4f);
        CameraView.SetRot(-23f); Assert.AreEqual(-30f, CameraView.Rot, 1e-4f);
    }

    [Test] public void Tilt_ClampAndSnap5()
    {
        CameraView.SetTilt(10f); Assert.AreEqual(35f, CameraView.Tilt, 1e-4f);
        CameraView.SetTilt(80f); Assert.AreEqual(60f, CameraView.Tilt, 1e-4f);
        CameraView.SetTilt(47.4f); Assert.AreEqual(45f, CameraView.Tilt, 1e-4f);
        CameraView.SetTilt(48f); Assert.AreEqual(50f, CameraView.Tilt, 1e-4f);
    }

    [Test] public void Pinch_OvershootsThenSettlesInsideLimits()
    {
        CameraView.Pinching = true; CameraView.SetZoomPinch(5f);
        Assert.Greater(CameraView.EffectiveZoom, CameraView.ZoomMax, "rimbalzo: durante il pinch si va un po' oltre il limite");
        Assert.LessOrEqual(CameraView.EffectiveZoom, CameraView.ZoomMax + CameraView.PinchOvershoot + 1e-4f);
        CameraView.EndPinch();
        Assert.AreEqual(CameraView.ZoomMax, CameraView.Zoom, 1e-4f);
        Assert.AreEqual(CameraView.Version, PlayerPrefs.GetInt(CameraView.PrefVer, 0), "salvataggio al rilascio del pinch");
    }

    [Test] public void SaveLoad_RoundTrip()
    {
        CameraView.SetZoom(0.85f); CameraView.SetRot(-30f); CameraView.SetTilt(40f); CameraView.Save();
        CameraView.SetForTest(1f, 0f, CameraView.DefaultTilt);
        CameraView.Load();
        Assert.AreEqual(0.85f, CameraView.Zoom, 1e-4f); Assert.AreEqual(-30f, CameraView.Rot, 1e-4f); Assert.AreEqual(40f, CameraView.Tilt, 1e-4f);
        Assert.AreEqual(1, PlayerPrefs.GetInt(CameraView.PrefVer));
    }

    [TestCase(0)] [TestCase(2)] [TestCase(-1)]
    public void Load_WrongVersion_UsesDefaults(int ver)
    {
        PlayerPrefs.SetFloat(CameraView.PrefZoom, 1.4f); PlayerPrefs.SetFloat(CameraView.PrefRot, 45f); PlayerPrefs.SetFloat(CameraView.PrefTilt, 60f);
        PlayerPrefs.SetInt(CameraView.PrefVer, ver); PlayerPrefs.Save();
        CameraView.Load();
        Assert.AreEqual(CameraView.ZoomDefault, CameraView.Zoom, 1e-4f);
        Assert.AreEqual(CameraView.RotDefault, CameraView.Rot, 1e-4f);
        Assert.AreEqual(CameraView.DefaultTilt, CameraView.Tilt, 1e-4f);
    }

    [Test] public void Load_OutOfRangeSavedValues_AreClamped()
    {
        PlayerPrefs.SetFloat(CameraView.PrefZoom, 9f); PlayerPrefs.SetFloat(CameraView.PrefRot, -200f); PlayerPrefs.SetFloat(CameraView.PrefTilt, 5f);
        PlayerPrefs.SetInt(CameraView.PrefVer, CameraView.Version); PlayerPrefs.Save();
        CameraView.Load();
        Assert.AreEqual(1.4f, CameraView.Zoom, 1e-4f); Assert.AreEqual(-45f, CameraView.Rot, 1e-4f); Assert.AreEqual(35f, CameraView.Tilt, 1e-4f);
    }

    [Test] public void Load_DeletesLegacyOrthoZoom()
    {
        PlayerPrefs.SetFloat(CameraView.LegacyPrefZoom, 6.5f); PlayerPrefs.Save();
        CameraView.Load();
        Assert.IsFalse(PlayerPrefs.HasKey(CameraView.LegacyPrefZoom), "cam_zoom_v3 deve essere eliminata");
        Assert.AreEqual(CameraView.ZoomDefault, CameraView.Zoom, 1e-4f);
    }

    [Test] public void Tutorial_LimitsZoomAndLocksRotationTilt()
    {
        CameraView.SetForTest(1.4f, 45f, 60f);
        CameraView.Tutorial = true;
        Assert.AreEqual(1.2f, CameraView.EffectiveZoom, 1e-4f);
        Assert.AreEqual(0f, CameraView.EffectiveRot, 1e-4f);
        Assert.AreEqual(CameraView.DefaultTilt, CameraView.EffectiveTilt, 1e-4f);
        CameraView.SetZoom(0.7f); Assert.AreEqual(0.85f, CameraView.Zoom, 1e-4f);
        CameraView.SetRot(-30f); Assert.AreEqual(45f, CameraView.Rot, 1e-4f, "rotazione bloccata nel tutorial");
        CameraView.SetTilt(35f); Assert.AreEqual(60f, CameraView.Tilt, 1e-4f, "inclinazione bloccata nel tutorial");
        CameraView.Tutorial = false;
        Assert.AreEqual(45f, CameraView.EffectiveRot, 1e-4f, "dal freePlay tornano i valori del giocatore");
    }

    [Test] public void Reset_RestoresDefaultsAndSaves()
    {
        CameraView.SetForTest(0.7f, -45f, 35f);
        CameraView.ResetView();
        Assert.AreEqual(1f, CameraView.Zoom, 1e-4f); Assert.AreEqual(0f, CameraView.Rot, 1e-4f); Assert.AreEqual(CameraView.DefaultTilt, CameraView.Tilt, 1e-4f);
        Assert.AreEqual(1f, PlayerPrefs.GetFloat(CameraView.PrefZoom), 1e-4f);
        CameraView.SetForTest(0.7f, -45f, 35f); CameraView.ResetZoom();
        Assert.AreEqual(1f, CameraView.Zoom, 1e-4f); Assert.AreEqual(-45f, CameraView.Rot, 1e-4f, "doppio tocco: solo lo zoom");
    }
}
