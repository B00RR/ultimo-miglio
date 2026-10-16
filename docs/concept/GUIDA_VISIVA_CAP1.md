# Guida visiva — Capitolo 1 «Ultimo Miglio»

Stile **Diorama Solare**: 3D low-poly toon, camera isometrica 3/4 dall'alto, formato portrait (riferimento 1170×2532).
Autore: Concept Artist · Stato: v1.1 (valori allineati alla build, vedi §12) · Riferimento approvato principale: `conceptA.jpg`.

![Concept A](img/ref_conceptA.jpg)

## 1. Ambientazione
- Città fittizia **SOLARA** (mediterranea, inventata): **niente Napoli, niente Vesuvio**, nessun landmark reale.
- **Deposito «Vela & Figli»** a **Borgo Gradoni**: base del capitolo 1.
- Landmark: **Faro Spento** sul promontorio (visibile come silhouette di sfondo), **Molo Basso** sul mare.
- Tono: **commedia calda con fondo distopico**. Solara è calda e artigianale; **OmniFreight** è l'unico elemento freddo (teal-grigio), corporativo, liscio.

![Key art](img/ref_solara_keyart_16x9.jpg)

## 2. Palette (valori esatti)
| Ruolo | Hex |
|---|---|
| Terracotta | `#C8633B` |
| Giallo sole | `#F4C430` |
| Menta | `#8FC9A8` |
| Limone | `#F7E26B` |
| Crema / tufo | `#F3E3C3` (variazioni `#E8D3AC`, `#E9D2A8`) |
| Bruno | `#3B2A20` |
| Noce | `#7A4A2E` (legno `#74503A`) |
| Arancia | `#E8913A` |
| OmniFreight | `#5E7F86` / `#3E5A61` |
| Outline | `#3B2F2A` |
| Ombra | `#8F7A76` (v1.1; era `#9E7873`) |
| Valore minimo volumi | `#5A4A55` |
| Tramonto/notte fondo | `#2B2230` |
| Lanterne | `#FFB65C` / `#FFB347` |
| Luna | `#C9A98A` |
| Ambient notte | `#5A4A55` |

**Vietati**: grigi freddi, blu, viola (unica eccezione: OmniFreight), neon. Il cielo, se inquadrato, va in crema/pesca caldo o tramonto, non azzurro.

## 3. Deposito / Bancone
- Materiali: **tufo** crema, **archi**, **legno** noce; insegna **«Vela & Figli» con il faro** nel logo.
- **Carico 0** sul banco con **cuciture emissive ambra** e **bloom isolato solo sul banco**.
- **Lastre pavimento grandi (3,5 m)** (v1.1; era ~0,25 m), 3 toni crema/tufo per lastra (`#F3E3C3` / `#E8D3AC` / `#E9D2A8`), **una fuga su due** (fuga ogni 7 m) in `#E8D3AC` con opacità ~30%. Non un unico arancio saturo.
- Set dressing: **vasi in terracotta e limoni**, casse, scaffali in legno.

![Beauty bancone v03 (APPROVATO)](img/ref_3d_beauty_bancone_v03_1170x2532.jpg)

## 4. Strada ed edifici di Solara
- **Case a gradoni su 3 livelli** con **scalinate**; base in tufo crema.
- Intonaci variati: terracotta chiara, giallo sole tenue, menta tenue, crema.
- **Tetti in coppi** e terrazze, balconi, **panni stesi grandi**, vasi di limoni, **piccoli tetti solari** (pannelli piccoli, caldi, non dominanti).

![Case di Solara v02 (APPROVATO)](img/ref_solara_houses_v02.jpg)

