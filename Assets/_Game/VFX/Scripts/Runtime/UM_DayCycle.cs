// Ultimo Miglio - ciclo del giorno del cap. 1: GIORNO -> ORA DORATA -> TRAMONTO (lanterne accese). Niente notte nel cap. 1.
// Pilota UM_LightingRig.SetGoldenBlend, le lanterne (UM_Lantern) e, solo se il post è attivo, il peso di un Volume "tramonto".
// Design/Narrative: UM_DayCycle.Instance.ForceTimeOfDay(UM_DayCycle.TimeOfDay.Sunset) oppure SetTime01(0.9f).
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltimoMiglio.VFX
{
    public class UM_DayCycle : MonoBehaviour
    {
        public enum TimeOfDay { Morning, Day, GoldenHour, Sunset }

        [Header("Durate (s) - da data/ch1/daycycle.json")]
        public float dayDuration = 415f;      // giorno 6:55
        public float sunsetDuration = 5f;     // tramonto
        [Header("Curva")]
        [Range(0, 1)] public float goldenStart = 0.8f;   // frazione del giorno in cui inizia l'ora dorata
        [Range(0, 1)] public float lanternsOnAt = 0.6f;  // blend dorato a cui si accendono le lanterne
        public float blendSmoothing = 0.6f;              // s, smorzamento: nessun salto anche con ForceTimeOfDay
        [Header("Riferimenti")]
        public UM_LightingRig rig;
        [Tooltip("Opzionale: Volume col profilo tramonto. Usato SOLO se il post è attivo (su WebGL è spento).")]
        public Volume sunsetVolume;
        public bool tintCameraBackground = true;
        public bool autoPlay = true;

        public static UM_DayCycle Instance { get; private set; }
        public event Action OnSunset, OnDayEnd;
        /// Tempo normalizzato 0..1 sull'intera giornata (giorno + tramonto).
        public float Time01 { get; private set; }
        public float GoldenBlend { get; private set; }
        public bool LanternsOn { get; private set; }
        public TimeOfDay Current => Time01 < 0.15f ? TimeOfDay.Morning : GoldenBlend < 0.05f ? TimeOfDay.Day : GoldenBlend < 0.95f ? TimeOfDay.GoldenHour : TimeOfDay.Sunset;

        float _targetGolden; bool _playing, _sunsetFired;
        Camera _cam;

        void Awake() => Bind();
        /// Registra l'istanza e trova il rig (chiamato da Awake; utile anche in editor/test).
        public void Bind() { Instance = this; if (!rig) rig = FindFirstObjectByType<UM_LightingRig>(); _playing = autoPlay; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// Inizio giornata (chiamato dal Tech Lead a ogni nuovo giorno di gioco).
        public void StartDay() { Time01 = 0; _sunsetFired = false; _playing = true; Snap(); }
        public void Pause() => _playing = false;
        public void Resume() => _playing = true;

        /// Forza un'ora del giorno (Design/Narrative: cutscene, onboarding). pause=true congela il ciclo.
        public void ForceTimeOfDay(TimeOfDay t, bool pause = true, bool instant = false)
        {
            float v = t switch { TimeOfDay.Morning => 0.05f, TimeOfDay.Day => 0.5f, TimeOfDay.GoldenHour => Mathf.Lerp(goldenStart, 1f, 0.5f) * DayFrac, _ => 1f };
            SetTime01(v, pause, instant);
        }
        public void SetTime01(float t01, bool pause = true, bool instant = false)
        {
            Time01 = Mathf.Clamp01(t01); if (pause) _playing = false;
            if (instant) Snap(); else Evaluate();
        }

        float DayFrac => dayDuration / Mathf.Max(0.01f, dayDuration + sunsetDuration);

        void Update()
        {
            if (_playing)
            {
                Time01 += Time.deltaTime / Mathf.Max(1f, dayDuration + sunsetDuration);
                if (Time01 >= 1f) { Time01 = 1f; _playing = false; OnDayEnd?.Invoke(); }
            }
            Evaluate();
        }

        float TargetGolden()
        {
            float dayT = Time01 / DayFrac; // 0..1 nel giorno, >1 nel tramonto
            float g = Mathf.InverseLerp(goldenStart, 1f, dayT);         // ora dorata a fine giorno
            g = Mathf.Lerp(g * 0.7f, 1f, Mathf.InverseLerp(DayFrac, 1f, Time01)); // tramonto porta a 1
            return Mathf.SmoothStep(0, 1, Mathf.Clamp01(g));
        }

        void Evaluate()
        {
            _targetGolden = TargetGolden();
            float k = blendSmoothing <= 0 ? 1f : 1f - Mathf.Exp(-Time.deltaTime / blendSmoothing);
            GoldenBlend = Mathf.Lerp(GoldenBlend, _targetGolden, k);
            Push();
        }
        void Snap() { _targetGolden = TargetGolden(); GoldenBlend = _targetGolden; Push(); UM_Lantern.SnapAll(); }

        void Push()
        {
            if (rig) rig.SetGoldenBlend(GoldenBlend);
            if (sunsetVolume && PostAllowed()) sunsetVolume.weight = GoldenBlend;
            if (tintCameraBackground) { if (!_cam) _cam = Camera.main; if (_cam) _cam.backgroundColor = RenderSettings.fogColor; }
            bool on = GoldenBlend >= lanternsOnAt;
            if (on != LanternsOn) { LanternsOn = on; UM_Lantern.SetAll(on); }
            if (!_sunsetFired && Time01 >= DayFrac) { _sunsetFired = true; OnSunset?.Invoke(); }
        }

        static bool PostAllowed() => UM_QualityTier.PostProcessingEnabled;
    }
}
