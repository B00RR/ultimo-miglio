using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.World
{
    /// <summary>"3/5" pill above Nina's stack; terracotta pulsing "MAX 5/5" when full (04_UI §2.3).
    /// Also hosts the rewarded buff chip (icon + timer) next to the stack. Capacity comes from gameplay/data.</summary>
    public class StackCounter : WorldAnchoredElement
    {
        protected override bool RequireOnPad => false;
        
        Label countL, buffL; VisualElement buff;
        public event System.Action BecameFull;
        bool wasFull;

        protected override void Bind(VisualElement e)
        {
            countL = e.Q<Label>("stack-count"); buff = e.Q("buff-chip"); buffL = e.Q<Label>("buff-timer");
            if (buff != null) buff.style.display = DisplayStyle.None;
            SetCount(0, 0);
        }

        /// <summary>Hidden when count == 0. capacity from the Zaino upgrade level (data).</summary>
        public void SetCount(int count, int capacity)
        {
            if (Element == null) return;
            bool full = capacity > 0 && count >= capacity;
            Element.style.visibility = count > 0 ? Visibility.Visible : Visibility.Hidden;
            countL.text = (full ? "MAX " : "") + count + "/" + capacity;
            Element.EnableInClassList("stack--max", full);
            if (full && !wasFull) BecameFull?.Invoke();
            wasFull = full;
        }

        public void SetBuff(string iconClass, float secondsLeft)
        {
            if (buff == null) return;
            bool on = secondsLeft > 0;
            buff.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
            if (!on) return;
            var ic = buff.Q("buff-icon"); if (ic != null) { ic.ClearClassList(); ic.AddToClassList("icon"); ic.AddToClassList(iconClass); }
            buffL.text = UltimoMiglio.UI.HudFormat.Clock(secondsLeft);
        }
    }
}
