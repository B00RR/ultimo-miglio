#!/usr/bin/env python3
"""Gioca il capitolo 1 nel browser, con input veri, leggendo Kaia dal ponte ?qa=1.

Uso: python3 qa/gioca_cap1.py http://127.0.0.1:8765/index.html
Scrive qa/diario_gioco_cap1.md e gli screenshot in qa/playtest_shots/.
"""
import json
import math
import os
import sys
import time
from playwright.sync_api import sync_playwright

BASE = sys.argv[1] if len(sys.argv) > 1 else "http://127.0.0.1:8765/index.html"
SEP = "&" if "?" in BASE else "?"
URL = BASE + SEP + "qa=1"
OUT = os.path.join(os.path.dirname(__file__), "playtest_shots")
DIARIO = os.path.join(os.path.dirname(__file__), "diario_gioco_cap1.md")
os.makedirs(OUT, exist_ok=True)

# Nomi propri della bibbia: restano così. Tutto il resto del fumetto è italiano.
SOSPETTI = ("Nonna Ada", "Mamma Rosa", "Vela & Figli", "Nina Vela", "Ultimo Miglio", "Skip tutorial", "Settings")
# Auntie Lulu e Sunn Depot sono i nomi della bibbia: li segnalo a parte, non come bug di traduzione.
NOMI_BIBBIA = ("Auntie Lulu", "Sunn Depot", "Oma Tilde", "OLDO", "Kaia", "GrayBox")

voci = []
nshot = 0


def leggi(page):
    return page.evaluate("() => window.umQa || null")


def sospetti(testo):
    if not testo:
        return []
    return [s for s in SOSPETTI if s in testo]


def bibbia(testo):
    if not testo:
        return []
    return [s for s in NOMI_BIBBIA if s in testo]


def scatta(page, nome, fatto, atteso, successo, stato):
    global nshot
    nshot += 1
    fn = f"gioco23_{nshot:02d}_{nome}.png"
    path = os.path.join(OUT, fn)
    page.screenshot(path=path)
    voce = {
        "n": nshot,
        "nome": nome,
        "fatto": fatto,
        "atteso": atteso,
        "successo": successo,
        "file": f"qa/playtest_shots/{fn}",
        "step": (stato or {}).get("step"),
        "text": (stato or {}).get("text"),
        "objective": (stato or {}).get("objective"),
        "hint": (stato or {}).get("hint"),
        "wallet": (stato or {}).get("wallet"),
        "zaino": (stato or {}).get("zaino"),
        "nonno": (stato or {}).get("nonno"),
        "kaia": (stato or {}).get("kaia"),
        "target": (stato or {}).get("target"),
        "sospetti": sospetti(((stato or {}).get("text") or "") + " " + ((stato or {}).get("objective") or "")),
        "nomi": bibbia(((stato or {}).get("text") or "") + " " + ((stato or {}).get("objective") or "")),
    }
    voci.append(voce)
    print(f"[{nshot}] {nome} step={voce['step']} {successo[:140]}")
    return voce


def verso_schermo(stato, tx, tz, raggio=48):
    """Da un punto mondo al trascinamento del dito. Stessa inversa dei test: Quaternion.Euler(0, -yaw, 0) * direzione.
    Value.y positivo = su dello schermo = dy Playwright negativo."""
    k = stato.get("kaia") or {"x": 0, "z": 0}
    dx, dz = tx - k["x"], tz - k["z"]
    dist = math.hypot(dx, dz)
    if dist < 1e-4:
        return 0, 0, dist
    yaw = math.radians(stato.get("yaw") or 45)
    c, s = math.cos(yaw), math.sin(yaw)
    # Unity, rotazione Y di -yaw (sistema sinistrorso): x' = x c - z s, z' = x s + z c
    lx = dx * c - dz * s
    lz = dx * s + dz * c
    m = math.hypot(lx, lz) or 1
    return lx / m * raggio, -lz / m * raggio, dist


