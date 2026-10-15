using System;
using System.IO;
using UnityEngine;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Owns the EconomyCore, save, QA log and the active-time clock. One per scene.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot I { get; private set; }
        public EconomyCore Eco { get; private set; }
        public QaEventLog Qa { get; private set; }
        public NinaController Nina;
        public double ActiveTime => Eco.S.ActiveSec;
        public event Action Changed;

        string SavePath => Path.Combine(Application.persistentDataPath, "save_v1.json");
        float _autosave;
        StreamWriter _qaFile;

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;
            var cfg = DataLoader.LoadChapter(1);
            var state = Load();
            Eco = new EconomyCore(cfg, state);
            Qa = new QaEventLog(Eco, Guid.NewGuid().ToString("N").Substring(0, 8));
            OpenQaLog();
            Qa.Log("session_start", null);
            Eco.ApplyOffline(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }

        void OpenQaLog()
        {
            try { _qaFile = new StreamWriter(Path.Combine(Application.persistentDataPath, "qa_log.jsonl"), true) { AutoFlush = true }; }
            catch (Exception e) { Debug.LogWarning("QA log file: " + e.Message); }
            Qa.Sink += line =>
            {
                _qaFile?.WriteLine(line);
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var log = new AndroidJavaClass("android.util.Log")) log.CallStatic<int>("i", "UMQA", line);
#else
                if (Debug.isDebugBuild || Application.isEditor) Debug.Log("[UMQA] " + line);
#endif
            };
        }

        void Update()
        {
            Eco.Tick(Time.deltaTime);
            if ((_autosave += Time.unscaledDeltaTime) > 30f) { _autosave = 0; Save(); }
            Changed?.Invoke();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) { Qa.Log("session_end", null); Save(); }
            else { Qa.Log("session_start", null); Eco.ApplyOffline(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); }
        }
        void OnApplicationQuit() { Qa.Log("session_end", null); Save(); _qaFile?.Dispose(); }

        EconomyState Load()
        {
            try { if (File.Exists(SavePath)) return JsonUtility.FromJson<EconomyState>(File.ReadAllText(SavePath)); }
            catch (Exception e) { Debug.LogWarning("Save corrupt, starting fresh: " + e.Message); }
            return new EconomyState();
        }
        public void Save()
        {
            Eco.S.LastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var tmp = SavePath + ".tmp"; File.WriteAllText(tmp, JsonUtility.ToJson(Eco.S));
            if (File.Exists(SavePath)) File.Replace(tmp, SavePath, null); else File.Move(tmp, SavePath); // atomic
        }
    }
}
