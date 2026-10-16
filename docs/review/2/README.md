# Review #2 — Personaggi e clienti vs GUIDA_VISIVA_CAP1 (3D Artist, 16/10/2026)

## Confronto con la guida
| Personaggio | Guida | Stato su main | Azione |
|---|---|---|---|
| Nina Vela | §5 casco arancio, giacca aperta su t-shirt #CFC2AE, sciarpa scura, cargo oliva | v02 conforme (3.332 tri) | nessuna (rig dell'Animator intatto) |
| NONNO | §6 muletto ruggine, faccia, baffi, 6 raggi gialli, tromba | v03 conforme (3.060 tri) | nessuna |
| TICO | §6 menta, cappello terracotta, occhiali ottone, timbro | v02 conforme (1.528 tri) | nessuna |
| Clienti ×6 (base + 5) | §7 residente/rider/anziana/turista/VIP | v02 conformi (1.976–2.320 tri, sopra il budget ~1.5k) | **nessuna modifica**: già riggati dall'Animator (0c05198). Lo script v03 può produrre versioni ~1.2–1.55k tri (`UM_ONLY=base,residente,...`) se servirà ottimizzare, ma richiederebbe un re-bind |
| **Nonna Ada** | §8 + aggiornamento (tavola v01) | **mancante** | **nuovo** `chr_nonna_ada` 1.518 tri |

## Nonna Ada (chr_nonna_ada)
Chignon bianco (crema) con matita gialla, occhiali tondi ottone + catenella, camicetta menta (colletto + scollo), cardigan terracotta lungo al ginocchio, grembiule noce (bruno_m #74503A) con tasche, attrezzi e libro mastro, fiocco dietro, spilla faro, pantaloni oliva, scarponi. Atlas unico T_Palette_Atlas, nessun colore nuovo.
- Scheletro **identico a Nina/clienti** (05_3D_PIPELINE §9): spalle (±0.19,0,1.27), gomiti (±0.237,0,1.014), polsi (±0.274,-0.02,0.783), bacino 0.86, collo 1.34, ginocchia (±0.11,-0.01,0.47), caviglie (±0.115,0,0.12); pivot tra i piedi, min Z 0 → si riusa il rig Humanoid di Nina (~24 ossa).
- Vertex group (solo nel .blend): hair, hair_bun, pencil, glasses, blouse_collar, cardigan_skirt, apron, ledger, pin_faro → rigidi su head (hair/bun/pencil/glasses), chest (collar, pin, pettorina), pelvis (cardigan_skirt, apron, ledger).
- Limite noto: con lo scheletro condiviso è alta come i clienti (non «piccola»): la statura ridotta andrebbe fatta con scala nel prefab o rig dedicato.

## Render
fronte/3-4/retro/iso 3-4: `ada_*.png`; lineup con i 6 clienti v02: `lineup_iso34.png`, `lineup_fronte.png`.
Generatori: `3d/clienti/build_clienti_v03.py`, `3d/clienti/render_review_2.py` (Blender 4.2.3, headless).
