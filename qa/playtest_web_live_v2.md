# Playtest LIVE v2 — Ultimo Miglio (PR #45 / fix-qa `4767a38`)

**Data:** 2026-10-15 ~19:00–19:17 CEST  
**Build:** server locale `http://127.0.0.1:8777/` (directory `/workspace/um-recovery/build/WebGL`) — **non** GitHub Pages  
**Viewport:** 390×844 (touch/phone) e 1080×2400 — clear storage (localStorage + IndexedDB) tra sessioni  
**Metodo:** skill webapp-testing + MCP `user-Playwright` + script `qa/playtest_web_live_v2.py` (senior-qa)  
**Vincoli:** no computerUse, no Unity editor, no spesa  

---

## Verdetto complessivo: **NO-VIA**

Due priorità su quattro falliscono in playtest live (NONNO loop + obiettivo in alto). HUD e zone a terra migliorate in modo chiaro; console senza NavMesh su locale.

---

## Tabella 4 priorità Tech Lead

| # | Priorità | Verdetto | Note brevi |
|---|----------|----------|------------|
| 1 | HUD: Ada ≤36 dp / 1 riga; chip/card off; non invasivo | **VIA** | Chip/card assenti; HUD top minimo; niente biglietto Ada invasivo (banner spento). Ada ≤36 dp: clamp in codice (`AdaNoteView.MaxHeightDp=36`); in live Ada non compare come overlay grande. |
| 2 | Zone a terra / pad più contrastati e chiari | **VIA** | Pad nero+giallo / nero+blu ad alto contrasto su tile chiaro, entrambi i viewport. |
| 3 | NONNO 10 LN; tutorial Nonno prima di Zaino + top-up; lavora ≤10 s senza NavMeshAgent | **NO-VIA** | Mai assunto: wallet resta **0 LN**; tutorial bloccato su Bancone (nessun cliente servito / monete non raccolte in automazione). Nessun robot NONNO visibile dopo attesa ≥10 s. Costo 10 LN solo in dati sorgente (`robots.json hireCostLn: 10`), non verificato a pad in UI. |
| 4 | Solo obiettivo in alto, chiaro e stabile | **NO-VIA** | Nessuna riga obiettivo stabile in alto (`Muoviti` / `Vai agli Arrivi` / …). Solo marker mondo `ARRIVI · Xm` / `BANCONE · Xm` (centro/bordo, non “solo in alto”). |
| — | Console: niente `Failed to create agent because there is no valid NavMesh` | **VIA** | Su `127.0.0.1:8777` assente. Restano solo: favicon 404, `last-child` HudV2, deprecation `persistentDataPath`. |

---

## Dettaglio per priorità

### 1) HUD — **VIA**

**Passi:** fresh load 390×844 e 1080×2400 → osservare start e mid-tutorial.  
**Evidenza:** `v2_phone_01_start.png`, `v2_phone_02_hud_objective.png`, `v2_mcp_phone_01_start.png`, `v2_tall_01_start.png`, `v2_tall_02_hud_objective.png`, `v2_mcp_phone_04_scaffali.png`.  
**Risultato:** pillola LN+G in alto a sinistra (1 riga), gear, TAB; **nessun** chip Prendi/Scaffale/Pacchi; **nessuna** scheda pad gigante; area di gioco libera. Confronto netto vs build Pages precedente.

### 2) Zone a terra — **VIA**

**Passi:** camminare Arrivi → scaffali → Bancone; osservare pad.  
**Evidenza:** `v2_mcp_phone_02_walk.png`, `v2_mcp_phone_03_arrivi.png`, `v2_phone_06_bancone.png`, `v2_tall_06_bancone.png`, `v2_tall_07_coins.png`.  
**Risultato:** pad circolari nero/giallo e nero/blu chiaramente distinti dal pavimento.

### 3) NONNO — **NO-VIA**

**Passi tentati:** Arrivi (stack fino a 2–3/5) → scaffali → Bancone → roam monete → linger pad ~10 s+ → retry.  
**Evidenza fail:**  
- LN HUD sempre **0** (`v2_phone_07_coins.png`, `v2_phone_09_nonno_after_10s.png`, `v2_tall_09_nonno_after_10s.png`, `v2_mcp_phone_09_deliver.png`).  
- Obiettivo resta **BANCONE** (non avanza a Nonno/Zaino).  
- Moneta a terra vista (`v2_phone_09`) ma wallet non incrementa in sessione auto.  
- Nessun NONNO in scena dopo attesa.  
**Nota positiva:** console locale **senza** NavMeshAgent error (coerente con `NonnoRobot` Transform-only).  
**Nota codice (non live):** `hireCostLn: 10`, ordine step `… Bancone → Monete → Nonno → Zaino`, top-up LN su Enter(Nonno) — non esercitati perché Bancone non completa.

### 4) Obiettivo in alto — **NO-VIA**

**Passi:** tutti gli step osservati su entrambi i viewport.  
**Evidenza:** start/walk/arrivi/bancone (`v2_phone_02`, `v2_tall_02`, `v2_mcp_phone_03`, `v2_phone_06`) — **nessun** label top tipo “Vai agli Arrivi” / “Porta al Bancone”; solo waypoint mondo.  
**Nota codice:** `TutorialManager` crea `tut-objective` top o riusa `tutorial-objective` HUD; in build servita non risulta visibile/leggibile.

---

## Console (locale 8777)

| Messaggio | Bloccante? |
|-----------|------------|
| *(assente)* `Failed to create agent because there is no valid NavMesh` | — **OK** |
| `favicon.ico` 404 | No |
| `Unknown pseudo class "last-child" in StyleSheet HudV2` | No |
| `JS_FileSystem_Sync` / persistentDataPath deprecated | No |

Log: `qa/playtest_console_v2_local_only.log`

---

## Copertura sessioni

| Sessione | Viewport | Clear storage | Shot prefisso |
|----------|----------|---------------|---------------|
| Phone + MCP | 390×844 | sì | `v2_phone_*`, `v2_mcp_phone_*` |
| Tall | 1080×2400 | sì (nuova context) | `v2_tall_*` |

Artefatti: `qa/playtest_shots/v2_*.png` (~32 file), script `qa/playtest_web_live_v2.py`.

---

## Blocchi aperti per TL (per tornare a VIA)

1. **Obiettivo top** sempre visibile (1 riga) su 390×844 e 1080×2400 — verificare `tutorial-objective` in HUD v3.  
2. **Loop Bancone → monete → wallet** in WebGL (consegna clienti + pickup monete) finché LN > 0.  
3. Poi **pad NONNO 10 LN** leggibile, top-up tutorial, lavoro ≤10 s (già ok assenza NavMesh in console).
