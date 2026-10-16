// Ultimo Miglio - #240: valori dello stato "spento / esci e rientra" del pad dopo l'acquisto, in UN solo punto.
// Fonte: UI/UX (prezzo #6E655B per contrasto 4,5:1; anello e freccia come da richiesta VFX). Cambiare solo qui.
using UnityEngine;

namespace UltimoMiglio.VFX
{
    public static class UM_PadOffStyle
    {
        /// Crema #F3E3C3 verso cui si desatura l'anello.
        public static readonly Color Crema = new Color(0xF3 / 255f, 0xE3 / 255f, 0xC3 / 255f, 1f);
        /// Quota di desaturazione dell'anello verso il crema (0..1).
        public const float RingDesat = 0.70f;
        /// Alpha dell'anello da spento.
        public const float RingAlpha = 0.5f;
        /// Glow / emissione dell'anello da spento (niente punta luminosa, niente flash, niente pulsazione).
        public const float RingGlowOff = 0f;
        /// Freccia (chevron) da spento, rivolta verso l'esterno: #9E8F7E.
        public static readonly Color Arrow = new Color(0x9E / 255f, 0x8F / 255f, 0x7E / 255f, 1f);
        /// Prezzo da spento: #6E655B (UI/UX, contrasto 4,5:1).
        public static readonly Color Price = new Color(0x6E / 255f, 0x65 / 255f, 0x5B / 255f, 1f);
        /// Pop di ritorno attivo: scala 1 → PopPeak → 1 in PopTime secondi (ease-out-back).
        public const float PopTime = 0.2f, PopPeak = 1.15f;
        /// Flash del colore attivo pieno nei primi FlashTime secondi del pop.
        public const float FlashTime = 0.08f;
        /// Frazione del pop dedicata alla salita (il resto torna a 1).
        public const float PopRise = 0.4f;

        /// Colore dell'anello da spento a partire dal colore attivo.
        public static Color RingOff(Color active) { var c = Color.Lerp(active, Crema, RingDesat); c.a = RingAlpha; return c; }

        /// Scala del pop al tempo t (s) dall'uscita: 1 a t=0, PopPeak al culmine, 1 a t>=PopTime.
        /// Salita ease-out-back normalizzata (il sorpasso tocca esattamente PopPeak), discesa smooth.
        public static float PopScale(float t)
        {
            if (t <= 0f || t >= PopTime) return 1f;
            float u = t / PopTime;
            float k;
            if (u < PopRise)
            {
                float x = u / PopRise;
                k = EaseOutBack(x) / EaseOutBackMax; // 0..1, con picco a fine salita grazie all'overshoot normalizzato
                k = Mathf.Min(k, 1f);
            }
            else
            {
                float x = (u - PopRise) / (1f - PopRise);
                k = 1f - x * x * (3f - 2f * x);
            }
            return 1f + (PopPeak - 1f) * k;
        }

        const float Back = 1.70158f;
        static float EaseOutBack(float x) { float c3 = Back + 1f, y = x - 1f; return 1f + c3 * y * y * y + Back * y * y; }
        // massimo di EaseOutBack su [0,1] (≈1,0996 a x≈0,7): normalizza così il picco del pop è PopPeak
        static readonly float EaseOutBackMax = ComputeMax();
        static float ComputeMax() { float m = 1f; for (int i = 0; i <= 200; i++) m = Mathf.Max(m, EaseOutBack(i / 200f)); return m; }
    }
}
