using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.World
{
    /// <summary>
    /// Base for screen-projected world labels (pad card, stack counter, Cassa card, x2 prompt, buff chip).
    /// Clones a UXML template into the HUD "world-layer" and keeps it above a world anchor. Cheap: one
    /// WorldToScreenPoint per frame, hidden when behind the camera or disabled.
    /// </summary>
    public abstract class WorldAnchoredElement : MonoBehaviour
    {
        [SerializeField] protected UltimoMiglio.UI.HudController hud;
        [SerializeField] protected VisualTreeAsset template;
        [SerializeField] protected Transform anchor;
        [SerializeField] protected Vector3 worldOffset = new Vector3(0, 2f, 0);
        [SerializeField] protected Camera cam;

        protected VisualElement Element { get; private set; }
        /// <summary>Nina: big cards show only within ProximityM of her (else the compact POI chip stays).</summary>
        public static Transform Focus; public static float ProximityM = 3f;
        protected virtual bool UseProximity => true;

        protected virtual void OnEnable() => TryCreate();

        // OnEnable order across objects is undefined: if this runs before HudController.OnEnable, hud.Root is null
        // and the card/counter would never appear. Retried from LateUpdate until it succeeds.
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

        protected virtual void LateUpdate()
        {
            if (Element == null) TryCreate();
            if (Element == null || cam == null || Element.panel == null) return;
            Vector3 sp = cam.WorldToScreenPoint(anchor.position + worldOffset);
            bool visible = sp.z > 0 && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
            if (visible && UseProximity && Focus) { var d = Focus.position - anchor.position; d.y = 0; visible = d.sqrMagnitude <= ProximityM * ProximityM; } // anchor off-screen: hide (edge arrow guides), on-screen: clamp
            Element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            var p = UltimoMiglio.UI.SafeAreaUtil.ScreenToPanel(Element.panel, sp);
            // clamp inside the panel (USS centers with translate -50% -100%): never cut at the screen edges
            // HUD v2: clamp to the SAFE area with 8 dp margin; near the top edge flip the card below the anchor.
            var pr = Element.panel.visualTree.layout; float w = Element.layout.width, h = Element.layout.height, m = 8f;
            var safe = hud && hud.SafeRoot != null ? hud.SafeRoot.resolvedStyle : null;
            float sl = safe != null ? safe.paddingLeft : 0f, st = safe != null ? safe.paddingTop : 0f, sr = safe != null ? safe.paddingRight : 0f, sb = safe != null ? safe.paddingBottom : 0f;
            bool flip = false;
            if (!float.IsNaN(w) && pr.width > 0)
            {
                p.x = Mathf.Clamp(p.x, sl + w * 0.5f + m, Mathf.Max(sl + w * 0.5f + m, pr.width - sr - w * 0.5f - m));
                if (p.y - h < st + m) { flip = true; p.y = Mathf.Min(p.y + h + 2f * m, pr.height - sb - m); } // would cross the top: hang below
                p.y = Mathf.Clamp(p.y, st + h + m, Mathf.Max(st + h + m, pr.height - sb - m));
            }
            Element.EnableInClassList("world-anchored--flipped", flip);
            Element.style.left = p.x; Element.style.top = p.y;
        }
    }
}
