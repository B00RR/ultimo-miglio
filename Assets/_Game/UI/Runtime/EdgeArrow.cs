using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Off-screen objective arrow (04_UI §2.4). Projects the target, and if outside the view clamps it to the
    /// safe area minus a 24 dp margin on the Nina→target line; shows icon class + "Name · 12 m".
    /// When on screen it becomes a small marker above the object (optional) and then hides.
    /// </summary>
    public class EdgeArrow : MonoBehaviour
    {
        [SerializeField] HudController hud;
        [SerializeField] Camera cam;
        [SerializeField] Transform nina;
        [SerializeField] float marginDp = 64f;
        [SerializeField] float markerHeight = 2.2f;
        [SerializeField] bool showMarkerWhenVisible = false; // QA #44: objective only in HUD top line

        Transform target; string targetName; string iconClass;
        VisualElement arrow, marker, arrowHead; Label arrowLabel;

        public Transform Target => target;

        void Start() { Bind(); Clear(); }

        void Bind()
        {
            if (!hud || hud.Root == null) return;
            arrow = hud.Root.Q("edge-arrow"); arrowHead = hud.Root.Q("edge-arrow-head");
            arrowLabel = hud.Root.Q<Label>("edge-arrow-label"); marker = hud.Root.Q("target-marker");
        }

        /// <summary>Point at a world target. iconClass: e.g. "icon-customer", "icon-van", "icon-safe", "icon-pad".</summary>
        public void SetTarget(Transform t, string displayName, string icon = null)
        {
            if (arrow == null) Bind();
            if (arrow != null && iconClass != null) arrow.RemoveFromClassList(iconClass);
            target = t; targetName = displayName; iconClass = icon;
            if (arrow != null && icon != null) arrow.AddToClassList(icon);
        }

        public void Clear() { target = null; Show(arrow, false); Show(marker, false); }

        void LateUpdate()
        {
            if (!target || !cam || arrow == null || arrow.panel == null) { Show(arrow, false); Show(marker, false); return; }
            Vector3 sp = cam.WorldToScreenPoint(target.position);
            bool behind = sp.z < 0; if (behind) { sp.x = Screen.width - sp.x; sp.y = Screen.height - sp.y; }
            float m = marginDp * SafeAreaUtil.DpToPx;
            Rect sa = SafeAreaUtil.SafeAreaPx;
            Rect inner = new Rect(sa.xMin + m, sa.yMin + m, sa.width - 2 * m, sa.height - 2 * m);
            bool onScreen = !behind && inner.Contains(new Vector2(sp.x, sp.y));

            if (onScreen)
            {
                Show(arrow, false);
                Show(marker, false); // QA #44: no floating waypoint mid-screen
                return;
            }
            Show(marker, false);
            Vector2 c = inner.center;
            Vector2 dir = new Vector2(sp.x, sp.y) - c;
            if (dir.sqrMagnitude < 1e-4f) dir = Vector2.up;
            float tx = Mathf.Abs(dir.x) > 1e-4f ? (inner.width * 0.5f) / Mathf.Abs(dir.x) : float.MaxValue;
            float ty = Mathf.Abs(dir.y) > 1e-4f ? (inner.height * 0.5f) / Mathf.Abs(dir.y) : float.MaxValue;
            Vector2 edge = c + dir * Mathf.Min(tx, ty);
            var p = SafeAreaUtil.ScreenToPanel(arrow.panel, edge);
            arrow.style.left = p.x; arrow.style.top = p.y;
            // #205: l'angolo è la direzione sullo schermo da Kaia al bersaglio (la rotazione della camera
            // è già dentro WorldToScreenPoint). Il centro dell'area sicura, mentre la camera sta ancora
            // scorrendo, non coincide con Kaia e girava la freccia (NonnoIntro, −22° invece di −134°).
            Vector2 aim = dir;
            if (!nina) { var who = GameObject.Find("Nina"); if (who) nina = who.transform; }
            if (nina)
            {
                Vector3 ns = cam.WorldToScreenPoint(nina.position);
                Vector3 ts = cam.WorldToScreenPoint(target.position);
                aim = new Vector2(ts.x - ns.x, ts.y - ns.y);
                if (aim.sqrMagnitude < 1e-4f) aim = dir;
            }
            float ang = Mathf.Atan2(-aim.y, aim.x) * Mathf.Rad2Deg; // panel y is down
            if (arrowHead != null) arrowHead.style.rotate = new Rotate(new Angle(ang));
            if (arrowLabel != null) arrowLabel.style.display = DisplayStyle.None; // QA v2: arrow only, no ARRIVI/BANCONE · Xm chip
            Show(arrow, true);
        }

        static void Show(VisualElement e, bool v) { if (e != null) e.style.display = v ? DisplayStyle.Flex : DisplayStyle.None; }
    }
}
