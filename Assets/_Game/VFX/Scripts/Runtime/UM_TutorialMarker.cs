// Ultimo Miglio - marker del tutorial (Nonna Ada, issue #4, parte VFX).
// 1) anello a terra sul bersaglio (shader RingFill, giallo sole #F4C430, bordo bruno) che pulsa lentamente;
// 2) freccia guida A TERRA davanti a Nina che punta verso il bersaglio (shader GroundArrow, chevron che scorrono).
// Niente oggetti fluttuanti a metà schermo (QA #44). WebGL: 2 quad trasparenti, 0 luci, 0 particelle, 2 draw call.
// API: UM_TutorialMarker.Show(target[, player]) / Show(Vector3) / Hide(). Si crea da solo se manca in scena.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    [DisallowMultipleComponent]
    public class UM_TutorialMarker : MonoBehaviour
    {
        public static UM_TutorialMarker Instance { get; private set; }

        [Header("Materiali (assegnati dal menu VFX; se vuoti si creano da Shader.Find)")]
        public Material ringMaterial;   // M_FX_TutorialRing (UltimoMiglio/FX/RingFill)
        public Material arrowMaterial;  // M_FX_TutorialArrow (UltimoMiglio/FX/GroundArrow)
        [Header("Anello sul bersaglio")]
        public float ringSize = 2.2f;          // diametro quad (m); raggio visivo ~0,9 m, più largo dei pad
        public float pulseSpeed = 1.6f;        // pulsazioni/s (lento, guida §9 "pulse lento")
        public float pulseScale = 0.08f;
        [Header("Freccia a terra")]
        public Transform player;               // Nina; se vuoto la freccia resta nascosta
        public float arrowDistance = 1.0f;     // dal centro di Nina verso il bersaglio
        public float arrowLength = 0.9f, arrowWidth = 0.7f;
        public float bobAmplitude = 0.12f, bobSpeed = 2.4f;
        public float hideArrowWithin = 2.0f;   // vicino al bersaglio basta l'anello
        public float groundY = 0.035f, fadeSpeed = 6f;

        static readonly int PulseId = Shader.PropertyToID("_Pulse"), AlphaId = Shader.PropertyToID("_Alpha"), ColorId = Shader.PropertyToID("_Color");
        Transform _ring, _arrow, _target; Vector3 _point; bool _visible, _usePoint; float _alpha, _arrowAlpha;
        Renderer _ringR, _arrowR; MaterialPropertyBlock _mpb;

        public static UM_TutorialMarker Ensure()
        {
            if (Instance) return Instance;
            Instance = FindAnyObjectByType<UM_TutorialMarker>();
            if (!Instance) Instance = new GameObject("UM_TutorialMarker").AddComponent<UM_TutorialMarker>();
            return Instance;
        }
        public static void Show(Transform target, Transform player = null) { var m = Ensure(); m._target = target; m._usePoint = false; if (player) m.player = player; m._visible = target; m.SnapIfHidden(); }
        public static void Show(Vector3 point, Transform player = null) { var m = Ensure(); m._target = null; m._usePoint = true; m._point = point; if (player) m.player = player; m._visible = true; m.SnapIfHidden(); }
        public static void Hide() { if (Instance) Instance._visible = false; }
        public static bool Visible => Instance && Instance._visible;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; _mpb = new MaterialPropertyBlock();
            if (!ringMaterial) ringMaterial = MakeMat("UltimoMiglio/FX/RingFill", true);
            if (!arrowMaterial) arrowMaterial = MakeMat("UltimoMiglio/FX/GroundArrow", false);
            _ring = MakeQuad("Ring", ringMaterial, out _ringR);
            _arrow = MakeQuad("Arrow", arrowMaterial, out _arrowR);
            SetActive(false);
        }
        void OnDestroy() { if (Instance == this) Instance = null; }

        static Material MakeMat(string shader, bool ring)
        {
            var sh = Shader.Find(shader); if (!sh) { Debug.LogWarning($"[UM VFX] shader {shader} non incluso nella build: assegnare il materiale (menu VFX)"); return null; }
            var m = new Material(sh) { name = shader + " (auto)" };
            if (ring) { m.SetFloat("_Fill", 1f); m.SetFloat("_Thickness", 0.09f); m.SetColor("_Color", UM_Palette.UpgradeSole); m.SetColor("_GlowColor", UM_Palette.UpgradeLimone); }
            else { m.SetColor("_Color", UM_Palette.UpgradeSole); m.SetColor("_EdgeColor", UM_Palette.Bruno); }
            return m;
        }

        Transform MakeQuad(string n, Material mat, out Renderer r)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var mesh = new Mesh { name = "UM_GroundQuad" };
            mesh.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 }; mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.enabled = mat; // niente materiale = niente quad magenta
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            r = mr; return go.transform;
        }

        void SnapIfHidden() { if (_alpha <= 0.01f) { _alpha = 0; UpdatePose(0); } }
        void SetActive(bool on) { if (_ring) _ring.gameObject.SetActive(on); if (_arrow) _arrow.gameObject.SetActive(on && _arrowAlpha > 0.01f); }

        Vector3 TargetPos => _target ? _target.position : _point;

        void LateUpdate()
        {
            if (_visible && !_usePoint && _target == null) _visible = false; // bersaglio distrutto
            float dt = Time.unscaledDeltaTime;
            _alpha = Mathf.MoveTowards(_alpha, _visible ? 1f : 0f, dt * fadeSpeed);
            bool arrowWanted = _visible && player && Flat(TargetPos - player.position).magnitude > hideArrowWithin;
            _arrowAlpha = Mathf.MoveTowards(_arrowAlpha, arrowWanted ? 1f : 0f, dt * fadeSpeed);
            SetActive(_alpha > 0.001f);
            if (_alpha <= 0.001f) return;
            UpdatePose(Time.time);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void UpdatePose(float t)
        {
            var tp = TargetPos;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed * Mathf.PI * 2f);
            float s = ringSize * (1f + pulseScale * pulse) * Mathf.Lerp(0.6f, 1f, _alpha);
            _ring.position = new Vector3(tp.x, groundY, tp.z); _ring.localScale = new Vector3(s, 1, s); _ring.rotation = Quaternion.identity;
            if (_ringR) { _ringR.GetPropertyBlock(_mpb); _mpb.SetFloat(PulseId, pulse); var c = UM_Palette.UpgradeSole; c.a = _alpha; _mpb.SetColor(ColorId, c); _ringR.SetPropertyBlock(_mpb); }
            if (!player || !_arrow) return;
            var d = Flat(tp - player.position); if (d.sqrMagnitude < 1e-4f) return;
            d.Normalize();
            float bob = Mathf.Sin(t * bobSpeed * Mathf.PI * 2f) * bobAmplitude;
            var p = player.position + d * (arrowDistance + bob);
            _arrow.position = new Vector3(p.x, groundY + 0.005f, p.z);
            _arrow.rotation = Quaternion.LookRotation(d, Vector3.up);
            _arrow.localScale = new Vector3(arrowWidth, 1, arrowLength);
            if (_arrowR) { _arrowR.GetPropertyBlock(_mpb); _mpb.SetFloat(AlphaId, _arrowAlpha * _alpha); _arrowR.SetPropertyBlock(_mpb); }
        }
    }
}
