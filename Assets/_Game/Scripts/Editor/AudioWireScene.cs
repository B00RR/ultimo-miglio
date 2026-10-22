using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltimoMiglio.Gameplay;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Audio cap.1: ricabla le clip dell'AudioManager nella scena Ch1_Deposito SENZA rigenerare la scena,
    /// poi stampa l'audit ([AudioAudit] OK/MANCA). Batchmode: -executeMethod UltimoMiglio.EditorTools.AudioWireScene.WireAndAudit</summary>
    public static class AudioWireScene
    {
        public const string ScenePath = "Assets/_Game/Scenes/Ch1_Deposito.unity";

        [MenuItem("Ultimo Miglio/Audio/Ricabla clip in Ch1_Deposito")]
        public static void WireAndAudit()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var am = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
            if (!am)
            {
                var root = Object.FindFirstObjectByType<GameRoot>(FindObjectsInactive.Include);
                am = new GameObject("AudioManager").AddComponent<AudioManager>();
                if (root) am.transform.SetParent(root.transform.parent ? root.transform.parent : root.transform);
                Debug.LogWarning("[AudioAudit] AudioManager assente nella scena: creato");
            }
            SliceSceneBuilder.WireAudioClips(am);
            EditorUtility.SetDirty(am);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            int missing = Audit(am);
            Debug.Log($"[AudioAudit] fine: {missing} voci mancanti");
            if (Application.isBatchMode && missing > 0) EditorApplication.Exit(3);
        }

        public static int Audit(AudioManager am)
        {
            int miss = 0;
            void Chk(string n, bool ok) { if (!ok) miss++; Debug.Log($"[AudioAudit] {(ok ? "OK  " : "MANCA")} {n}"); }
            bool All(AudioClip[] a, int min) => a != null && a.Length >= min && a.All(c => c);
            Chk("Musica stem x3", All(am.MusicStems, 3)); Chk("Musica full", am.MusicFull);
            Chk("Passi (stone x4)", All(am.Steps, 4)); Chk("Pickup pacco x8", All(am.Pickup, 8));
            Chk("Deposito scaffale x4", All(am.Deposit, 4)); Chk("Bancone servito", am.Served); Chk("Campanello cliente", am.Bell);
            Chk("Monete drop x4", All(am.CoinDrop, 4)); Chk("Monete pickup x4", All(am.CoinPickup, 4));
            Chk("Stack MAX", am.StackMax); Chk("Upgrade loop", am.UpgradeLoop); Chk("Upgrade completo", am.UpgradeComplete);
            Chk("Combo x1.5/x2", am.ComboX15 && am.ComboX2); Chk("Combo +N (#159)", All(am.ComboPlus, 8)); Chk("NONNO", am.NonnoStart && All(am.NonnoMumble, 3));
            Chk("UI tap x2", All(am.UiTap, 2)); Chk("Tutorial freccia", am.TutorialArrow); Chk("Tutorial step completato", am.TutorialStep);
            Chk("Tappe debito x3", All(am.DebtStamp, 3));
            return miss;
        }
    }
}
