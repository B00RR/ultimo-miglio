using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.World
{
    /// <summary>Card above the Cassa: "240 / 352 LN", terracotta "PIENA" when full, optional "x2 con video" prompt (04_UI §3.4).</summary>
    public class CassaCard : WorldAnchoredElement
    {
        public event System.Action DoubleWithVideoTapped;
        Label amountL, stateL; VisualElement x2;

        protected override void Bind(VisualElement e)
        {
            amountL = e.Q<Label>("cassa-amount"); stateL = e.Q<Label>("cassa-state"); x2 = e.Q("cassa-x2");
            if (x2 != null) { x2.pickingMode = PickingMode.Position; x2.RegisterCallback<ClickEvent>(_ => DoubleWithVideoTapped?.Invoke()); }
            ShowDoublePrompt(false);
        }

        public void SetAmount(double amount, double capacity)
        {
            if (Element == null) return;
            bool full = capacity > 0 && amount >= capacity;
            Element.style.visibility = amount > 0 ? Visibility.Visible : Visibility.Hidden;
            amountL.text = UltimoMiglio.UI.HudFormat.Amount(amount) + " / " + UltimoMiglio.UI.HudFormat.Amount(capacity);
            stateL.text = full ? "PIENA" : "OFFLINE · PASSA A RITIRARE";
            Element.EnableInClassList("cassa--full", full);
        }

        /// <summary>Only when an ad is ready and the daily cap allows (decided by game/services, not UI).</summary>
        public void ShowDoublePrompt(bool show) { if (x2 != null) x2.style.display = show ? DisplayStyle.Flex : DisplayStyle.None; }
    }
}
