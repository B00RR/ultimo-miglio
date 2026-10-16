using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>Micro-animazioni della direzione C (solo scala e rotazione, niente blur):
    /// l'insegna oscilla appesa ai chiodi, il cartellino del giorno ruota, la moneta brilla,
    /// LN e giorno fanno pop, l'ordine si ritrae a icona dopo 3 secondi e si riapre se il testo cambia.</summary>
    public sealed class InsegnaMotion : MonoBehaviour
    {
        public const float RetractSec = 3f;
        public const float PopSec = 0.4f;

        HudController hud;
        string lastText;
        float retractAt = -1f;
        bool open = true;
        bool sawText;
        float swing, spark;
        float signPop = -1f, dayPop = -1f;

        void OnEnable() { hud = GetComponent<HudController>(); }

        void Update()
        {
            if (hud == null || !hud.IsBound || hud.Root == null) return;
            var root = hud.Root;
            TickSwing(root.Q("currency-pill"), root.Q("day-group"));
            TickSpark(root);
            TickOrder(root);
        }

        /// <summary>La cattura e i test possono forzare biglietto aperto o icona senza aspettare i 3 secondi.</summary>
        public void SetOrderOpen(bool value)
        {
            if (hud == null) hud = GetComponent<HudController>();
            var label = hud != null && hud.Root != null ? hud.Root.Q<Label>("tutorial-objective") : null;
            if (label != null) { lastText = label.text; sawText = true; }
            open = value;
            retractAt = value ? Time.unscaledTime + RetractSec : -1f;
            var row = hud != null && hud.Root != null ? hud.Root.Q("order-row") : null;
            if (row != null) Apply(row);
        }

        void TickSwing(VisualElement sign, VisualElement tag)
        {
            swing += Time.unscaledDeltaTime;
            if (sign != null)
            {
                if (sign.ClassListContains("sign--pop"))
                {
                    if (signPop < 0f) signPop = 0f;
                    signPop += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(signPop / PopSec);
                    float k = u < 0.45f ? u / 0.45f : 1f - (u - 0.45f) / 0.55f;
                    sign.style.rotate = new Rotate(new Angle(Mathf.Lerp(0f, -3.2f, k)));
                    float s = Mathf.Lerp(1f, 1.08f, k);
                    sign.style.scale = new Scale(new Vector2(s, s));
                    var coin = sign.Q("sign-coin");
                    if (coin != null) coin.style.scale = new Scale(new Vector2(Mathf.Lerp(1f, 1.16f, k), Mathf.Lerp(1f, 1.16f, k)));
                    if (u >= 1f)
                    {
                        sign.RemoveFromClassList("sign--pop");
                        signPop = -1f;
                        sign.style.scale = new StyleScale(StyleKeyword.Null);
                        if (coin != null) coin.style.scale = new StyleScale(StyleKeyword.Null);
                    }
                }
                else if (!HudReveal.IsHidden("hud.pillolaValute"))
                    sign.style.rotate = new Rotate(new Angle(Mathf.Sin(swing * 1.3f) * 1.3f));
            }
            if (tag == null) return;
            if (tag.ClassListContains("day--pop"))
            {
                if (dayPop < 0f) dayPop = 0f;
                dayPop += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(dayPop / 0.5f);
                float flip = u < 0.35f ? Mathf.Lerp(1f, 0.08f, u / 0.35f) : Mathf.Lerp(0.08f, 1f, (u - 0.35f) / 0.65f);
                float ang = u < 0.55f ? Mathf.Lerp(6f, -12f, u / 0.55f) : Mathf.Lerp(-12f, 4f, (u - 0.55f) / 0.45f);
                float sc = u > 0.4f && u < 0.75f ? 1.12f : 1f;
                tag.style.rotate = new Rotate(new Angle(ang));
                tag.style.scale = new Scale(new Vector2(flip * sc, sc));
                if (u >= 1f)
                {
                    tag.RemoveFromClassList("day--pop");
                    dayPop = -1f;
                    tag.style.scale = new StyleScale(StyleKeyword.Null);
                }
            }
            else
                tag.style.rotate = new Rotate(new Angle(4f + Mathf.Sin(swing * 0.8f) * 1.5f));
        }

        void TickSpark(VisualElement root)
        {
            spark += Time.unscaledDeltaTime;
            var a = root.Q("sign-spark-a");
            var b = root.Q("sign-spark-b");
            if (a != null) a.style.opacity = 0.25f + 0.75f * Mathf.Abs(Mathf.Sin(spark * 3.2f));
            if (b != null) b.style.opacity = 0.15f + 0.85f * Mathf.Abs(Mathf.Sin(spark * 3.2f + 1.4f));
        }

        void TickOrder(VisualElement root)
        {
            var row = root.Q("order-row");
            var label = root.Q<Label>("tutorial-objective");
            if (row == null || label == null) return;
            bool visible = label.resolvedStyle.display != DisplayStyle.None && !string.IsNullOrEmpty(label.text);
            row.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            if (!sawText || label.text != lastText)
            {
                sawText = true;
                lastText = label.text;
                open = true;
                retractAt = Time.unscaledTime + RetractSec;
                Apply(row);
            }
            else if (open && retractAt >= 0f && Time.unscaledTime >= retractAt)
            {
                open = false;
                retractAt = -1f;
                Apply(row);
            }
        }

        void Apply(VisualElement row)
        {
            row.EnableInClassList("order--open", open);
            row.EnableInClassList("order--icon", !open);
            var el = row.Q(open ? "order-ticket" : "order-chip");
            if (el == null) return;
            el.style.scale = new Scale(new Vector2(0.72f, 0.72f));
            el.schedule.Execute(() => { if (el.panel != null) el.style.scale = new Scale(Vector2.one); }).StartingIn(20);
        }
    }
}
