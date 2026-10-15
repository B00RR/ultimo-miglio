// Ultimo Miglio - palette "Diorama Solare" (source: 3d/05_3D_PIPELINE.md + conceptA.jpg)
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public static class UM_Palette
    {
        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        // Base (05_3D_PIPELINE)
        public static readonly Color Terracotta = H("#C8603A");
        public static readonly Color GialloSole = H("#F2B53A");
        public static readonly Color Menta      = H("#8FC9A8");
        public static readonly Color Limone     = H("#F4E04D");
        public static readonly Color TealOmni   = H("#4E7F86");
        public static readonly Color Tufo       = H("#D9C4A0");
        public static readonly Color Pavimento  = H("#E6D2B0");
        public static readonly Color Legno      = H("#9A6A42");
        public static readonly Color Cartone    = H("#C99A62");
        public static readonly Color ScuroCaldo = H("#3B2F2A");
        public static readonly Color AranceNina = H("#E5743A");
        public static readonly Color Crema      = H("#FFF4D6");
        public static readonly Color Cielo      = H("#F3DDB6");
        // VFX/lighting derivati (proposti dal VFX, da validare col Concept Artist)
        public static readonly Color Sole       = H("#FFE6B8"); // directional giorno
        public static readonly Color AmbSky     = H("#F3DDB6");
        public static readonly Color AmbEquator = H("#E2C49C");
        public static readonly Color AmbGround  = H("#8C6A50");
        public static readonly Color Polvere    = H("#B8906A"); // v1.4: darker, reads on cream floor
        public static readonly Color MonetaOro  = H("#F2B53A"); // lira/gettone di Solara (faro inciso)
        public static readonly Color MonetaBordo= H("#3B2A20"); // bordo marrone moneta
        public static readonly Color UpgradeSole  = H("#F4C430");
        public static readonly Color UpgradeLimone= H("#F7E26B"); // highlight/scintille
        public static readonly Color MonetaLuce = H("#FFF4D6");
        // Tramonto (cap. 1): sole basso arancio-dorato, ambiente pesca, cielo albicocca
        public static readonly Color SoleTramonto = H("#FFC27A");
        public static readonly Color TramSky   = H("#F2C29A");
        public static readonly Color TramEq    = H("#D9A27E");
        public static readonly Color TramCielo = H("#F0C8A0");
        // Notte: stessa palette, toni scuri caldi, niente neon
        public static readonly Color LunaCalda  = H("#C9A98A");
        public static readonly Color NottSky    = H("#8C7880");
        public static readonly Color NottEq     = H("#7C665C");
        public static readonly Color NottGround = H("#4A3C35");
        public static readonly Color Lanterna   = H("#FFB65C");
    }
}
