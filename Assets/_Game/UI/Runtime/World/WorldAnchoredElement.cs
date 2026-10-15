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
            bool visible = sp.z > 0;
            Element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            var p = UltimoMiglio.UI.SafeAreaUtil.ScreenToPanel(Element.panel, sp);
            Element.style.left = p.x; Element.style.top = p.y; // USS centers with translate -50% -100%
        }
    }
}
