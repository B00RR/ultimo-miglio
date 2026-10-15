// Ultimo Miglio - anello del pad upgrade: SetProgress(0..1) con inseguimento morbido, pulsazione lenta, flash + burst a 1.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public class UM_PadFillRing : MonoBehaviour
    {
        public Renderer ring;
        [Tooltip("Velocità con cui il riempimento visivo insegue il valore (1/s)")] public float followSpeed = 8f;
        public float pulseSpeed = 2.2f, flashTime = 0.45f;
        public bool playCompleteBurst = true;
        static readonly int FillId = Shader.PropertyToID("_Fill"), PulseId = Shader.PropertyToID("_Pulse"), FlashId = Shader.PropertyToID("_Flash");
        MaterialPropertyBlock _mpb; float _target, _shown, _flash; bool _completed;
        public System.Action OnCompleted;

        void Awake() { _mpb = new MaterialPropertyBlock(); Push(); }

        /// Imposta il progresso 0..1. Al raggiungimento di 1 parte flash + burst (una volta, finché non si torna sotto 1).
        public void SetProgress(float p)
        {
            _target = Mathf.Clamp01(p);
            if (_target < 0.999f) _completed = false;
        }
        public void ResetRing() { _target = _shown = 0; _completed = false; _flash = 0; Push(); }
        /// Per screenshot/test: applica subito senza inseguimento.
        public void SetImmediate(float p, float flash = 0) { _target = _shown = Mathf.Clamp01(p); _flash = flash; Push(); }

        void Update()
        {
            _shown = Mathf.MoveTowards(_shown, _target, Time.deltaTime * followSpeed * Mathf.Max(0.05f, Mathf.Abs(_target - _shown)) + Time.deltaTime * 0.2f);
            if (!_completed && _target >= 0.999f && _shown >= 0.995f)
            {
                _completed = true; _flash = 1f;
                if (playCompleteBurst) UM_FxPlayer.Play(UM_FxPlayer.Fx.UpgradeComplete, transform.position);
                OnCompleted?.Invoke();
            }
            if (_flash > 0) _flash = Mathf.Max(0, _flash - Time.deltaTime / flashTime);
            Push();
        }

        void Push()
        {
            if (!ring) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            ring.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FillId, _shown);
            _mpb.SetFloat(PulseId, _shown > 0 && _shown < 1 ? 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) : 0);
            _mpb.SetFloat(FlashId, _flash);
            ring.SetPropertyBlock(_mpb);
        }
    }
}
