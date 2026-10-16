// Ultimo Miglio - palette "Diorama Solare". v2.1 (#48): valori allineati ESATTAMENTE a docs/concept/GUIDA_VISIVA_CAP1.md §2/§9.
// Vietati grigi freddi/blu/viola (eccezione OmniFreight). Ambient "fresco" = solo tinta piu neutra del sole, sempre calda.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public static class UM_Palette
    {
        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        // Base (05_3D_PIPELINE)
        public static readonly Color Terracotta = H("#C8633B");
        public static readonly Color GialloSole = H("#F4C430");
        public static readonly Color Menta      = H("#8FC9A8");
        public static readonly Color Limone     = H("#F7E26B");
        public static readonly Color TealOmni   = H("#5E7F86");
        public static readonly Color Tufo       = H("#E8D3AC");
        public static readonly Color Pavimento  = H("#F3E3C3");
        public static readonly Color Legno      = H("#74503A");
        public static readonly Color Cartone    = H("#C99A62");
        public static readonly Color ScuroCaldo = H("#3B2F2A");
        public static readonly Color AranceNina = H("#E8913A");
        public static readonly Color Crema      = H("#F3E3C3");
        public static readonly Color Cielo      = H("#F3DDB6");
        // VFX/lighting derivati (proposti dal VFX, da validare col Concept Artist)
        public static readonly Color Sole       = H("#FFE6B8");
        public static readonly Color AmbSky     = H("#C9B4A6");
        public static readonly Color AmbEquator = H("#D8B898");
        public static readonly Color AmbGround  = H("#9E7873");
        public static readonly Color Polvere    = H("#B8906A"); // v1.4: darker, reads on cream floor
        public static readonly Color MonetaOro  = H("#F2B53A"); // lira/gettone di Solara (faro inciso)
        public static readonly Color MonetaBordo= H("#3B2A20"); // bordo marrone moneta
        public static readonly Color UpgradeSole  = H("#F4C430");
        public static readonly Color UpgradeLimone= H("#F7E26B"); // highlight/scintille
        // Colori UI/UX (esatti, condivisi con HUD e anelli zona): pacco/raccolta, moneta/acquisto, cliente/consegna, upgrade
        public static readonly Color UiPacco    = H("#5E7F86"); // teal: pacco / Pickup
        public static readonly Color UiSole     = H("#F4C430"); // sole: moneta / Purchase / riempimento in pagamento
        public static readonly Color UiCliente  = H("#8FC9A8"); // verde menta: consegna / Delivery (scaffali, Bancone, clienti) - correzione Lorenzo 17:28
        public static readonly Color UiMenta    = H("#8FC9A8"); // = UiCliente
        public static readonly Color UiUpgrade  = H("#F7E26B"); // limone: upgrade (distinto da consegna e acquisto)
        public static readonly Color MonetaLuce = H("#FFF4D6");
        // Tramonto (cap. 1): sole basso arancio-dorato, ambiente pesca, cielo albicocca
        public static readonly Color SoleTramonto = H("#FF9F5A");
        public static readonly Color TramSky   = H("#D49A86");
        public static readonly Color TramEq    = H("#E6A07E");
        public static readonly Color TramCielo = H("#F4B88E");
        public static readonly Color Foschia   = H("#F6D7B0"); // nebbia/orizzonte giorno
        // Notte: stessa palette, toni scuri caldi, niente neon
        public static readonly Color LunaCalda  = H("#C9A98A");
        public static readonly Color NottSky    = H("#8C7880");
        public static readonly Color NottEq     = H("#7C665C");
        public static readonly Color NottGround = H("#4A3C35");
        public static readonly Color Lanterna   = H("#FFB65C");
        // Guida §2 - nomi esatti (v2.1, #48)
        public static readonly Color Bruno       = H("#3B2A20");
        public static readonly Color Noce        = H("#7A4A2E");
        public static readonly Color Arancia     = H("#E8913A");
        public static readonly Color CremaVarA   = H("#E8D3AC");
        public static readonly Color CremaVarB   = H("#E9D2A8");
        public static readonly Color OmniScuro   = H("#3E5A61");
        public static readonly Color Outline     = H("#3B2F2A");
        public static readonly Color Ombra       = H("#9E7873"); // ombre tinte calde
        public static readonly Color ValoreMin   = H("#5A4A55"); // valore minimo volumi = ambient notte
        public static readonly Color FondoNotte  = H("#2B2230");
        public static readonly Color Lanterna2   = H("#FFB347");
        public static readonly Color GiaccaNina  = H("#C8633B");
        public static readonly Color TshirtNina  = H("#CFC2AE");
        // Cielo/fondo caldo (mai azzurro): crema in alto -> pesca all'orizzonte
        public static readonly Color CieloTop    = H("#F3E3C3");
        public static readonly Color CieloOriz   = H("#F6D7B0");
        public static readonly Color CieloBasso  = H("#E9D2A8");
        public static readonly Color TramTop     = H("#E8B48A");
    }
}
