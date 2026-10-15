using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI.World
{
    /// <summary>Tipo di pad: decide icona, etichetta di default e colore (issue #33).</summary>
    public enum PadKind
    {
        Pacco = 0,   // ritiro / magazzino — teal
        Moneta = 1,  // cassa / acquisto — sun
        Cliente = 2  // bancone / consegna — menta (Lorenzo)
    }

    /// <summary>
    /// Cartellino sopra un pad (billboard: sempre rivolto alla camera perché proiettato nel layer HUD).
    /// Icona + etichetta corta (max 12 caratteri) e, sui pad di acquisto, prezzo grande "120 LN".
    /// Colora anche lo ZoneFillRing del pad con lo stesso colore del tipo.
    /// Contrasti (WCAG): bianco su teal scuro #3E5A61 ≈ 7,4:1; ink #3B2A20 su sun #F4C430 ≈ 8,9:1;
    /// ink su menta #8FC9A8 (consegna, Lorenzo). Upgrade VFX: giallo sole/limone.
    /// </summary>
    public class PadTag : WorldAnchoredElement
    {
        public const int MaxLabelChars = 12;

        public static readonly Color Teal = new Color32(0x5E, 0x7F, 0x86, 0xFF);
        public static readonly Color TealDark = new Color32(0x3E, 0x5A, 0x61, 0xFF);
        public static readonly Color Sun = new Color32(0xF4, 0xC4, 0x30, 0xFF);
        public static readonly Color Mint = new Color32(0x8F, 0xC9, 0xA8, 0xFF);
        public static readonly Color Lemon = new Color32(0xF7, 0xE2, 0x6B, 0xFF);
        public static readonly Color Ink = new Color32(0x3B, 0x2A, 0x20, 0xFF);

        [SerializeField] PadKind kind = PadKind.Pacco;
        [Tooltip("Vuoto = default del tipo (Ritira / Cassa / Servi). Max 12 caratteri.")]
        [SerializeField] string label = "";
        [Tooltip("> 0 = pad di acquisto: mostra il prezzo grande (es. 120 LN).")]
        [SerializeField] double price = 0;
        [SerializeField] ZoneFillRing ring; // se vuoto: cercato su ProximityZone.Ring o nei figli

        Label labelL, priceL; VisualElement icon;

        public PadKind Kind => kind;

        void Reset() { kind = GuessKind(); }

        void Awake()
        {
            if (!ring)
            {
                // UI non referenzia Gameplay (Gameplay -> UI): anello cercato nella gerarchia del pad
                ring = GetComponentInChildren<ZoneFillRing>();
            }
            ApplyRingColor();
        }

        PadKind GuessKind()
        {
            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                string tn = mb ? mb.GetType().Name : "";
                if (tn == "UpgradePad" || tn == "CassaZone") return PadKind.Moneta;
            }
            string n = name.ToLowerInvariant();
            if (n.Contains("banc") || n.Contains("client") || n.Contains("consegn") || n.Contains("deliver")) return PadKind.Cliente;
            if (n.Contains("cassa") || n.Contains("upgrade") || n.Contains("compra")) return PadKind.Moneta;
            return PadKind.Pacco;
        }

        public static string DefaultLabel(PadKind k, bool purchase) =>
            k == PadKind.Pacco ? "Ritira" : k == PadKind.Cliente ? "Servi" : purchase ? "Compra" : "Cassa";

        public static Color RingColor(PadKind k) => k == PadKind.Pacco ? Teal : k == PadKind.Moneta ? Sun : Mint;

        protected override void Bind(VisualElement e)
        {
            labelL = e.Q<Label>("tag-label"); priceL = e.Q<Label>("tag-price"); icon = e.Q("tag-icon");
            Refresh();
        }

        public void Set(PadKind k, string text = null, double priceLn = -1)
        {
            kind = k; if (text != null) label = text; if (priceLn >= 0) price = priceLn;
            Refresh(); ApplyRingColor();
        }

        public void SetPrice(double priceLn) { price = priceLn; Refresh(); }

        void ApplyRingColor() { if (ring) ring.SetColor(RingColor(kind)); }

        void Refresh()
        {
            if (Element == null) return;
            bool purchase = price > 0;
            string t = string.IsNullOrWhiteSpace(label) ? DefaultLabel(kind, purchase) : label.Trim();
            if (t.Length > MaxLabelChars) t = t.Substring(0, MaxLabelChars);
            var card = Element.Q(className: "pad-tag") ?? Element;
            card.EnableInClassList("pad-tag--pacco", kind == PadKind.Pacco);
            card.EnableInClassList("pad-tag--moneta", kind == PadKind.Moneta);
            card.EnableInClassList("pad-tag--cliente", kind == PadKind.Cliente);
            if (labelL != null) labelL.text = t;
            if (priceL != null)
            {
                priceL.style.display = purchase ? DisplayStyle.Flex : DisplayStyle.None;
                priceL.text = purchase ? UltimoMiglio.UI.HudFormat.Amount(price) + " LN" : "";
            }
        }

        void OnValidate()
        {
            if (label != null && label.Length > MaxLabelChars) label = label.Substring(0, MaxLabelChars);
            if (Application.isPlaying) { Refresh(); ApplyRingColor(); }
        }
    }
}
