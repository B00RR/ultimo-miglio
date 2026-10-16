// Ultimo Miglio - aggiunge UM_TutorialMarker alla scena aperta con i materiali assegnati (così gli shader entrano nella build WebGL).
// Menu "Ultimo Miglio/VFX/Aggiungi Tutorial Marker (#4)"
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UltimoMiglio.VFX.EditorTools
{
    public static class UM_TutorialMarkerSetup
    {
        const string M = "Assets/_Game/VFX/Materials/";
        [MenuItem("Ultimo Miglio/VFX/Aggiungi Tutorial Marker (#4)")]
        public static void Setup()
        {
            var m = Object.FindAnyObjectByType<UM_TutorialMarker>();
            if (!m) m = new GameObject("UM_TutorialMarker").AddComponent<UM_TutorialMarker>();
            m.ringMaterial = AssetDatabase.LoadAssetAtPath<Material>(M + "M_FX_TutorialRing.mat");
            m.arrowMaterial = AssetDatabase.LoadAssetAtPath<Material>(M + "M_FX_TutorialArrow.mat");
            var nina = GameObject.Find("Nina"); if (nina) m.player = nina.transform;
            if (!m.ringMaterial || !m.arrowMaterial) Debug.LogError("[UM VFX] materiali M_FX_TutorialRing/Arrow mancanti");
            EditorUtility.SetDirty(m); EditorSceneManager.MarkSceneDirty(m.gameObject.scene);
            Debug.Log("[UM VFX] UM_TutorialMarker in scena (anello + freccia a terra). Salvare la scena.");
        }
    }
}
