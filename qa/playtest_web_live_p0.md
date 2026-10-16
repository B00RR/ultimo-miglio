# LIVE P0 — build/WebGL_p0 (8780), ramo fix-p0-lorenzo
Data: 16/10/2026 ~09:30 (Roma). Playwright Python headless (SwiftShader), contesto touch (has_touch, is_mobile) 390x844 / 360x800 / 1170x2532. Storage svuotato + reload. Script: `../pw_p0.py`, `../pw_p0b.py`. Shot: `demo/p0_*.png`.
Prove in-engine: PlayMode 11/11 (incl. P0CollisionFacingTest 3/3, Ch1LoopTest, NonnoWorkTest, CameraFrameTest), EditMode 35/35.

| ID | Esito | Evidenza |
|---|---|---|
| C01 Muro N/archi | VIA | p0_390_wall_N.png (Nina ferma davanti all'insegna); P0CollisionFacingTest |
| C02 Muro E/arco | VIA | p0_390_wall_E.png; test |
| C03 Bordo S | VIA | p0_390_wall_S.png; test (Bound_S) |
| C04 Bordo O | VIA | p0_390_wall_W.png; test (Bound_W) |
| C05 Angoli | VIA | p0_390_wall_NE.png, p0_390_wall_SO.png |
| C06 Scaffali | VIA | test: Scaffale_A/B/C con BoxCollider, nessuna compenetrazione |
| C07 Bancone | VIA | p0_390_dir_d.png; test (Bancone_Mesh) |
| C08 Props | VIA | test (vasi, casse, set >25 cm solidi); shot vaso |
| C09 Recinzioni/fioriere | VIA | test (oggetti a terra solidi) |
| C10 Pad trigger | VIA | Ch1LoopTest; pad attraversati negli shot |
| C11 NONNO vs solidi | VIA | AgentMove.Slide (SphereCast); NonnoWorkTest/Walkers |
| C12 Clienti vs solidi | VIA | AgentMove.Slide; Walkers_FaceMovementDirection |
| A01 Nina orientamento | VIA | p0_390_dir_*.png; test dot p10>0.9 |
| A02 Nina clip | VIA | walk/idle negli shot; carry/cheer coperti da Ch1LoopTest (non a schermo) |
| A03 NONNO orientamento | VIA | test dot p10>0.9 (yaw rig 0°); strip a schermo non fatta (fps headless ~4) |
| A04 NONNO clip | VIA | Speed = velocità reale |
| A05 Clienti | VIA | test dot>0.9, Speed reale |
| A06 Ada | n.v. | non raggiunta a schermo |
| P01–P05, Z01, R02 | VIA (in-engine) | Ch1LoopTest_PickStackDeliverServeBuyZaino, HireNonno_*; non percorsi a schermo |
| P06 altri upgrade | n.v. | |
| B01 Ada B4 | n.v. | |
| S01 Impostazioni | VIA con nota | p0_390_settings.png: si apre; si chiude solo con SALVA, il tap sull'ingranaggio non la richiude (P3) |
| S02–S05 | n.v. | |
| V01 Salvataggio | VIA parziale | p0_390_reload.png: reload pulito, nessun errore; stato a 0 LN invariato |
| V02 Offline | n.v. | |
| K01/K02 Camera | VIA | CameraFrameTest; Nina sempre in quadro negli shot |
| M01/M02/M03 Viewport | VIA | p0_390_start, p0_360_start, p0_1170_later: HUD leggibile, niente tagli |
| M04 Touch drag | VIA | p0_360_touchdrag.png: joystick visibile, Nina si muove lungo il drag |
| F01 FPS | annotato | headless SwiftShader 4–5 fps (390/360), 1,4 fps (1170, DPR 1). Non rappresentativo del device |
| F02 Memoria | annotato | heap JS 31–37 MB, stabile |
| F03 Console | VIA | 0 errori/pageerror in 3 viewport |
| R01 Regressione | VIA (in-engine) | suite verde |

Nessun NO-VIA P0/P1 trovato a schermo.
Note extra:
- P2: il muro set dietro il Bancone (DepotKit.DressGameplay, muro+arco a banPos+2.8) crea una sacca giocabile: con un drag verso NE Nina ci entra e la camera la mostra "dietro un muro" (p0_360_touchdrag.png, p0_1170_later.png). Non è un'uscita dalla mappa ma confonde la lettura.
- P3: le impostazioni non si chiudono col tap sull'ingranaggio.
