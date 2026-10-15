// Ultimo Miglio - cablaggio in un clic per il Tech Lead: genera materiali/prefab, poi aggiunge alla scena aperta
// UM_FxPlayer (prefab assegnati), UM_LookSettings (cielo + grading in-shader), UM_DayCycle sul sole.
// Menu "Ultimo Miglio/VFX/Setup Scene" o -executeMethod UltimoMiglio.VFX.EditorTools.UM_VfxSceneSetup.Setup
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UltimoMiglio.VFX.EditorTools
{
    public static class UM_VfxSceneSetup
    {
        const string P = "Assets/_Game/VFX/Prefabs/", M = "Assets/_Game/VFX/Materials/";
        static GameObject L(string n) => AssetDatabase.LoadAssetAtPath<GameObject>(P + n + ".prefab");

        [MenuItem("Ultimo Miglio/VFX/Setup Scene (FX + Look + DayCycle)")]
        public static void Setup()
        {
            if (!L("fx_purchase") || !AssetDatabase.LoadAssetAtPath<Material>(M + "M_FX_Sky.mat")) UM_FxPrefabBuilder.BuildAll();
            UM_LightingSetup.Setup();
            var sun = RenderSettings.sun;
            var fxGo = GameObject.Find("UM_FX");
            if (!fxGo) fxGo = new GameObject("UM_FX");
            var fx = fxGo.GetComponent<UM_FxPlayer>(); if (!fx) fx = fxGo.AddComponent<UM_FxPlayer>();
            fx.packagePop = L("fx_package_pop"); fx.coins = L("fx_coins"); fx.upgrade = L("fx_upgrade");
            fx.packagePickup = L("fx_package_pickup"); fx.packageDeliver = L("fx_package_deliver"); fx.upgradeComplete = L("fx_upgrade_complete");
            fx.purchase = L("fx_purchase"); fx.coinFlyer = L("fx_coin_flyer"); fx.worldCamera = Camera.main;
            var look = fxGo.GetComponent<UM_LookSettings>(); if (!look) look = fxGo.AddComponent<UM_LookSettings>();
            look.targetCamera = Camera.main; look.skyMaterial = AssetDatabase.LoadAssetAtPath<Material>(M + "M_FX_Sky.mat");
            var nina = GameObject.Find("Nina"); if (nina) look.focus = nina.transform;
            if (sun)
            {
                var dc = sun.GetComponent<UM_DayCycle>(); if (!dc) dc = sun.gameObject.AddComponent<UM_DayCycle>();
                dc.rig = sun.GetComponent<UM_LightingRig>();
                EditorUtility.SetDirty(dc);
            }
            look.Apply();
            EditorUtility.SetDirty(fx); EditorUtility.SetDirty(look);
            EditorSceneManager.MarkSceneDirty(fxGo.scene);
            Debug.Log("[UM VFX] scena cablata: UM_FX (FxPlayer + LookSettings), DayCycle sul sole. Salvare la scena.");
        }
    }
}