def ruota(ox, oy, gradi):
    a = math.radians(gradi)
    c, s = math.cos(a), math.sin(a)
    return ox * c - oy * s, ox * s + oy * c


# Quanto si sposta Kaia tenendo il dito 50 px in su e a destra. Si misura una volta, a inizio gioco.
BASE_N = None
BASE_E = None

# Varchi del layout (DepotKit, distanze di zones.json: Arrivi −5,6; scaffali a 6 m; Bancone a 9,4 m).
# Il giocatore segue la freccia che passa di qui. Il ponte, se pubblica "tappa", ha la precedenza.
VARCHI = (
    ("ScaffaliEst", -0.6, 1.2, 0.9),
    ("UscitaOvest", 0.2, -4.6, 0.7),
    ("CorridoioOvest", 1.52, -5.27, 0.9),
    ("ArcoBancone", 3.42, -4.19, 1.2),
    ("ArcoEst", 5.72, -5.94, 1.2),
    ("StrisciaEst", 6.67, -4.34, 2.0),
    ("AngoloCassa", 2.0, 0.9, 0.7),
)


def scegli_meta(st, bloccata):
    """Punto verso cui spingere il dito: la tappa del ponte, oppure un varco se la retta non fa avvicinare."""
    tappa = st.get("tappa") or {}
    if tappa.get("x") is not None and tappa.get("name"):
        return tappa.get("x"), tappa.get("z"), tappa.get("name")
    tgt = st.get("target") or {}
    if not tgt.get("name") or tgt.get("x") is None:
        return None
    k = st.get("kaia") or {}
    kx, kz = k.get("x") or 0, k.get("z") or 0
    tx, tz = tgt["x"], tgt["z"]
    diretta = math.hypot(tx - kx, tz - kz)
    if diretta < 3.5 and not bloccata:
        return tx, tz, tgt["name"]
    scelta = (tx, tz, tgt["name"])
    best = 1e9
    for nome, x, z, raggio in VARCHI:
        if math.hypot(x - kx, z - kz) < raggio:
            continue
        if math.hypot(tx - x, tz - z) > diretta - 1.2:
            continue
        giro = math.hypot(x - kx, z - kz) + math.hypot(tx - x, tz - z)
        if giro < best:
            best = giro
            scelta = (x, z, nome)
    return scelta


def campione_dito(page, ox, oy, ms=700):
    origine = (200, 700)
    st = leggi(page) or {}
    k0 = st.get("kaia") or {"x": 0, "z": 0}
    page.mouse.move(*origine)
    page.mouse.down()
    page.mouse.move(origine[0] + ox, origine[1] + oy)
    page.wait_for_timeout(ms)
    page.mouse.up()
    page.wait_for_timeout(80)
    st2 = leggi(page) or {}
    k1 = st2.get("kaia") or k0
    return (k1["x"] - k0["x"], k1["z"] - k0["z"])


def calibra(page):
    """Spinte in otto direzioni: due che muovono Kaia e non sono parallele diventano la bussola del dito."""
    global BASE_N, BASE_E
    direzioni = [(0, -50), (50, 0), (0, 50), (-50, 0), (35, -35), (35, 35), (-35, 35), (-35, -35)]
    utili = []
    for ox, oy in direzioni:
        d = campione_dito(page, ox, oy, 450)
        print(f"  dito ({ox},{oy}) -> {tuple(round(v, 2) for v in d)}")
        if math.hypot(*d) > 0.25:
            utili.append((ox, oy, d))
        if len(utili) >= 2:
            det = utili[0][2][0] * utili[1][2][1] - utili[0][2][1] * utili[1][2][0]
            if abs(det) > 0.15:
                break
            utili.pop()
    if len(utili) < 2:
        print("calibrazione ferma")
        return False
    # base_n è la spinta «su» (oy negativo), base_e la spinta «destra», ricostruite dal campione.
    (ox1, oy1, (wx1, wz1)), (ox2, oy2, (wx2, wz2)) = utili[0], utili[1]
    # [wx] = M * [ox, oy]. Risolvi le due colonne con i due campioni.
    det = ox1 * oy2 - oy1 * ox2
    if abs(det) < 1:
        print("calibrazione degenere", utili)
        return False
    # colonna ox: mondo prodotto da (1, 0) in pixel del campione
    ex = (wx1 * oy2 - wx2 * oy1) / det
    ez = (wz1 * oy2 - wz2 * oy1) / det
    nx = (ox1 * wx2 - ox2 * wx1) / det
    nz = (ox1 * wz2 - ox2 * wz1) / det
    # verso_empirico si aspetta il mondo di una spinta su (oy=-50) e di una spinta destra (ox=50)
    BASE_E = (ex * 50, ez * 50)
    BASE_N = (nx * -50, nz * -50)
    print(f"calibrazione su={tuple(round(v, 2) for v in BASE_N)} destra={tuple(round(v, 2) for v in BASE_E)}")
    return True


