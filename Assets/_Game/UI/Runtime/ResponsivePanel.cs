using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Responsive scaling for a UIDocument (WebGL any window size, Android portrait).
    /// USS is authored on a 1080x1920 portrait reference. Scale is driven by the SHORTER side so text stays
    /// readable on landscape desktop:
    ///   portrait : scale = min(W/1080, H/1920)
    ///   landscape: scale = H / landscapeShortSideRef   (1080p → 0.9, 768 → 0.64, 600 → 0.5)
    /// clamped to [minScale, maxScale]. Adds "um-landscape"/"um-portrait" classes on the root.
    /// The PanelSettings asset is cloned at runtime so nothing in the project gets dirtied.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(-50)]
    public sealed class ResponsivePanel : MonoBehaviour
    {
        public const float RefW = 1080f, RefH = 1920f;
        [SerializeField] float landscapeShortSideRef = 1200f;
        [SerializeField] float minScale = 0.42f, maxScale = 2.5f;

        UIDocument doc; PanelSettings runtimeSettings;
        Vector2Int lastSize = new Vector2Int(-1, -1);

        /// <summary>Overrides Screen size (used by batch captures that render into a RenderTexture).</summary>
        public static Vector2Int? SizeOverride;
        public static Vector2Int CurrentSize => SizeOverride ?? new Vector2Int(Screen.width, Screen.height);
        public float Scale { get; private set; } = 1f;
        public bool Landscape { get; private set; }

        void Awake()
        {
            doc = GetComponent<UIDocument>();
            if (doc.panelSettings != null && runtimeSettings == null)
            {
                runtimeSettings = Instantiate(doc.panelSettings);
                runtimeSettings.name = doc.panelSettings.name + " (runtime)";
                doc.panelSettings = runtimeSettings;
            }
            Apply(true);
        }

        void Update()
        {
            Apply(false);
            var root = doc ? doc.rootVisualElement : null;
            if (root != null && !root.ClassListContains(Landscape ? "um-landscape" : "um-portrait"))
            { root.EnableInClassList("um-landscape", Landscape); root.EnableInClassList("um-portrait", !Landscape); }
        }

        public static float ComputeScale(int w, int h, float landscapeRef, float min, float max)
        {
            if (w <= 0 || h <= 0) return 1f;
            float s = w > h ? h / landscapeRef : Mathf.Min(w / RefW, h / RefH);
            return Mathf.Clamp(s, min, max);
        }

        void Apply(bool force)
        {
            var size = CurrentSize;
            if (!force && size == lastSize) return;
            lastSize = size;
            Landscape = size.x > size.y;
            Scale = ComputeScale(size.x, size.y, landscapeShortSideRef, minScale, maxScale);
            var ps = doc ? doc.panelSettings : null;
            if (ps != null)
            {
                ps.scaleMode = PanelScaleMode.ConstantPixelSize;
                ps.scale = Scale;
            }
            var root = doc ? doc.rootVisualElement : null;
            if (root != null)
            {
                root.EnableInClassList("um-landscape", Landscape);
                root.EnableInClassList("um-portrait", !Landscape);
            }
        }
    }
}
