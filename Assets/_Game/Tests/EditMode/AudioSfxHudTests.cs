using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UltimoMiglio.Gameplay;

/// <summary>#159: asset sfx_combo_plus_01..08 e sfx_debt_stamp_01..03 (#153) presenti e scelta della clip per indice.</summary>
public class AudioSfxHudTests
{
    [Test] public void AssetDella153_Presenti()
    {
        for (int i = 1; i <= 8; i++) Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/SFX/sfx_combo_plus_{i:00}.ogg"), "combo_plus_" + i);
        for (int i = 1; i <= 3; i++) Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/SFX/sfx_debt_stamp_{i:00}.ogg"), "debt_stamp_" + i);
    }
    [Test] public void At_Clamp()
    {
        var a = new[] { AudioClip.Create("a", 10, 1, 8000, false), AudioClip.Create("b", 10, 1, 8000, false) };
        Assert.AreSame(a[0], AudioManager.At(a, -1)); Assert.AreSame(a[1], AudioManager.At(a, 1)); Assert.AreSame(a[1], AudioManager.At(a, 9));
        Assert.IsNull(AudioManager.At(new AudioClip[0], 0)); Assert.IsNull(AudioManager.At(null, 0));
    }
}
