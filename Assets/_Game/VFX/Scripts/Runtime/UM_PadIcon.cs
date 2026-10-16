// Ultimo Miglio - icona fluttuante sopra ogni punto di interazione (pad acquisto/upgrade, zona pacco, consegna).
// Billboard verso la camera a ~1.6 m: disco colorato (colore UI per Kind) + glifo bianco, ondeggia (0.12 m, 1.2 s)
// con leggera pulsazione di scala; si attenua quando il giocatore e' vicino (SetPlayerNear).
// Icone UI/UX: Assets/_Game/UI/Icons/ic_pad_{pickup,buy,deliver,upgrade}_white.png (assegnate in editor); se mancano, glifo procedurale.
// Unlit, materiale condiviso (URP Unlit trasparente), nessuna luce, nessuna ombra: WebGL2 OK. Spazio sopra lasciato per etichette/prezzi UI.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    [DisallowMultipleComponent]
    public class UM_PadIcon : MonoBehaviour
    {
        public UM_PadFillRing.Kind kind = UM_PadFillRing.Kind.Purchase;
        [Tooltip("Se c'e' UM_PadFillRing sullo stesso oggetto, usa il suo Kind")] public bool kindFromRing = true;
        [Tooltip("Glifo bianco con alpha (UI/UX). Vuoto = glifo procedurale")] public Texture2D icon;
        public float height = 1.6f, size = 0.7f;
        public float bobAmplitude = 0.12f, bobPeriod = 1.2f, scalePulse = 0.05f;
        [Range(0, 1)] public float nearAlpha = 0.45f;
        public Camera targetCamera;

        public const string IconDir = "Assets/_Game/UI/Icons/";
        public static string IconPathFor(UM_PadFillRing.Kind k) => IconDir + (k == UM_PadFillRing.Kind.Pickup ? "ic_pad_pickup_white.png"
            : k == UM_PadFillRing.Kind.Delivery ? "ic_pad_deliver_white.png" : k == UM_PadFillRing.Kind.Upgrade ? "ic_pad_upgrade_white.png" : "ic_pad_buy_white.png");

        static Material _shared; static Mesh _quad;
        static Texture2D _disc; static readonly Texture2D[] _glyphs = new Texture2D[4];
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"), BaseMapId = Shader.PropertyToID("_BaseMap");

        Transform _root, _disc0, _glyph0; MeshRenderer _discR, _glyphR; MaterialPropertyBlock _mpb;
        float _alpha = 1f, _alphaTarget = 1f, _phase;

        /// Attenua l'icona quando il giocatore e' sul pad (true) e la ripristina quando esce (false).
        public void SetPlayerNear(bool near) { _alphaTarget = near ? nearAlpha : 1f; }
        /// Cambia tipo (colore + glifo di default) a runtime.
        public void SetKind(UM_PadFillRing.Kind k) { kind = k; Refresh(); }
        public void SetIcon(Texture2D tex) { icon = tex; Refresh(); }
        public void SetVisible(bool v) { if (_root) _root.gameObject.SetActive(v); }

        void Awake() { _phase = Random.value * 10f; }

        void OnEnable()
        {
            if (kindFromRing) { var r = GetComponent<UM_PadFillRing>(); if (r) kind = r.kind; }
            Build(); Refresh();
        }

        void OnDisable() { if (_root) _root.gameObject.SetActive(false); }

#if UNITY_EDITOR
        void Reset() { AssignDefaultIcon(); }
        void OnValidate() { if (!icon) AssignDefaultIcon(); }
        /// Editor: assegna l'icona UI/UX del Kind se esiste (chiamato anche da UM_VfxSceneSetup).
        public void AssignDefaultIcon()
        {
            if (kindFromRing) { var r = GetComponent<UM_PadFillRing>(); if (r) kind = r.kind; }
            var t = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(IconPathFor(kind));
            if (t) icon = t;
        }
#endif

        void Build()
        {
            if (_root) { _root.gameObject.SetActive(true); return; }
            EnsureShared();
            _root = new GameObject("UM_PadIcon_Billboard").transform;
            _root.gameObject.hideFlags = HideFlags.DontSave;
            _root.SetParent(transform, false);
            _disc0 = MakeQuad("disc", out _discR, 0f);
            _glyph0 = MakeQuad("glyph", out _glyphR, -0.01f); // un filo davanti al disco
            _glyph0.localScale = Vector3.one * 0.62f;
            _mpb = new MaterialPropertyBlock();
        }

        Transform MakeQuad(string n, out MeshRenderer r, float z)
        {
            var go = new GameObject(n); go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(_root, false); go.transform.localPosition = new Vector3(0, 0, z);
            go.AddComponent<MeshFilter>().sharedMesh = _quad;
            r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = _shared;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return go.transform;
        }

        void Refresh()
        {
            if (!_root) return;
            int ki = (int)kind;
            Texture2D g = icon ? icon : Glyph(kind);
            var c = UM_PadFillRing.ColorOf(kind);
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _mpb.Clear(); _mpb.SetTexture(BaseMapId, _disc); _mpb.SetColor(BaseColorId, new Color(c.r, c.g, c.b, _alpha));
            _discR.SetPropertyBlock(_mpb);
            _mpb.Clear(); _mpb.SetTexture(BaseMapId, g); _mpb.SetColor(BaseColorId, new Color(1f, 1f, 1f, _alpha));
            _glyphR.SetPropertyBlock(_mpb);
            _ = ki;
        }

        void LateUpdate()
        {
            if (!_root) return;
            float t = Time.time + _phase;
            float w = 2f * Mathf.PI / Mathf.Max(0.1f, bobPeriod);
            _root.position = transform.position + Vector3.up * (height + Mathf.Sin(t * w) * bobAmplitude);
            _root.localScale = Vector3.one * size * (1f + Mathf.Sin(t * w * 2f) * scalePulse);
            var cam = targetCamera ? targetCamera : Camera.main;
            if (cam) _root.rotation = cam.transform.rotation; // billboard parallelo allo schermo (ortho-friendly)
            if (!Mathf.Approximately(_alpha, _alphaTarget)) { _alpha = Mathf.MoveTowards(_alpha, _alphaTarget, Time.deltaTime * 3f); Refresh(); }
        }

        // ---------- risorse condivise ----------
        static void EnsureShared()
        {
            if (!_shared)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit");
                _shared = new Material(sh) { name = "M_FX_PadIcon (runtime)", renderQueue = 3100, enableInstancing = false };
                _shared.SetFloat("_Surface", 1f); _shared.SetFloat("_Blend", 0f);
                _shared.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _shared.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _shared.SetFloat("_ZWrite", 0f); _shared.SetFloat("_Cull", 0f);
                _shared.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _shared.SetOverrideTag("RenderType", "Transparent");
            }
            if (!_quad)
            {
                _quad = new Mesh { name = "UM_PadIconQuad" };
                _quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
                _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                _quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                _quad.RecalculateNormals(); _quad.RecalculateBounds();
            }
            if (!_disc) _disc = Draw(64, (x, y) => Circle(x, y, 0.5f, 0.5f, 0.46f) * 0.95f
                                                 + Ring(x, y, 0.5f, 0.5f, 0.47f, 0.035f) * 0.05f); // disco pieno con bordo morbido
        }

        /// Glifi procedurali di riserva (bianchi): pacco = scatola, acquisto = moneta, consegna = persona, upgrade = freccia su.
        static Texture2D Glyph(UM_PadFillRing.Kind k)
        {
            int i = (int)k; if (_glyphs[i]) return _glyphs[i];
            System.Func<float, float, float> f;
            switch (k)
            {
                case UM_PadFillRing.Kind.Pickup: // scatola con nastro
                    f = (x, y) => Mathf.Max(Mathf.Max(Box(x, y, 0.5f, 0.45f, 0.30f, 0.24f) - Box(x, y, 0.5f, 0.45f, 0.24f, 0.18f), Box(x, y, 0.5f, 0.45f, 0.03f, 0.24f)), Box(x, y, 0.5f, 0.6f, 0.30f, 0.03f));
                    break;
                case UM_PadFillRing.Kind.Delivery: // persona: testa + spalle
                    f = (x, y) => Mathf.Max(Circle(x, y, 0.5f, 0.66f, 0.13f), Circle(x, y, 0.5f, 0.22f, 0.27f) * (y > 0.2f && y < 0.46f ? 1f : 0f));
                    break;
                case UM_PadFillRing.Kind.Upgrade: // freccia su
                    f = (x, y) => Mathf.Max(Box(x, y, 0.5f, 0.35f, 0.08f, 0.2f), Tri(x, y));
                    break;
                default: // moneta con "L"
                    f = (x, y) => Mathf.Max(Ring(x, y, 0.5f, 0.5f, 0.3f, 0.05f), Mathf.Max(Box(x, y, 0.45f, 0.5f, 0.04f, 0.15f), Box(x, y, 0.52f, 0.37f, 0.1f, 0.035f)));
                    break;
            }
            return _glyphs[i] = Draw(64, f);
        }

        static Texture2D Draw(int n, System.Func<float, float, float> a)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float v = Mathf.Clamp01(a((x + 0.5f) / n, (y + 0.5f) / n));
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(v * 255f));
                }
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }
        const float AA = 0.02f;
        static float Circle(float x, float y, float cx, float cy, float r) { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); return Mathf.Clamp01((r - d) / AA + 0.5f); }
        static float Ring(float x, float y, float cx, float cy, float r, float w) { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); return Mathf.Clamp01((w - Mathf.Abs(d - r)) / AA + 0.5f); }
        static float Box(float x, float y, float cx, float cy, float hx, float hy) => Mathf.Min(Mathf.Clamp01((hx - Mathf.Abs(x - cx)) / AA + 0.5f), Mathf.Clamp01((hy - Mathf.Abs(y - cy)) / AA + 0.5f));
        static float Tri(float x, float y) { float h = y - 0.52f; if (h < 0f || h > 0.26f) return 0f; float half = 0.24f * (1f - h / 0.26f); return Mathf.Clamp01((half - Mathf.Abs(x - 0.5f)) / AA + 0.5f); }
    }
}
