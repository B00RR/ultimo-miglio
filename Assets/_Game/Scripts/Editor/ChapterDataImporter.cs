using System.IO;
using UnityEditor;
using UnityEngine;
using UltimoMiglio.Gameplay;

namespace UltimoMiglio.EditorTools
{
    /// <summary>JSON (Assets/_Game/Data/ch1/*.json) → Assets/_Game/Data/Resources/Ch1Data.asset. Re-runs when any ch1 JSON is reimported.</summary>
    public sealed class ChapterDataImporter : AssetPostprocessor
    {
        public const string JsonDir = "Assets/_Game/Data/ch1", AssetPath = "Assets/_Game/Data/Resources/Ch1Data.asset";

        [MenuItem("Ultimo Miglio/Data/Import Ch1 Data")]
        public static void Import()
        {
            var data = DataLoader.Parse(1, n => File.ReadAllText(Path.Combine(JsonDir, n + ".json")));
            var so = AssetDatabase.LoadAssetAtPath<ChapterDataAsset>(AssetPath);
            if (!so) { Directory.CreateDirectory(Path.GetDirectoryName(AssetPath)); so = ScriptableObject.CreateInstance<ChapterDataAsset>(); AssetDatabase.CreateAsset(so, AssetPath); }
            so.Data = data; EditorUtility.SetDirty(so); AssetDatabase.SaveAssets();
            Debug.Log("[Data] Ch1Data.asset imported from " + JsonDir);
        }

        static void OnPostprocessAllAssets(string[] imported, string[] d, string[] m, string[] mf)
        {
            foreach (var p in imported) if (p.StartsWith(JsonDir) && p.EndsWith(".json")) { EditorApplication.delayCall += Import; return; }
        }
    }
}