def verso_empirico(stato, tx, tz, raggio=48):
    k = stato.get("kaia") or {"x": 0, "z": 0}
    dx, dz = tx - k["x"], tz - k["z"]
    dist = math.hypot(dx, dz)
    if BASE_N is None or dist < 1e-4:
        return verso_schermo(stato, tx, tz, raggio)
    nx, nz = BASE_N
    ex, ez = BASE_E
    det = nx * ez - nz * ex
    if abs(det) < 1e-4:
        return verso_schermo(stato, tx, tz, raggio)
    a = (dx * ez - dz * ex) / det
    b = (nx * dz - nz * dx) / det
    ox, oy = b * raggio, -a * raggio
    m = math.hypot(ox, oy) or 1
    return ox / m * raggio, oy / m * raggio, dist


def stato_ok(page, timeout_s=90):
    t0 = time.time()
    ultimo = None
    while time.time() - t0 < timeout_s:
        ultimo = leggi(page)
        if ultimo and ultimo.get("ready"):
            return ultimo
        page.wait_for_timeout(250)
    return ultimo


def cammina(page, secondi, punto, nome, atteso):
    """Tiene il dito premuto verso un punto mondo. Ritorna l'ultimo stato."""
    origine = (200, 700)
    page.mouse.move(*origine)
    page.mouse.down()
    t0 = time.time()
    fermo_da = time.time()
    ultima = None
    invertito = False
    while time.time() - t0 < secondi:
        st = leggi(page) or ultima
        if not st:
            page.wait_for_timeout(200)
            continue
        prima = (st.get("kaia") or {}).get("x"), (st.get("kaia") or {}).get("z")
        ox, oy, dist = verso_schermo(st, punto[0], punto[1])
        if invertito:
            oy = -oy
        if dist < 0.45:
            page.mouse.move(*origine)
        else:
            page.mouse.move(origine[0] + ox, origine[1] + oy)
        page.wait_for_timeout(200)
        st2 = leggi(page) or st
        dopo = (st2.get("kaia") or {}).get("x"), (st2.get("kaia") or {}).get("z")
        if prima[0] is not None and dopo[0] is not None:
            mosso = math.hypot((dopo[0] or 0) - (prima[0] or 0), (dopo[1] or 0) - (prima[1] or 0))
            if mosso > 0.05:
                fermo_da = time.time()
            elif dist > 0.8 and time.time() - fermo_da > 2.5 and not invertito:
                invertito = True
                fermo_da = time.time()
                print("  inversione asse Y del dito")
        ultima = st2
        if nome and st2.get("step") and st2.get("step") != nome:
            break
    page.mouse.up()
    page.wait_for_timeout(150)
    return leggi(page) or ultima


