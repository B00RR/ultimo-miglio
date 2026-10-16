# Confronto build web ↔ Guida visiva — Capitolo 1

Build: https://b00rr.github.io/ultimo-miglio-web/ (commit `25ab5cf`), catturata il 16/10/2026 ~01:50 (ora di Roma) con Chrome headless (WebGL 2 via SwiftShader), attesa 30 s.
WebGL **rende correttamente in headless**: le immagini sotto sono catture reali della build.

| Portrait 390×844 (avvio) | Portrait dopo 2 tap (stato invariato) |
|---|---|
| ![](img/build_portrait_01.png) | ![](img/build_portrait_03.png) |

Desktop 1440×900:
![](img/build_desktop_01.png)

## Scostamenti
| # | Area | Cosa si vede | Cosa dovrebbe essere (guida) | Gravità | Responsabile | Issue |
|---|---|---|---|---|---|---|
| D1 | Deposito – pavimento | Lastre grandi, arancio saturo quasi uniforme | Lastre ~0,25 m, crema/tufo con lieve variazione (§3) | Media | 3D Artist | #47 |
| D2 | Deposito – muri/insegna | Muretti bassi in mattoni; insegna «Vela & Figli» non visibile; pochi vasi/limoni | Tufo, archi, legno, insegna col faro, vasi terracotta e limoni (§3) | Alta | 3D Artist | #47 |
| D3 | Bancone | Cassone arancio semplice | Banco in legno con Carico 0 e cuciture ambra emissive (§3) | Alta | 3D Artist | #47 |
| L1 | Palette – cielo | Cielo azzurro visibile in alto | Niente blu: fondo caldo o fuori inquadratura (§2) | Media | VFX & Technical Artist | #48 |
| L2 | Luce/valori | Scena monocromatica arancio, ombre/AO/bande toon poco leggibili | Sole #FFE6B8 alto-sx, ombre #9E7873, AO contatto, 3 bande (§9) | Alta | VFX & Technical Artist | #48 |
| L3 | Camera | Nina ~30 px di altezza in portrait | Iso 3/4 abbastanza vicina da leggere Nina (§11) | Alta | VFX & Technical Artist | #48 |
| H1 | HUD – telefono | Telefono (TAB) in alto a destra | In basso a destra, zona pollice (§10) | Media | UI/UX Designer | #49 |
| H2 | HUD – stile | Pannelli crema piatti, nessuna CTA terracotta | Legno/cartone, bordo #3B2A20 3-4 px, CTA terracotta in basso (§10) | Media | UI/UX Designer | #49 |
| H3 | HUD – dialogo Ada | Dialogo esce dalla fascia alta ~350 px e copre la scena | Fasce libere ~350 alto / ~500 basso (§10) | Alta | UI/UX Designer | #41 (esistente, commentata) |
| H4 | HUD – label mondo | «Prendi/Scaffale» impilate e sovrapposte | HUD non invade la scena (§10) | Alta | UI/UX Designer | #43 (esistente, commentata) |
| A1 | Nonna Ada | Ritratto segnaposto «ADA» | Manca tavola di riferimento (§8) | Media | Concept Artist | #4 (commentata) |

## Coerente con la guida (visto)
- Case di sfondo: intonaci caldi, tetti in coppi terracotta, balconi con vasi di limoni, panni stesi, persiane menta.
- Nina: casco/giacca arancio riconoscibili, ma troppo piccola per verificare i dettagli.
- Prezzi a terra su pannelli giallo sole con bordo bruno.

## NON verificato
- **Animazioni** (Animator): la build non ha risposto ai tap/click in headless (nessun movimento di Nina) → camminata, NONNO al lavoro, TICO timbro non osservati.
- **NONNO, TICO, clienti**: non presenti/leggibili nelle catture (oggetto verde cubico vicino al muro non identificabile con certezza). Il problema clienti-capsule è già in #2; NONNO non utilizzabile in #42.
- Notte/lanterne, FX (moneta, upgrade, scintille, polvere), bloom del Carico 0, nebbia 30-90 m, outline selettivo, scalinate a gradoni su 3 livelli (fuori inquadratura).
- Console build: 3 errori «Shader» e «no valid NavMesh» (vedi #42) — segnalati per il Tech, non valutati visivamente.

## Confronto 16/10 mattina
Build live locale `http://127.0.0.1:8778/`, 390×844, Chrome headless (SwiftShader), catturata il 16/10/2026 ~08:58 (ora di Roma). La build **reagisce all'input** (WASD/Tab): Nina si muove.

| Avvio | Scaffali/insegna | Arrivi/archi | Pad telefono |
|---|---|---|---|
| ![](img/cfr3/cfr3_01_start.png) | ![](img/cfr3/cfr3_04_scaffali.png) | ![](img/cfr3/cfr3_06_insegna_archi.png) | ![](img/cfr3/cfr3_07_pad.png) |

Altre: `cfr3_02_movimento`, `cfr3_03_arrivi`, `cfr3_05_bancone`.

### Risolti rispetto alla PR #55
- Fughe pavimento: ora tenui e larghe, lastre crema/tufo ✅
- Camera: iso 3/4 portrait, Nina ~120 px su 844 ✅ (in linea con ~100 px)
- Archi: almeno un arco in tufo visibile sul muro N ✅ (parziale)
- Insegna: la scritta «Vela & Figli» ora c'è ✅

### Scostamenti ancora presenti
| # | Area | Cosa si vede | Guida | Owner | Issue |
|---|---|---|---|---|---|
| C1 | Zone/pad a terra | Dischi blu-grigio scuro, pali grigio freddo, riquadri gialli tipo griglia di debug sotto i pad | Niente grigi/blu freddi; zone in menta/limone/terracotta, nessuna griglia | VFX & Technical Artist | vedi issue [Arte] zone |
| C2 | Insegna | Pannello bianco piatto, nessun faro | Insegna in legno/terracotta con faro (§3) | 3D Artist | vedi issue [Arte] insegna |
| C3 | Pad telefono | Ora «08:00» bianca su crema illeggibile, due barre vuote | Testo bruno #3B2A20, niente campi vuoti (§10) | UI/UX Designer | vedi issue [Arte] pad |

### Non verificato
Notte/lanterne, FX, animazioni di NONNO/TICO (non inquadrati).
