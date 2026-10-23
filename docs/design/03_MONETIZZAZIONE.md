> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# 03 - MONETIZZAZIONE & LIVEOPS v2 (LiveOps & Monetizzazione)
Riferimenti: 00_BIBBIA, 01_CORE_LOOP v3.3, qa/07_REVIEW. Lancio **solo Android / Google Play**. Gameplay a **controllo diretto con joystick** (MLU / Shipping Store Simulator), niente idle a menu. Ambientazione: **Solara** (città fittizia). il Sunn Depot è a Borgo Gradoni, il mercato nero al Molo Basso, il landmark è il Faro Spento. Valute: LN (soft, monete a terra), Bulloni (hard: **solo timer e cosmetici, mai progressione**), Gettoni di Madame Moth (notte), Reputazione.

## 1. Competitor hybrid-casual a controllo diretto
| Gioco | Modello | Lezione |
|---|---|---|
| **My Little Universe** | Rewarded forti, no-ads, pass, skin | Il riferimento per il nostro loop: le rewarded aiutano il movimento (velocità, capacità) e non lo saltano. Il pass è semplice. |
| **Shipping Store Simulator / Supermarket Simulator mobile** | Ads, no-ads, espansioni | Il fantasy del negozio regge bene. Gli interstitial frequenti in mezzo all'azione portano recensioni negative su Play. |
| **Eatventure / giochi "carry & stack" alla Supersonic/Voodoo** | Rewarded per boost a tempo, pacchi starter, pass | La progressione a stanze o locali corrisponde ai nostri capitoli. I boost temporanei funzionano meglio delle scorciatoie permanenti. |

Lezioni: rewarded sempre volontarie e posizionate *nel mondo* (zone a terra); interstitial mai durante l'input; no-ads e starter pack a basso prezzo; nessuna vendita di progressione.

## 2. Modello di business (Android-first)
Su Android l'ARPPU è più basso e ci sono meno paganti: stimiamo **55-65% dei ricavi da ads e 35-45% da IAP e pass**.

**Rewarded (volontarie, fisiche e coerenti col loop):**
- **x2 alla Cassa (01 §9):**
  - l'offline riempie la Cassa al **50% della resa reale dei robot**, fino a una capienza del **4% della tranche** (6% con il pad "Turno di notte");
  - **tetto rigido: l'offline non supera il 20% delle LN del capitolo** (12% in Assist), anche con il x2;
  - quando Kaia ci passa sopra compare "x2 con video" (o con i Bulloni), **al massimo 2 volte al giorno**; niente modale, solo un toast;
  - peso stimato dell'offline sulle LN: ≤20% per capitolo (≤12% in Assist), concentrato nei cap 1-3 e nei rientri ravvicinati.
- **Totem "Caffè Turbo"** (dal cap 4): una zona a terra che dà Scarpe +30% per 2 min.
- **Totem "Zaino Gonfio"** (dal cap 4): una zona a terra che dà +50% di pila per 2 min.
- **Tetto dei totem (01 v3.3):** al massimo 1 totem per sessione e 4 al giorno, sommando i due tipi.
- **Drone:** -30 min sulla missione, al massimo 3 volte al giorno.
- **Corse:** un tentativo extra alle corse di Kenzo, che non conta per la classifica.
- **Streak:** raddoppio della ricompensa di login.
- **Notte:** si riduce il Calore di un livello, una volta per notte.

Tetto morbido: circa 8-10 rewarded al giorno.

**Interstitial:**
- **Mai con il joystick attivo.** Trigger ammessi solo in pausa vera:
  - fine di una corsa di Kenzo (schermata risultato);
  - chiusura del telefono di Kaia (menu);
  - schermata di un nuovo capitolo;
  - ritorno in app dopo più di 10 min.
- **Non si usa** la "fine giornata di gioco" da 4 min, perché cade in mezzo al gioco attivo.
- **Frequency cap:** al massimo 1 ogni 5 min, 6 al giorno, nessuno nei primi 2 capitoli e nessuno entro 60 s da una rewarded.
- Il no-ads li rimuove.

**Banner:** nessuno.

**Mediation:**
- AppLovin MAX come primaria, con AdMob, Unity, Meta, Mintegral e Liftoff in bidding. LevelPlay resta l'alternativa.
- Consenso UMP/GDPR e Data safety su Play.
- Test della rewarded con rete lenta: se il video non si carica, il totem viene nascosto e non resta con errore.