def gioca_passi(page):
    """Segue il bersaglio del tutorial finché il capitolo non si chiude, o per 9 minuti."""
    visti = set()
    t0 = time.time()
    reload_fatto = False
    blocco = None
    blocchi_visti = set()
    fermo_da = time.time()
    pos_ferma = None
    origine = (180, 620)
    dito = False
    # Gradi aggiunti al dito quando Kaia non si avvicina (scaffale in mezzo). Si tiene finché avvicina.
    sbandata = 0
    dist_migliore = None
    ultimo_stampa = 0
    while time.time() - t0 < 720:
        st = leggi(page)
        if not st or not st.get("ready"):
            page.wait_for_timeout(300)
            continue
        step = st.get("step") or ""
        if step and step not in visti:
            visti.add(step)
            if dito:
                page.mouse.up()
                dito = False
                page.wait_for_timeout(100)
            atteso = "Il passo cambia da solo quando il gate è soddisfatto. Il testo è quello del design."
            fatto = f"Arrivata al passo {step}. Obiettivo «{st.get('objective')}». Fumetto «{st.get('text')}»."
            scatta(page, step, fatto, atteso, f"Passo {step}, portafoglio {st.get('wallet')} LN, zaino {st.get('zaino')}, OLDO {st.get('nonno')}, coda {st.get('coda')}.", st)
            if step == "ColdOpen":
                scatta(page, "coldopen_skip", "Controllo se «Salta tutorial» è acceso al cold open.",
                       "Il design (ONB-15, onboarding.json) lo tiene spento fino alla combo.",
                       "Acceso." if st.get("skip") else "Spento, come da design.", st)
            fermo_da = time.time()
            pos_ferma = st.get("kaia")
            dist_migliore = None
            sbandata = 0
        if step in ("Chiuso", "FreePlay") and "FreePlay" in visti:
            # un giro sul free play e poi si esce dal ciclo di guida
            if time.time() - t0 > 5 and step == "Chiuso":
                break
            if step == "FreePlay" and time.time() - fermo_da > 3:
                break
        tgt = st.get("target") or {}
        bloccata = time.time() - fermo_da > 1.1 and step not in ("ColdOpen", "Combo", "NonnoIntro", "NonnoConfirm", "WallPad", "FreePlay", "Chiuso")
        scelto = scegli_meta(st, bloccata)
        if scelto:
            ox, oy, dist = verso_schermo(st, scelto[0], scelto[1])
            meta_nome = scelto[2]
        elif step == "Move":
            ox, oy, dist = 0.0, -48.0, 9.0
            meta_nome = "avanti"
        else:
            ox, oy, dist = 0.0, 0.0, 0.0
            meta_nome = ""
        if scelto or step == "Move":
            if not dito:
                page.mouse.move(*origine)
                page.mouse.down()
                dito = True
            ox, oy = ruota(ox, oy, sbandata)
            sul_bersaglio = bool(scelto) and scelto[2] == (tgt.get("name") or "")
            # Sul pad e sul bancone si resta fermi: è così che si paga e si serve.
            if dist < 0.55 and sul_bersaglio and step != "Move":
                page.mouse.move(*origine)
            else:
                page.mouse.move(origine[0] + ox, origine[1] + oy)
        page.wait_for_timeout(180)
        st2 = leggi(page) or st
        k = st2.get("kaia") or {}
        if scelto and k:
            dist_ora = math.hypot(scelto[0] - (k.get("x") or 0), scelto[1] - (k.get("z") or 0))
        elif tgt.get("x") is not None and k:
            dist_ora = math.hypot((tgt.get("x") or 0) - (k.get("x") or 0), (tgt.get("z") or 0) - (k.get("z") or 0))
        else:
            dist_ora = dist
        if time.time() - ultimo_stampa > 3 and k:
            ultimo_stampa = time.time()
            print(f"  {step} via {meta_nome} kaia=({k.get('x')},{k.get('z')}) dist {dist_ora:.1f} LN {st2.get('wallet')} stack {st2.get('stack')}")
        if k and pos_ferma:
            if dist_ora < (dist_migliore if dist_migliore is not None else 1e9) - 0.25:
                pos_ferma = k
                dist_migliore = dist_ora
                fermo_da = time.time()
                sbandata = 0
            elif time.time() - fermo_da > 1.6 and step not in ("ColdOpen", "NonnoIntro", "NonnoConfirm", "WallPad", "FreePlay", "Chiuso", "Combo"):
                ciclo = (0, 40, -40, 80, -80)
                try:
                    i = ciclo.index(sbandata)
                except ValueError:
                    i = -1
                sbandata = ciclo[(i + 1) % len(ciclo)]
                fermo_da = time.time()
                print(f"  sbandata {sbandata}° dist {dist_ora:.2f} via {meta_nome} da ({k.get('x')},{k.get('z')})")
                if sbandata == -80 and step not in blocchi_visti:
                    blocchi_visti.add(step)
                    blocco = step
                    scatta(page, "blocco_" + step,
                           f"Kaia non si avvicina a {tgt.get('name')} nemmeno passando dai varchi.",
                           "La freccia del tutorial la porta al bersaglio.",
                           f"Resta a ({k.get('x')},{k.get('z')}), a {dist_ora:.1f} m. Passo bloccato.",
                           st2)
        # un reload a metà, dopo lo Zaino o dopo OLDO
        if not reload_fatto and st2.get("zaino", 0) >= 1:
            if dito:
                page.mouse.up()
                dito = False
            prima = leggi(page)
            scatta(page, "prima_reload", "Annoto lo stato e ricarico la pagina, senza cancellare il salvataggio.",
                   "Passo, LN, Zaino e OLDO restano quelli di prima.",
                   f"Prima: passo {prima.get('step')} LN {prima.get('wallet')} zaino {prima.get('zaino')} OLDO {prima.get('nonno')}.",
                   prima)
            # Il salvataggio WebGL parte quando la pagina perde il focus (GameRoot.OnApplicationFocus).
            pagina2 = page.context.new_page()
            pagina2.goto("about:blank")
            page.wait_for_timeout(800)
            page.bring_to_front()
            pagina2.close()
            page.wait_for_timeout(400)
            page.reload(wait_until="domcontentloaded")
            page.wait_for_selector("canvas")
            page.wait_for_function("() => !document.getElementById('l')", timeout=90000)
            dopo = stato_ok(page, 90)
            if dopo:
                uguale = dopo.get("step") == prima.get("step") and abs((dopo.get("wallet") or 0) - (prima.get("wallet") or 0)) < 1 and dopo.get("zaino") == prima.get("zaino")
                scatta(page, "dopo_reload", "Ricarico e rileggo il ponte.",
                       "Stesso passo, stesse LN, stesso Zaino.",
                       "Uguale." if uguale else f"Diverso: passo {dopo.get('step')} LN {dopo.get('wallet')} zaino {dopo.get('zaino')} OLDO {dopo.get('nonno')}.",
                       dopo)
            reload_fatto = True
            fermo_da = time.time()
            visti.discard(dopo.get("step") if dopo else None)
    if dito:
        page.mouse.up()
    return blocco


