using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.World
{
    /// <summary>
    /// Base for screen-projected world labels. Big cards (UpgradePadCard, CassaCard) set RequireOnPad and only show
    /// while Nina stands on that pad (Occupied via reflection, because UI cannot reference Gameplay). Compact PadTag
    /// stays always. Max one card at a time because only one ProximityZone is Active.
    /// </summary>
    public abstract class WorldAnchoredElement : MonoBehaviour
    {
        [SerializeField] protected UltimoMiglio.UI.HudController hud;
        [SerializeField] protected VisualTreeAsset template;
        [SerializeField] protected Transform anchor;
        [SerializeField] protected Vector3 worldOffset = new Vector3(0, 2f, 0);
        [SerializeField] protected Camera cam;

        protected VisualElement Element { get; private set; }
        /// <summary>True for big cards: visible only while Nina is ON this pad.</summary>
        protected virtual bool RequireOnPad => false;
        protected bool padProximityRequired, padVisible;
        public virtual void ShowOnPad(bool on) { padProximityRequired = true; padVisible = on; }

        protected virtual void OnEnable() => TryCreate();

        void TryCreate()
        {
            if (Element != null) return;
            if (!hud) hud = FindFirstObjectByType<UltimoMiglio.UI.HudController>();
            if (!cam) cam = Camera.main;
            if (!anchor) anchor = transform;
            if (hud == null || hud.Root == null || template == null) return;
            var layer = hud.Root.Q("world-layer") ?? hud.Root;
            Element = template.Instantiate();
            Element.AddToClassList("world-anchored");
            Element.pickingMode = PickingMode.Ignore;
            layer.Add(Element);
            Bind(Element);
        }

        protected virtual void OnDisable() { Element?.RemoveFromHierarchy(); Element = null; }
        protected abstract void Bind(VisualElement e);

        bool PadOccupied()
        {
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (!mb) continue;
                var prop = mb.GetType().GetProperty("Occupied");
                if (prop != null && prop.PropertyType == typeof(bool) && (bool)prop.GetValue(mb)) return true;
            }
            return false;
        }

        protected virtual void LateUpdate()
        {
            if (Element == null) TryCreate();
            if (Element == null || cam == null || Element.panel == null) return;
            Vector3 sp = cam.WorldToScreenPoint(anchor.position + worldOffset);
            bool visible = sp.z > 0 && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
            if (visible && (RequireOnPad || padProximityRequired)) visible = padProximityRequired ? padVisible : PadOccupied();
            Element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            var p = UltimoMiglio.UI.SafeAreaUtil.ScreenToPanel(Element.panel, sp);
            var pr = Element.panel.visualTree.layout; float w = Element.layout.width, h = Element.layout.height, m = 8f;
            var safe = hud && hud.SafeRoot != null ? hud.SafeRoot.resolvedStyle : null;
            float sl = safe != null ? safe.paddingLeft : 0f, st = safe != null ? safe.paddingTop : 0f, sr = safe != null ? safe.paddingRight : 0f, sb = safe != null ? safe.paddingBottom : 0f;
            bool flip = false;
            if (!float.IsNaN(w) && pr.width > 0)
            {
                p.x = Mathf.Clamp(p.x, sl + w * 0.5f + m, Mathf.Max(sl + w * 0.5f + m, pr.width - sr - w * 0.5f - m));
                if (p.y - h < st + m) { flip = true; p.y = Mathf.Min(p.y + h + 2f * m, pr.height - sb - m); }
                p.y = Mathf.Clamp(p.y, st + h + m, Mathf.Max(st + h + m, pr.height - sb - m));
            }
            Element.EnableInClassList("world-anchored--flipped", flip);
            Element.style.left = p.x; Element.style.top = p.y;
        }
    }
}
