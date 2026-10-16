// Ultimo Miglio - look premium v2.0 senza post-processing (WebGL: post OFF).
// Imposta i globali dello shader UM_ToonDiorama (grading in-shader + vignetta a terra) e crea un cielo a gradiente
// dietro la scena (quad figlio della camera, colori giorno -> tramonto da UM_DayCycle). Costo: ~10 ALU/pixel + 1 quad.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class UM_LookSettings : MonoBehaviour
    {
        [Header("Grading in-shader (attivo quando il post e' spento)")]
        public bool forceInShaderGrade = false;
        [Range(0.5f, 1.5f)] public float exposure = 1.00f;
        [Range(0.8f, 1.5f)] public float contrast = 1.14f;
        [Range(0.5f, 1.8f)] public float saturation = 1.14f; // +~12% guida
        [Range(0f, 1f)] public float glow = 0.12f; // niente wash arancio
        public Color lift = new Color(0.42f, 0.36f, 0.32f); // lift caldo neutro (no viola/arancio)
        [Header("Vignetta a terra (world-space)")]
        public Transform focus;                                // Nina o centro stanza; vuoto = questo transform
        public float innerRadius = 4.5f, range = 6f;
        [Range(0f, 1f)] public float vignetteDark = 0.25f;
        public Color vignetteColor = new Color(0.62f, 0.471f, 0.451f); // #9E7873: bordi verso l'ombra calda, mai viola/grigio
        [Header("Cielo")]
        public Camera targetCamera;
        public Material skyMaterial;                           // UltimoMiglio/FX/SkyGradient (M_FX_Sky)
        // v2.1 (#48): niente azzurro. Giorno crema #F3E3C3 -> pesca #F6D7B0 -> tufo #E9D2A8
        public Color skyTopDay = new Color(0.953f, 0.890f, 0.765f), skyHorizonDay = new Color(0.945f, 0.875f, 0.760f), skyBottomDay = new Color(0.914f, 0.824f, 0.659f); // orizzonte meno pesca
        // Tramonto: albicocca #E8B48A -> arancia #E8913A -> pesca #F4B88E (niente viola)
        public Color skyTopSunset = new Color(0.910f, 0.706f, 0.541f), skyHorizonSunset = new Color(0.910f, 0.569f, 0.227f), skyBottomSunset = new Color(0.957f, 0.722f, 0.557f);

        static readonly int GradeId = Shader.PropertyToID("_UM_Grade"), ExpId = Shader.PropertyToID("_UM_Exposure"),
            ConId = Shader.PropertyToID("_UM_Contrast"), SatId = Shader.PropertyToID("_UM_Saturation"), GlowId = Shader.PropertyToID("_UM_Glow"),
            LiftId = Shader.PropertyToID("_UM_Lift"), FocusId = Shader.PropertyToID("_UM_Focus"), VRangeId = Shader.PropertyToID("_UM_VignetteRange"),
            VDarkId = Shader.PropertyToID("_UM_VignetteDark"), VColId = Shader.PropertyToID("_UM_VignetteColor");
        static readonly int TopId = Shader.PropertyToID("_Top"), HorId = Shader.PropertyToID("_Horizon"), BotId = Shader.PropertyToID("_Bottom");

        GameObject _sky; MaterialPropertyBlock _mpb;

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void LateUpdate() { PushFocus(); FitSky(); PushSky(); }
        void OnDisable() { Shader.SetGlobalFloat(GradeId, 0f); Shader.SetGlobalFloat(VDarkId, 0f); }

        public void Apply()
        {
            bool grade = forceInShaderGrade || !UM_QualityTier.PostProcessingEnabled;
            Shader.SetGlobalFloat(GradeId, grade ? 1f : 0f);
            Shader.SetGlobalFloat(ExpId, exposure); Shader.SetGlobalFloat(ConId, contrast);
            Shader.SetGlobalFloat(SatId, saturation); Shader.SetGlobalFloat(GlowId, glow);
            Shader.SetGlobalColor(LiftId, lift.linear);
            Shader.SetGlobalFloat(VRangeId, range); Shader.SetGlobalFloat(VDarkId, vignetteDark);
            Shader.SetGlobalColor(VColId, vignetteColor.linear);
            PushFocus(); EnsureSky(); FitSky(); PushSky();
        }

        void PushFocus()
        {
            var f = focus ? focus.position : transform.position;
            Shader.SetGlobalVector(FocusId, new Vector4(f.x, f.y, f.z, innerRadius));
        }

        Camera Cam => targetCamera ? targetCamera : Camera.main;

        void EnsureSky()
        {
            if (_sky || !skyMaterial || !Cam) return;
            var t = Cam.transform.Find("UM_Sky");
            if (t) { _sky = t.gameObject; return; }
            _sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _sky.name = "UM_Sky"; _sky.hideFlags = HideFlags.DontSave;
            var col = _sky.GetComponent<Collider>(); if (col) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
            _sky.transform.SetParent(Cam.transform, false);
            var r = _sky.GetComponent<Renderer>(); r.sharedMaterial = skyMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }

        void FitSky()
        {
            var cam = Cam; if (!_sky || !cam) return;
            float d = cam.farClipPlane * 0.95f;
            float h = cam.orthographic ? cam.orthographicSize * 2f : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            _sky.transform.localPosition = new Vector3(0, 0, d);
            _sky.transform.localRotation = Quaternion.identity;
            _sky.transform.localScale = new Vector3(h * cam.aspect * 1.05f, h * 1.05f, 1f);
        }

        void PushSky()
        {
            if (!_sky) return;
            float g = UM_DayCycle.Instance ? UM_DayCycle.Instance.GoldenBlend : 0f;
            var r = _sky.GetComponent<Renderer>(); if (_mpb == null) _mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(_mpb);
            _mpb.SetColor(TopId, Color.Lerp(skyTopDay, skyTopSunset, g));
            _mpb.SetColor(HorId, Color.Lerp(skyHorizonDay, skyHorizonSunset, g));
            _mpb.SetColor(BotId, Color.Lerp(skyBottomDay, skyBottomSunset, g));
            r.SetPropertyBlock(_mpb);
        }
    }
}
