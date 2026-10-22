// Ultimo Miglio - anello del pad upgrade: SetProgress(0..1) con inseguimento morbido, pulsazione lenta, flash + burst a 1.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public class UM_PadFillRing : MonoBehaviour
    {
        public enum Kind { Purchase, Upgrade, Pickup, Delivery }
        public Renderer ring;
        [Tooltip("Colore UI dell'anello (pulsazione + riempimento). Purchase = sole #F4C430, Upgrade = sole #F4C430 (#69), Pickup = terracotta #C8633B (#69), Delivery = menta #8FC9A8")]
        public Kind kind = Kind.Purchase;
        [Tooltip("Velocità con cui il riempimento visivo insegue il valore (1/s)")] public float followSpeed = 8f;
        public float pulseSpeed = 2.2f, flashTime = 0.45f;
        public bool playCompleteBurst = true;
        static readonly int ColorId = Shader.PropertyToID("_Color"), FillId = Shader.PropertyToID("_Fill"), PulseId = Shader.PropertyToID("_Pulse"), FlashId = Shader.PropertyToID("_Flash"), GlowId = Shader.PropertyToID("_GlowColor");
        MaterialPropertyBlock _mpb; float _target, _shown, _flash; bool _completed;
        // #240: stato spento dopo l'acquisto (finché Kaia non esce) e pop di ritorno. Valori in UM_PadOffStyle.
        bool _off; float _popT = -1f; Vector3 _baseScale = Vector3.one; bool _baseCached;
        static readonly Color GlowDefault = new Color(1f, 0.957f, 0.839f, 1f); // = _GlowColor di default dello shader
        public bool IsOff => _off;
        public bool IsPopping => _popT >= 0f;
        /// Intensità di glow/emissione applicata all'ultimo Push (0 da spento). Per test.
        public float GlowIntensity { get; private set; }
        /// Colore dell'anello applicato all'ultimo Push. Per test.
        public Color AppliedColor { get; private set; }
        public System.Action OnCompleted;

        void Awake() { _mpb = new MaterialPropertyBlock(); CacheScale(); Push(); }
        void CacheScale() { if (_baseCached) return; _baseScale = transform.localScale; _baseCached = true; }

        /// #240: spegne (true) o riaccende (false) l'anello. Alla riaccensione parte il pop di 0,2 s con flash del colore attivo.
        public void SetOff(bool off)
        {
            if (off == _off) return;
            CacheScale();
            _off = off;
            if (off) { _popT = -1f; _flash = 0f; transform.localScale = _baseScale; }
            else _popT = 0f;
            Push();
        }
        /// Avanza il pop di dt secondi (Update lo chiama con Time.deltaTime; i test lo chiamano direttamente).
        public void TickPop(float dt)
        {
            if (_popT < 0f) return;
            _popT += dt;
            if (_popT >= UM_PadOffStyle.PopTime) { _popT = -1f; transform.localScale = _baseScale; }
            else transform.localScale = _baseScale * UM_PadOffStyle.PopScale(_popT);
            Push();
        }

        /// Imposta il progresso 0..1. Al raggiungimento di 1 parte flash + burst (una volta, finché non si torna sotto 1).
        public void SetProgress(float p)
        {
            _target = Mathf.Clamp01(p);
            if (_target < 0.999f) _completed = false;
        }
        /// Cambia il colore UI dell'anello (es. pad acquisto vs zona pacco/cliente).
        public void SetKind(Kind k) { kind = k; Push(); }
        public static Color ColorOf(Kind k) => k == Kind.Pickup ? UM_Palette.Terracotta /* #69: niente teal nelle zone */ : k == Kind.Delivery ? UM_Palette.UiCliente : k == Kind.Upgrade ? UM_Palette.UiSole /* #69: upgrade = sole come acquisto */ : UM_Palette.UiSole;
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
            if (_popT >= 0f) TickPop(Time.deltaTime); else Push();
        }

        void Push()
        {
            if (!ring) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            ring.GetPropertyBlock(_mpb);
            var active = ColorOf(kind);
            if (_off)
            {
                // #240 spento: anello intero desaturato verso il crema, alpha 0,5, glow/emissione a 0 (punta = stesso colore, niente flash/pulse)
                var c = UM_PadOffStyle.RingOff(active);
                _mpb.SetColor(ColorId, c); _mpb.SetColor(GlowId, c);
                _mpb.SetFloat(FillId, 1f); _mpb.SetFloat(PulseId, 0f); _mpb.SetFloat(FlashId, UM_PadOffStyle.RingGlowOff);
                AppliedColor = c; GlowIntensity = UM_PadOffStyle.RingGlowOff;
            }
            else if (_popT >= 0f && _popT < UM_PadOffStyle.FlashTime)
            {
                // #240 ritorno: flash del colore attivo pieno nei primi 0,08 s
                _mpb.SetColor(ColorId, active); _mpb.SetColor(GlowId, active);
                _mpb.SetFloat(FillId, 1f); _mpb.SetFloat(PulseId, 0f); _mpb.SetFloat(FlashId, 1f);
                AppliedColor = active; GlowIntensity = 1f;
            }
            else
            {
                _mpb.SetColor(ColorId, active); _mpb.SetColor(GlowId, GlowDefault);
                _mpb.SetFloat(FillId, _shown);
                _mpb.SetFloat(PulseId, _shown > 0 && _shown < 1 ? 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) : 0);
                _mpb.SetFloat(FlashId, _flash);
                AppliedColor = active; GlowIntensity = _flash;
            }
            ring.SetPropertyBlock(_mpb);
        }
    }
}
