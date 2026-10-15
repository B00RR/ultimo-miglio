// Ultimo Miglio - cablaggio in un clic per il Tech Lead: genera materiali/prefab, poi aggiunge alla scena aperta
// UM_FxPlayer (prefab assegnati), UM_LookSettings (cielo + grading in-shader), UM_DayCycle sul sole,
// e assegna le icone UI/UX a ogni UM_PadIcon presente.
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
            AssignPadIcons();
            EditorUtility.SetDirty(fx); EditorUtility.SetDirty(look);
            EditorSceneManager.MarkSceneDirty(fxGo.scene);
            Debug.Log("[UM VFX] scena cablata: UM_FX (FxPlayer + LookSettings), DayCycle sul sole, icone pad assegnate. Salvare la scena.");
        }

        /// Assegna le icone UI/UX (Assets/_Game/UI/Icons/ic_pad_*_white.png) a ogni UM_PadIcon in scena.
        /// Aggiungere UM_PadIcon a ciascun oggetto pad (acquisto/upgrade/pacco/consegna); Kind da UM_PadFillRing se presente.
        public static void AssignPadIcons()
        {
            foreach (var icon in Object.FindObjectsByType<UM_PadIcon>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                icon.AssignDefaultIcon();
                EditorUtility.SetDirty(icon);
            }
        }
    }
}
