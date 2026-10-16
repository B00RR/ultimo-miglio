using UnityEngine;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Drives the "_Fill" parameter (0..1) of a ground ring quad/decal (04_UI §2.1). Works with any shader that
    /// exposes _Fill (VFX's toon ring or the fallback UM_ZoneRing.shader). Empties in 0.3 s on exit.
    /// Hook: ProximityZone.OnProgress(float) → SetFill; ProximityZone.OnExit → Drain().
    /// </summary>
    public class ZoneFillRing : MonoBehaviour
    {
        static readonly int FillId = Shader.PropertyToID("_Fill");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int PulseId = Shader.PropertyToID("_Pulse");

        [SerializeField] Renderer ringRenderer;
        [SerializeField] Color color = new Color(0.957f, 0.769f, 0.188f, 0.55f); // soft sun pad fill
        [SerializeField] float drainSeconds = 0.3f;

        MaterialPropertyBlock mpb;
        float fill, target, pulse;
        bool draining;

        public float Fill => fill;

        void Awake() { if (!ringRenderer) ringRenderer = GetComponent<Renderer>(); mpb = new MaterialPropertyBlock(); Apply(); }

        public void SetFill(float value) { draining = false; target = fill = Mathf.Clamp01(value); Apply(); }
        public void Drain() { draining = true; target = 0f; }
        public void PulseOnce() { pulse = 1f; }
        public void SetColor(Color c) { color = c; Apply(); }

        void Update()
        {
            bool dirty = false;
            if (draining && fill > 0f) { fill = Mathf.MoveTowards(fill, 0f, Time.deltaTime / Mathf.Max(0.01f, drainSeconds)); dirty = true; }
            if (pulse > 0f) { pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 4f); dirty = true; }
            if (dirty) Apply();
        }

        void Apply()
        {
            if (!ringRenderer) ringRenderer = GetComponent<Renderer>();
            if (!ringRenderer) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            ringRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(FillId, fill); mpb.SetColor(ColorId, color); mpb.SetFloat(PulseId, pulse);
            ringRenderer.SetPropertyBlock(mpb);
        }
    }
}
