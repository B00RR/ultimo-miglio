namespace UltimoMiglio.Core
{
    /// <summary>#129: nomi Bokobay mostrati al giocatore. Classi, file, GUID e chiavi di salvataggio restano coi nomi storici
    /// (NonnoRobot, NinaController, um_save_v1...): qui si traducono solo i testi visibili, anche quelli già serializzati nella scena.</summary>
    public static class DisplayNames
    {
        public const string Gioco = "Bokobay", Deposito = "Sunn Depot", Protagonista = "Kaia Rio", Nonna = "Oma Tilde", Muletto = "OLDO";

        // ordine: prima le forme lunghe
        static readonly (string vecchio, string nuovo)[] Map =
        {
            ("Ultimo Miglio", Gioco), ("ULTIMO MIGLIO", "BOKOBAY"),
            ("Vela & Figli", Deposito), ("Vela &amp; Figli", Deposito), ("VELA & FIGLI", "SUNN DEPOT"),
            ("Nina Vela", Protagonista), ("Nonna Ada", Nonna), ("NONNA ADA", "OMA TILDE"),
            ("NONNO", Muletto), ("Mamma Rosa", "Auntie Lulu"),
        };

        /// <summary>Sostituisce i nomi storici nel testo visibile (null resta null).</summary>
        public static string Apply(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            foreach (var (v, n) in Map) if (s.Contains(v)) s = s.Replace(v, n);
            return s;
        }
    }
}
