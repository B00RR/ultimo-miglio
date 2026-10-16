# Review #2 — Personaggi e clienti vs GUIDA_VISIVA_CAP1 (3D Artist, 16/10/2026)

## Confronto con la guida
| Personaggio | Guida | Stato su main | Azione |
|---|---|---|---|
| Nina Vela | §5 casco arancio, giacca aperta su t-shirt #CFC2AE, sciarpa scura, cargo oliva | v02 conforme (3.332 tri) | nessuna (rig dell'Animator intatto) |
| NONNO | §6 muletto ruggine, faccia, baffi, 6 raggi gialli, tromba | v03 conforme (3.060 tri) | nessuna |
| TICO | §6 menta, cappello terracotta, occhiali ottone, timbro | v02 conforme (1.528 tri) | nessuna |
| Clienti ×6 (base + 5) | §7 residente/rider/anziana/turista/VIP | v02 conformi (1.976–2.320 tri, sopra il budget ~1.5k) | **nessuna modifica**: già riggati dall'Animator (0c05198). Lo script v03 può produrre versioni ~1.2–1.55k tri (`UM_ONLY=base,residente,...`) se servirà ottimizzare, ma richiederebbe un re-bind |

## Render
Lineup dei 6 clienti (v03-uv): `lineup_iso34.png`, `lineup_fronte.png`.
Script: `3d/clienti/clienti_v03_uv.py`, `3d/clienti/render_review_2.py` (Blender 4.2.3, headless).

## v02 — fix dalla review del Concept Artist (PR #85)
Nonna Ada è stata tolta da questa PR (decisione del Producer: la consegna l'Animator nella PR #93).

**Clienti v03-uv**: SOLO remap UV su celle calde esistenti di T_Palette_Atlas (nessuna cella nuova). **Numero e ordine dei vertici e posizioni INVARIATI** (verificato via assert nello script `3d/clienti/clienti_v03_uv.py`); triangoli invariati.
- rider: cappello limone #F7E26B (prima era terracotta come il residente).
- Pelle: tono A pelle/pelle_d (base, anziana, invariati), tono B pelle_d/legno (residente, vip), tono C intonaco_terra/pelle (rider, turista).
- File cambiati: `3d/export/chr/chr_cliente_{residente,rider,turista,vip}.{fbx,glb,blend}`. base e anziana: nessun cambio. I FBX riggati in `Assets/_Game/Art/Characters/Clienti/` NON sono toccati: l'Animator deve trasferire le UV (o rifare il bind con gli stessi pesi, perché la topologia è identica).
