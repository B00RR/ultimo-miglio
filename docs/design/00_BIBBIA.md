> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# BOKOBAY - Bibbia del progetto (game director: DevOn)
Mobile tycoon/idle logistico, ispirato a Shipping Store Simulator 2037. Portrait 9:16, controlli a un dito, sessioni brevi.
## Decisioni di Lorenzo
- AMBIENTAZIONE (vincolante): citta fittizia SOLARA, citta-terrazza su una baia, sole quasi perenne, tetti solari terracotta. Landmark: il FARO SPENTO sul promontorio (diventa rampa del razzo e si riaccende nel finale). Deposito a Borgo Gradoni (vicoli e scalinate). Mercato nero al Molo Basso (banchine semiallagate a lanterne). Nessun luogo, dialetto o riferimento reale.
- GAMEPLAY (vincolante): NON punta e clicca, NON idle a menu. Si controlla Kaia direttamente con joystick virtuale a un dito (stile My Little Universe / Shipping Store Simulator): cammina nel diorama 3D, raccoglie e trasporta pile di pacchi, li porta a scaffali/banco/furgone, interazioni automatiche per prossimita (stai sopra una zona = azione). Robot e droni automatizzano gradualmente. Le stanze sono spazi fisici in cui si cammina; il telefono e solo per menu secondari.
- Plot: BOKOBAY + arco secondario IL CARICO 0.
- Stile: SOLO "Diorama Solare": 3D low-poly, toon shading morbido, camera isometrica 3/4, palette terracotta/giallo sole/menta/limoni, GrayBox teal-grigio freddo. Notte nella stessa palette, luci calde e toni scuri (no neon). Riferimento: conceptA.jpg
- Piattaforma: lancio iniziale SOLO Android (Google Play). iOS eventualmente in seguito.
- ENGINE (vincolante): Unity 6 + URP, priorita alla qualita grafica. three.js ESCLUSO: demo/assaggio.html e solo riferimento di gameplay, non base di produzione. Blender gira sul box (3D Artist); Unity richiede un PC adeguato, da definire con Lorenzo.
- Navigazione: NO tab bar, NO swipe. Controllo diretto: camera segue Kaia, si cambia stanza attraversando le porte. Telefono di Kaia solo per menu secondari. Rif: 04_UI.md v3, mockups/hud_direct_*.png
## Plot (sintesi)
Solara 2037. Kaia Rio (27, ex corriera in bici) eredita il "Sunn Depot" di Oma Tilde: 1 anno per renderlo redditizio o GrayBox (CEO Maxim Vane) lo acquisisce. Sul bancone il Carico 0, pacco illeggibile.
Robot: OLDO (muletto, magazzino), FIZZ (drone-colibri, droni), TIKTAK (smistatore, automazione), HUGO (gru, carichi pesanti), NULO (si attiva col Carico 0).
Quartiere: Auntie Lulu (pizzaiola, quest giornaliere), Kenzo (rider, pilota corse), Dr. Nova Kade (razzo). MADAME MOTH: dealer mercato nero, ex socio della nonna.
Atto I Riapertura (cap 1-3, tutorial, droni) - Atto II Quartiere (4-8: automazione, guerra prezzi, mercato nero notturno, corse, carichi pesanti) - Atto III Verita' (9-12: furto, Carico 0 = nucleo rete logistica libera, sabotaggi, evento cooperativo, cantiere razzo) - Atto IV Lancio (13-15).
Finale: rete aperta, debito saldato, Maxim superfluo. Gancio: NULO trasmette coordinate oltre la Luna.
Agganci: capitoli gated da barra del debito e sblocchi; ciclo giorno/notte = consegne legittime / mercato nero.
## Squadra
Narrative Designer, Concept Artist, Game Designer, UI/UX Designer, Tech Lead, LiveOps & Monetizzazione. Documenti condivisi in questa cartella, numerati (01_..., 02_...).
