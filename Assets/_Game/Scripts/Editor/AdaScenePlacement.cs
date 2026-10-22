using UnityEditor;
using UnityEngine;
using UltimoMiglio.Gameplay;

namespace UltimoMiglio.EditorTools
{
    /// <summary>#118 – posa Nonna Ada (prefab riggato di #93) nella scena del Cap. 1, a fianco del Bancone sul lato est, rivolta verso la sala.
    /// Null-safe: se prefab/controller mancano, warning e nessun oggetto. Scala 0.88 già sul root del prefab (AdaSetup.PrefabScale), qui la si forza.
    /// Outline: materiale personaggi M_Char_Atlas (toon + pass outline #3B2F2A), come Nina/clienti.</summary>
    public static class AdaScenePlacement
    {
        public const string PrefabPath = "Assets/_Game/Art/Characters/chr_nonna_ada.prefab";
        public const string ControllerPath = "Assets/_Game/Art/Characters/chr_nonna_ada.controller";
        public const float Scale = 0.88f;
        /// <summary>Offset dal centro della zona Bancone: a fianco del banco sul lato est, fuori dalla Zona_Bancone (x ±1.2, z ±0.6) dove sta Nina, dalla coda clienti (-Z), dal vaso (1.6, 0.7) e dalla Cassa (x ≥ 2.5); visibile dalla camera (il vaso di limoni a ovest la copriva).</summary>
        public static readonly Vector3 OffsetFromBancone = new Vector3(1.6f, 0f, -0.15f);

        public static GameObject Place(Vector3 banPos, Transform parent)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!pf) { Debug.LogWarning("[Scene] Nonna Ada: prefab mancante " + PrefabPath + " (nessuna Ada in scena)"); return null; }
            var ada = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            ada.name = "NonnaAda";
            if (parent) ada.transform.SetParent(parent, true);
            ada.transform.position = banPos + OffsetFromBancone;
            ada.transform.rotation = Quaternion.Euler(0, 180f, 0); // fronte +Z del modello → guarda la sala (-Z), verso camera e coda
            ada.transform.localScale = Vector3.one * Scale;
            var an = ada.GetComponentInChildren<Animator>();
            if (an && !an.runtimeAnimatorController)
            {
                var ctl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                if (ctl) an.runtimeAnimatorController = ctl; else Debug.LogWarning("[Scene] Nonna Ada: controller mancante " + ControllerPath);
            }
            if (an) { an.applyRootMotion = false; an.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/VFX/Materials/M_Char_Atlas.mat");
            if (mat) foreach (var r in ada.GetComponentsInChildren<Renderer>()) r.sharedMaterials = System.Array.ConvertAll(r.sharedMaterials, _ => mat);
            // solida come clienti/NONNO (#154): Nina non la attraversa. Collider sul root (scala 0.88 → raggio/altezza in unità locali)
            var cap = ada.AddComponent<CapsuleCollider>(); cap.radius = 0.3f / Scale; cap.height = 1.5f / Scale; cap.center = new Vector3(0, 0.75f / Scale, 0);
            var pres = ada.AddComponent<AdaPresence>(); pres.Animator = an;
            var shadowPf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/VFX/Prefabs/fx_contact_shadow.prefab");
            if (shadowPf) { var sh = (GameObject)PrefabUtility.InstantiatePrefab(shadowPf); sh.transform.SetParent(ada.transform, false); sh.transform.localPosition = new Vector3(0, 0.02f, 0); }
            Debug.Log($"[Scene] Nonna Ada posata a {ada.transform.position} scala {Scale} controller={(an && an.runtimeAnimatorController ? an.runtimeAnimatorController.name : "nessuno")}");
            return ada;
        }
    }
}
