using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>Screen/panel/dp conversions shared by HUD, joystick and edge arrow.</summary>
    public static class SafeAreaUtil
    {
        /// <summary>Pixels per dp (Android: dpi/160). Falls back to 160 dpi when unknown (editor).</summary>
        public static float DpToPx
        {
            get { float dpi = Screen.dpi; return Mathf.Max(1f, (dpi > 1f ? dpi : 160f) / 160f); } // desktop browsers report ~96 dpi: never below 1 px/dp
        }

        /// <summary>Safe area in screen pixels, origin bottom-left (Unity convention).</summary>
        public static Rect SafeAreaPx => Screen.safeArea;

        /// <summary>Applies the safe area as padding on <paramref name="safeRoot"/> (panel coordinates, top-left origin).</summary>
        public static void ApplyPadding(VisualElement safeRoot)
        {
            if (safeRoot?.panel == null) return;
            if (ResponsivePanel.SizeOverride.HasValue) { safeRoot.style.paddingLeft = safeRoot.style.paddingTop = safeRoot.style.paddingRight = safeRoot.style.paddingBottom = 0; return; }
            Rect sa = Screen.safeArea;
            var tl = RuntimePanelUtils.ScreenToPanel(safeRoot.panel, new Vector2(sa.xMin, Screen.height - sa.yMax));
            var br = RuntimePanelUtils.ScreenToPanel(safeRoot.panel, new Vector2(sa.xMax, Screen.height - sa.yMin));
            var full = RuntimePanelUtils.ScreenToPanel(safeRoot.panel, new Vector2(Screen.width, Screen.height));
            safeRoot.style.paddingLeft = tl.x;
            safeRoot.style.paddingTop = tl.y;
            safeRoot.style.paddingRight = Mathf.Max(0, full.x - br.x);
            safeRoot.style.paddingBottom = Mathf.Max(0, full.y - br.y);
        }

        /// <summary>Screen pixel (bottom-left origin) to panel position (top-left origin).</summary>
        public static Vector2 ScreenToPanel(IPanel panel, Vector2 screenPx) =>
            RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPx.x, Screen.height - screenPx.y));
    }
}