**IAP (tier Play, EUR, localizzati per regione):**
- **Starter "Riapertura"** a 1,99 €: Bulloni e skin del furgone/zaino. È solo cosmetico più valuta per i timer.
- **No-ads "Bottega Tranquilla"** a 4,99 €: niente interstitial e un piccolo bonus giornaliero di Bulloni. Le rewarded restano.
- **Bulloni** a 0,99 / 4,99 / 9,99 / 19,99 €.
- **Cosmetici:** outfit di Kaia, skin dei robot, livree dei droni, temi della bottega.
- **"Turno di notte" non è in vendita:** è un pad a terra del cap 3 da **2.500 LN** che porta la capienza della Cassa al 6% della tranche (+50%), con il tetto del 20% invariato. Si ottiene solo giocando: nessun IAP, nessun prezzo in Bulloni.

**Pass "Libretto del Corriere"** (28 giorni):
- Gratuito oppure premium a 4,99 €, 30 livelli che salgono con le consegne.
- Il premium dà cosmetici, Bulloni e Gettoni.

**Regole anti pay-to-win (vincolanti):**
- I Bulloni accelerano solo i timer (droni, spedizioni, blocco da ispezione) e comprano solo cosmetici. Non comprano zone a terra, livelli dei robot, stanze, tranche del debito o sblocchi di capitolo.
- I timer accelerabili non devono mai bloccare la trama.
- Ai Bulloni serve una conversione fissa minuti/Bulloni (QA §1.2-f), indicativamente 1 Bullone ogni 3 min.
- La classifica delle corse usa solo i 3 tentativi base.

## 3. LiveOps: stagioni "Segnali di NULO"
Il gancio è NULO (cap 10, Carico 0), che trasmette coordinate oltre la Luna. Ogni stagione è un frammento del segnale che apre una nuova **zona fisica temporanea** del diorama. Chi è prima del cap 10 riceve il segnale come "interferenza misteriosa", senza spoiler.

**Stagioni (28 giorni):**
1. **S1 "Interferenza"**: al lancio, a tema estate e limoni.
2. **S2 "Rotta del Faro"**: il segnale punta al Faro Spento.
3. **S3 "Madame Moth Parla"**: stagione notturna stealth.
4. **S4 "Oltre la Luna"**: le coordinate complete e il cantiere della Dr. Nova Kade. Poi il ciclo riparte con nuove destinazioni (dettaglio in 08 §9).

**Weekend del Segnale (72 h):** un **mini-diorama giocabile a joystick**, cioè un magazzino-evento con economia propria ("Crediti Segnale") e zone a terra dedicate, che si azzera a fine evento. Classifica a gruppi di 50, premi cosmetici e Gettoni.

**Settimanale:** quest di Auntie Lulu, streak di 7 giorni, classifica corse a leghe (Bronzo / Argento / Oro).

**Mensile:** evento cooperativo (dal cap 12) per costruire l'antenna per NULO, con obiettivo globale.

## 4. KPI target (Android, stime da benchmark hybrid-casual)
I valori sono indicativi, senza una fonte specifica: vanno ricalibrati in soft launch.

| KPI | Soglia soft launch | Obiettivo |
|---|---|---|
| Retention D1 | 35% | 40-45% |
| Retention D7 | 10% | 13-18% |
| Retention D30 | 3% | 5-7% |
| ARPDAU (blended) | 0,05 € | 0,08-0,14 € |
| Conversione payer | 1% | 1,5-2,5% |
| Rewarded/DAU | 3 | 4-6 |
| Interstitial/DAU (non paganti) | - | 2-4 (entro il cap) |
| Sessione | 4 min | 5-8 min, 3-5 sessioni al giorno |

**Soft launch:**
- Paesi: Filippine e Colombia per la retention, poi Italia e Spagna per la monetizzazione.
- A/B test su cap degli interstitial, prezzo dello starter, durata dei totem e posizione/testo dell'offerta x2 sulla Cassa.
- Si passa al global con D1 ≥ 40%, D7 ≥ 13% e LTV D30 superiore al CPI.
- Prerequisito: l'Atto I (cap 1-3) si chiude in 4-6 giorni (QA §2.4), quindi il cap 4 deve essere pronto al lancio.
