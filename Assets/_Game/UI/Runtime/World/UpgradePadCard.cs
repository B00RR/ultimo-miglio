using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.World
{
    /// <summary>Cream card above an UpgradePad: icon, title, paid / total, progress bar, locked "Cap. N" (04_UI §2.2).
    /// Hook: UpgradePad.OnPaymentProgress(paid,total) → SetProgress; prices come from the pad (data), never from here.</summary>
    public class UpgradePadCard : WorldAnchoredElement
    {
        protected override bool RequireOnPad => true;
        public static UpgradePadCard ActiveCard { get; private set; }
        public override void ShowOnPad(bool on)
        {
            if (on) { if (ActiveCard != null && ActiveCard != this) ActiveCard.ShowOnPad(false); ActiveCard = this; }
            else if (ActiveCard == this) ActiveCard = null;
            base.ShowOnPad(on);
        }
        [SerializeField] string title = "ASSUMI NONNO";
        [SerializeField] string iconClass = "icon-robot";
        [SerializeField] ZoneFillRing padFill; // optional: pad fills with terracotta from below

        Label titleL, priceL, lockL; VisualElement fill, icon;
        double paid, total; int lockedChapter;

        protected override void Bind(VisualElement e)
        {
            titleL = e.Q<Label>("pad-title"); priceL = e.Q<Label>("pad-price"); lockL = e.Q<Label>("pad-lock");
            fill = e.Q("pad-progress-fill"); icon = e.Q("pad-icon");
            Refresh();
        }

        public void SetTitle(string t, string icon = null) { title = t; if (icon != null) iconClass = icon; Refresh(); }
        public void SetProgress(double paidLn, double totalLn) { paid = paidLn; total = totalLn; lockedChapter = 0; Refresh(); }
        public void SetLocked(int chapter) { lockedChapter = chapter; Refresh(); }

        void Refresh()
        {
            if (Element == null) return;
            titleL.text = title;
            if (icon != null) { icon.ClearClassList(); icon.AddToClassList("icon"); icon.AddToClassList(iconClass); }
            bool locked = lockedChapter > 0;
            Element.EnableInClassList("pad-card--locked", locked);
            lockL.style.display = locked ? DisplayStyle.Flex : DisplayStyle.None;
            lockL.text = locked ? "Cap. " + lockedChapter : "";
            priceL.style.display = locked ? DisplayStyle.None : DisplayStyle.Flex;
            priceL.text = UltimoMiglio.UI.HudFormat.Amount(paid) + " / " + UltimoMiglio.UI.HudFormat.Amount(total) + " LN";
            float pct = total > 0 ? Mathf.Clamp01((float)(paid / total)) : 0f;
            fill.style.width = Length.Percent(pct * 100f);
            if (padFill) padFill.SetFill(pct);
        }
    }
}
