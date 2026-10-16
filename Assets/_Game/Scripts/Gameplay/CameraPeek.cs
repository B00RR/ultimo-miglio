using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Camera breve (#94): presentazione di elementi nuovi 2,75 s; aiuto a 40 s ≤ 1,5 s (rampe 0,4 s, accorciate al 30 % se serve). Per 0,8–3 s la camera guarda un punto (nuovo elemento rivelato, bersaglio fermo da 40 s)
    /// e poi torna su Nina. Il joystick resta SEMPRE attivo: Nina si muove, cambia solo il punto guardato da DepotCameraFit.
    /// Curva: 0,4 s andata (smoothstep) → tenuta → 0,4 s ritorno. Una richiesta nuova sostituisce la precedente.
    /// Uso: CameraPeek.Request(pos, 2.75f). Se il giocatore muove il joystick durante la tenuta, il ritorno parte subito (NotifyInput).</summary>
    public static class CameraPeek
    {
        public const float DefaultSec = 2.75f, RampSec = 0.4f;
        static Vector3 _pos; static float _start = -1f, _dur, _ramp = RampSec;

        public static bool Active => _start >= 0f && Time.unscaledTime - _start < _dur;

        public static void Request(Vector3 worldPos, float seconds = DefaultSec)
        {
            _pos = worldPos; _start = Time.unscaledTime; _dur = Mathf.Clamp(seconds, 0.8f, 3f); float ramp = Mathf.Min(RampSec, _dur * 0.3f); _ramp = ramp;
            Debug.Log($"[Onb] cameraPeek {worldPos} {_dur:0.00}s");
        }

        /// <summary>Input del giocatore: accorcia la tenuta (ritorno in RampSec), non blocca mai il movimento.</summary>
        public static void NotifyInput()
        {
            if (!Active) return;
            float t = Time.unscaledTime - _start;
            if (t < _dur - _ramp) _dur = Mathf.Max(t, _ramp) + _ramp;
        }

        public static void Cancel() { _start = -1f; }

        /// <summary>Punto che la camera deve seguire: Nina blendata verso il bersaglio (0 = Nina, 1 = bersaglio).</summary>
        public static Vector3 Blend(Vector3 nina)
        {
            if (!Active) return nina;
            float t = Time.unscaledTime - _start;
            float w = Mathf.Min(Mathf.Clamp01(t / _ramp), Mathf.Clamp01((_dur - t) / _ramp));
            w = w * w * (3f - 2f * w);
            return Vector3.Lerp(nina, _pos, w);
        }
    }
}