def probe_muri(page):
    st = leggi(page)
    if not st:
        return
    # quattro spinte lunghe verso i bordi della mappa
    mete = [("muro_n", (0, 20), "z resta ≤ 7"), ("muro_s", (0, -20), "z resta ≥ −10,2"),
            ("muro_e", (20, 0), "x resta ≤ 7"), ("muro_o", (-20, 0), "x resta ≥ −8,6")]
    for nome, punto, atteso in mete:
        fine = cammina(page, 4.5, punto, None, atteso)
        k = (fine or {}).get("kaia") or {}
        scatta(page, nome, f"Tengo il dito 4 secondi verso {punto}.", atteso,
               f"Kaia finisce a x={k.get('x')} z={k.get('z')}.", fine)


def probe_impostazioni(page):
    page.mouse.up()
    page.wait_for_timeout(200)
    page.mouse.click(354, 30)
    page.wait_for_timeout(600)
    st = leggi(page)
    scatta(page, "impostazioni_apri", "Tap sull'ingranaggio in alto a destra (354, 30).",
           "Il pannello Impostazioni si apre.",
           "Aperto." if st and st.get("impostazioni") else "Il ponte dice che il pannello è chiuso.",
           st)
    page.mouse.click(354, 30)
    page.wait_for_timeout(500)
    st = leggi(page)
    scatta(page, "impostazioni_chiudi", "Secondo tap sull'ingranaggio.",
           "Il pannello si chiude.",
           "Chiuso." if st and not st.get("impostazioni") else "Risulta ancora aperto.",
           st)


