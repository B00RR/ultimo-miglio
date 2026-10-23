using System;
using System.IO;
using UnityEngine;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Owns the EconomyCore, save, QA log and the active-time clock. One per scene.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot I { get; private set; }
        /// <summary>False if chapter data failed to load: dependents skip their Update instead of NRE-spamming every frame.</summary>
        public static bool Ready => I && I.Eco != null;
        public EconomyCore Eco { get; private set; }
        public QaEventLog Qa { get; private set; }
        public NinaController Nina;
        public double ActiveTime => Eco.S.ActiveSec;
        public event Action Changed;

        string SavePath => Path.Combine(Application.persistentDataPath, "save_v1.json");
        const string SaveKey = "um_save_v1";
        float _autosave;
        StreamWriter _qaFile;

        void Awake()
        {
            I = this;
            if (!GetComponent<QaBridge>()) gameObject.AddComponent<QaBridge>();
            // WebGL: nessun NavMesh bake runtime → elimina ogni NavMeshAgent subito
            NonnoRobot.StripAllNavMeshAgentsInScene();
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.targetFrameRate = -1;
#else
            Application.targetFrameRate = 60;
#endif
            EconomyConfig cfg;
            try { cfg = DataLoader.LoadChapter(1); }
            catch (Exception e) { Debug.LogError("[GameRoot] " + e.Message); enabled = false; return; }
            var state = Load();
            Eco = new EconomyCore(cfg, state);
            Qa = new QaEventLog(Eco, Guid.NewGuid().ToString("N").Substring(0, 8));
            OpenQaLog();
            Qa.Log("session_start", null);
            Eco.ApplyOffline(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            Eco.RewardGranted += (k, i, ln) => Debug.Log($"[REWARD] kind={k} i={i} ln={ln:0.#}");
            if (!GetComponent<NextPadArrow>()) gameObject.AddComponent<NextPadArrow>();
            if (!GetComponent<DebtMilestoneFeedback>()) gameObject.AddComponent<DebtMilestoneFeedback>(); // #136
        }

        void OpenQaLog()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            // WebGL: persistentDataPath is IndexedDB (IDBFS), never flushed by an open StreamWriter -> no QA file sink there
            try { _qaFile = new StreamWriter(Path.Combine(Application.persistentDataPath, "qa_log.jsonl"), true) { AutoFlush = true }; }
            catch (Exception e) { Debug.LogWarning("QA log file: " + e.Message); }
#endif
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
            if (Eco == null) return;
            if (paused) { Qa.Log("session_end", null); Save(); }
            else { Qa.Log("session_start", null); Eco.ApplyOffline(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: no OnApplicationPause on tab hide; focus loss is the last reliable moment to persist
        void OnApplicationFocus(bool focus) { if (!focus && Eco != null) Save(); }
#endif
        void OnApplicationQuit() { if (Eco == null) return; Qa.Log("session_end", null); Save(); _qaFile?.Dispose(); }

        EconomyState Load()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { var j = PlayerPrefs.GetString(SaveKey, null); if (!string.IsNullOrEmpty(j)) return JsonUtility.FromJson<EconomyState>(j); }
#else
            try { if (File.Exists(SavePath)) return JsonUtility.FromJson<EconomyState>(File.ReadAllText(SavePath)); }
#endif
            catch (Exception e) { Debug.LogWarning("Save corrupt, starting fresh: " + e.Message); }
            return new EconomyState();
        }
        public void Save()
        {
            Eco.S.LastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: System.IO writes to IDBFS are lost without FS.syncfs; PlayerPrefs.Save() flushes to IndexedDB
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Eco.S)); PlayerPrefs.Save();
#else
            try
            {
                var tmp = SavePath + ".tmp"; File.WriteAllText(tmp, JsonUtility.ToJson(Eco.S));
                if (File.Exists(SavePath)) File.Replace(tmp, SavePath, null); else File.Move(tmp, SavePath); // atomic
            }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
#endif
        }
    }
}