## 5. Nina Vela (protagonista)
27 anni, ex corriera in bici, ironica.
- **Casco tutto arancio**; **giacca arancio `#C8633B` aperta** su **t-shirt `#CFC2AE`** con **sciarpa grigio scuro**; **cargo oliva**; **scarponi**; **zaino**; **coda di cavallo corta**.
- Proporzioni: **testa grande / corpo tozzo** (leggibile dall'alto).
- **Viso sempre ben illuminato** (fill caldo dedicato).

![Sheet Nina](img/ref_sheet_nina_v01.jpg)

## 6. Robot
**NONNO** — muletto anni '20 **rosso ruggine**, **faccia grande sopra le forche** con **sopracciglia folte e baffi**, **gomme piene con 6 raggi gialli**, **faro tondo**, ruggine, **clacson a tromba sulla tettoia**.

![Turnaround NONNO v02 (APPROVATO)](img/ref_turnaround_bot_nonno_v02.jpg)

**TICO** — **alto e rigido**, corpo **menta** con **cappello terracotta**, **occhiali tondi in ottone**, **timbro**.

![Turnaround TICO v02 (APPROVATO)](img/ref_turnaround_bot_tico_v02.jpg)

![Sheet robot](img/ref_sheet_robot_v01.jpg)

> Nota (capitoli successivi, non nel cap. 1): **PIPPA** drone-colibrì meccanico; **BRACCIO** gru gialla timida; **ECO** guscio bianco con occhio teal.

## 7. Clienti
5 tipologie, proporzioni come Nina, **copricapo e busto leggibili dall'alto**:
1. **Residente** — terracotta + coppola.
2. **Rider** — menta + zaino cubo.
3. **Anziana** — tutta bruna con fazzoletto limone.
4. **Turista** — giallo sole + cappello.
5. **VIP** — cappello largo terracotta con nastro giallo.

![Sheet clienti](img/ref_sheet_clienti_v01.jpg)
![Lineup clienti v02](img/ref_clienti_lineup_v02.jpg)

## 8. Nonna Ada — ⚠️ tavola mancante
Nonna Ada guida il tutorial del capitolo 1 (ritratto nel dialogo HUD), ma **non esiste ancora una tavola di riferimento** (né sheet né turnaround) tra i materiali approvati. In build compare solo un cerchio con la scritta «ADA». **Da produrre**: sheet volto/busto per il ritratto HUD (stessa palette, toni bruni/limone coerenti con l'anziana, ma riconoscibile). Fino ad allora il ritratto resta segnaposto.

## 9. Illuminazione «Diorama Solare»
- **Sole da alto-sinistra (50°, −35°)**, colore `#FFF0D4` (v1.1; era `#FFE6B8`).
- **Ombre tinte calde `#8F7A76`** (v1.1; era `#9E7873`): ombra calda con una lieve nota fresca, mai blu o viola.
- ⚠️ **DECISIONE DA CONFERMARE DA DevOn**: il VFX propone ombre fredde colorate e palette più satura. Proposta della guida: *ombre leggermente fresche ammesse solo come tinta lieve nelle zone d'ombra, mai blu/viola; saturazione +10-15% ok.*
- Toon a **3 bande morbide**; **outline solo su personaggi, robot e oggetti interattivi** (non sul set).
- **Nebbia calda leggera 30-90 m**; **AO di contatto**.
- **Notte** ≈ **47% dell'esposizione del giorno**, **lanterne (intensità 1.7) come fonte primaria**, fondo `#2B2230`, luna `#C9A98A`.

### FX
- **Moneta-gettone di Solara** con faro inciso: oro `#F2B53A`, bordo bruno.
- **Upgrade**: giallo sole con **anello a terra a pulse lento**.
- **Scintille core**: limone. **Polvere**: `#B8906A`.

## 10. HUD
- Pannelli **legno/cartone** con **bordo bruno `#3B2A20` 3-4 px**, angoli arrotondati.
- Font titoli **rounded tipo insegna**.
- **CTA terracotta in zona pollice** (basso).
- **Telefono di Nina in basso a destra**.
- Fasce libere: **~350 px in alto e ~500 px in basso** su 1170×2532 (il gioco vive nel centro, l'HUD non invade la scena); **niente tab bar**.

![Mockup gameplay iso](img/ref_bg_gameplay_iso.jpg)

## 11. Camera
Isometrica **3/4 dall'alto**, **portrait**; Nina deve restare leggibile (silhouette, casco arancio) al centro dell'inquadratura.

## Aggiornamento: Nonna Ada (tavola v01, chiude il buco di §8)
![Nonna Ada](img/sheet_nonna_ada_v01.jpg)
Nonna Ada, fondatrice di Vela & Figli, circa 75 anni, piccola e robusta. Ha lo chignon bianco fermato da una matita, occhiali tondi in ottone con catenella, cardigan terracotta #C8633B su camicetta menta #8FC9A8, grembiule noce #7A4A2E con attrezzi e il libro mastro, e una spilla a forma di faro. Ritratto di dialogo circolare: usare il busto in alto a destra. La variante "foto d'epoca" serve per ricordi e flashback. Le scritte nella tavola sono in inglese e vanno ignorate: fa fede questo testo.

## Decisione confermata da DevOn (16/10)
Ombre leggermente fresche ammesse come tinta lieve nelle zone d'ombra, mai blu o viola. Saturazione +10-15% approvata.

## 12. Registro modifiche
- **v1.1 (16/10/2026)**: la guida riprende i valori presenti in build su `main`, introdotti dal Concept Artist con i commit `fix(concept)` `ef0b5b2` (pavimento a lastre grandi) e `f464333` (look crema/noce) dopo la QA v7. Chiude #81.
  - Sole `#FFE6B8` → `#FFF0D4` (`UM_Palette.Sole`).
  - Ombra `#9E7873` → `#8F7A76` (`UM_Palette.Ombra`, `_ShadowTint` di `M_Diorama_Atlas` = 0.561/0.478/0.463).
  - Lastre pavimento ~0,25 m → 3,5 m (`_TileSize` 3.5), una fuga su due in `#E8D3AC` al ~30% (shader `UM_ToonDiorama`).
- **v1.2 (16/10/2026)**: decisione DevOn, **vince la guida**. La build torna ai valori v1: sole `#FFE6B8`, ombra `#9E7873`, legno `#74503A`, notte al 47% dell'esposizione del giorno, swatch dell'atlas `T_Palette_Atlas.png` allineati alla palette §2. Il Concept Artist conferma le lastre del pavimento da 3,5 m con una fuga ogni due (`#E8D3AC` ~30%). I valori v1.1 di sole (`#FFF0D4`) e ombra (`#8F7A76`) sono superati. Rif. #81, PR #126.