def probe_viewport(page, w, h, nome):
    page.set_viewport_size({"width": w, "height": h})
    page.wait_for_timeout(800)
    st = leggi(page)
    scatta(page, nome, f"Cambio il viewport a {w}×{h} senza ricaricare.",
           "Il gioco resta in quadro e il passo non si perde.",
           f"Passo {st.get('step') if st else '?'} a {w}×{h}.",
           st)


def scrivi():
    via = True
    righe = ["# Diario di gioco — capitolo 1", "",
             "Build giocata in Chrome, viewport 390×844, dito vero (trascinamento). La posizione di Kaia arriva dal ponte `?qa=1`, che non cambia le regole.",
             ""]
    for v in voci:
        if v["successo"].startswith("Resta a") or "Diverso:" in v["successo"] or v["sospetti"]:
            via = False
        righe.append(f"## {v['n']}. {v['nome']}")
        righe.append(f"- Fatto: {v['fatto']}")
        righe.append(f"- Atteso: {v['atteso']}")
        righe.append(f"- Successo: {v['successo']}")
        if v["sospetti"]:
            righe.append(f"- Testi da verificare: {', '.join(v['sospetti'])}")
        if v["nomi"]:
            righe.append(f"- Nomi della bibbia presenti: {', '.join(v['nomi'])}")
        righe.append(f"- Shot: `{v['file']}`")
        righe.append("")
    righe.append(f"Verdetto automatico dello script: **{'NO-VIA' if not via else 'DA LEGGERE'}**. Il VIA lo decide chi rilegge questo diario.")
    with open(DIARIO, "w") as f:
        f.write("\n".join(righe) + "\n")
    with open(os.path.join(os.path.dirname(__file__), "diario_gioco_raw.json"), "w") as f:
        json.dump(voci, f, ensure_ascii=False, indent=2)
    print("diario", DIARIO, "voci", len(voci))


def main():
    with sync_playwright() as p:
        browser = p.chromium.launch(channel="chrome", headless=True, args=[
            "--use-gl=angle", "--use-angle=swiftshader", "--enable-webgl", "--ignore-gpu-blocklist"])
        # Viewport da telefono. Il dito è il trascinamento del mouse: su WebGL lo stick legge il mouse,
        # e due tocchi insieme lo sospendono (issue #205).
        ctx = browser.new_context(viewport={"width": 390, "height": 844}, device_scale_factor=1)
        page = ctx.new_page()
        errori = []
        page.on("console", lambda m: errori.append(m.type + ": " + m.text[:180]) if m.type in ("error", "warning") else None)
        page.goto(URL, wait_until="domcontentloaded")
        page.wait_for_selector("canvas")
        page.wait_for_function("() => !document.getElementById('l')", timeout=90000)
        page.wait_for_timeout(1500)
        st = stato_ok(page, 90)
        if not st:
            scatta(page, "boot_fallito", "Apro la build e aspetto il ponte.",
                   "window.umQa.ready diventa vero.", "Il ponte non arriva.", st)
            open(os.path.join(OUT, "console_gioco.txt"), "w").write("\n".join(errori))
            scrivi()
            browser.close()
            return 1
        scatta(page, "boot", "Aspetto il caricamento con ?qa=1.",
               "Il ponte legge il passo coldOpen.",
               f"Passo {st.get('step')}, testo «{st.get('text')}».", st)
        gioca_passi(page)
        probe_impostazioni(page)
        probe_muri(page)
        probe_viewport(page, 360, 800, "vista_360")
        probe_viewport(page, 1170, 2532, "vista_1170")
        probe_viewport(page, 390, 844, "vista_390")
        open(os.path.join(OUT, "console_gioco.txt"), "w").write("\n".join(errori[:80]))
        scrivi()
        browser.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
