#!/usr/bin/env python3
"""Controlli veloci #186, senza Unity. Esce 0 se tutto torna."""
import sys
from pathlib import Path

from fontTools.ttLib import TTFont
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
FAIL = []


def ok(cond, msg):
    if cond:
        print("ok ", msg)
    else:
        print("NO ", msg)
        FAIL.append(msg)


def text(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def guid_of(rel):
    for line in text(rel).splitlines():
        if line.startswith("guid:"):
            return line.split(":", 1)[1].strip()
    return ""


def main():
    for rel, copyright in (
        ("Assets/_Game/UI/Fonts/OFL/Baloo2/OFL.txt", "Baloo 2 Project Authors"),
        ("Assets/_Game/UI/Fonts/OFL/Fredoka/OFL.txt", "Fredoka Project Authors"),
    ):
        t = text(rel)
        ok("SIL Open Font License" in t and copyright in t, "OFL " + rel)

    for rel, family, style in (
        ("Assets/_Game/UI/Fonts/Baloo2-Bold.ttf", "Baloo 2", "Bold"),
        ("Assets/_Game/UI/Fonts/Fredoka-Medium.ttf", "Fredoka", "Medium"),
    ):
        raw = (ROOT / rel).read_bytes()
        ok(raw[:4] == b"\x00\x01\x00\x00" and len(raw) > 10000, "ttf vero " + rel)
        font = TTFont(ROOT / rel)
        ok(font["name"].getDebugName(1) == family and font["name"].getDebugName(2) == style, f"nome {family} {style}")

    baloo_guid = guid_of("Assets/_Game/UI/Fonts/Baloo2-Bold.ttf.meta")
    fredoka_guid = guid_of("Assets/_Game/UI/Fonts/Fredoka-Medium.ttf.meta")
    baloo_asset = guid_of("Assets/_Game/UI/Fonts/Baloo2-Bold_SDF.asset.meta")
    fredoka_asset = guid_of("Assets/_Game/UI/Fonts/Fredoka-Medium_SDF.asset.meta")
    baloo = text("Assets/_Game/UI/Fonts/Baloo2-Bold_SDF.asset")
    fredoka = text("Assets/_Game/UI/Fonts/Fredoka-Medium_SDF.asset")
    ok("m_FamilyName: Baloo 2\n" in baloo and "m_StyleName: Bold\n" in baloo, "Font Asset Baloo 2 Bold")
    ok("m_FamilyName: Fredoka\n" in fredoka and "m_StyleName: Medium\n" in fredoka, "Font Asset Fredoka Medium")
    ok("m_AtlasPopulationMode: 1" in baloo and "m_AtlasPopulationMode: 1" in fredoka, "atlas dinamico TextCore")
    ok(baloo_guid in baloo and fredoka_guid in fredoka, "i Font Asset puntano ai ttf")
    pannello = text("Assets/_Game/UI/Fonts/UM_PanelTextSettings.asset")
    ok(fredoka_asset in pannello and baloo_asset in pannello, "PanelTextSettings: Fredoka di base, Baloo in fallback")

    uss = text("Assets/_Game/UI/USS/Etichette.uss")
    for needle in ("Baloo2-Bold_SDF.asset", "Fredoka-Medium_SDF.asset", "tex_insegna_noce.png",
                   "#7A4A2E", "#F3E3C3", "#F2B53A", "#3B2A20", "#9E7873", "text-shadow"):
        ok(needle in uss, "USS " + needle)
    up = uss.upper()
    ok("#FFFFFF" not in up and "#FFF " not in up and "#FFF;" not in up, "niente bianco puro nel foglio")
    ok("tex_wood_panel" not in uss and "tex_cardboard_panel" not in uss, "le etichette non riusano i pannelli #185")
    ok("white-space: normal !important" in uss and "ellipsis" not in uss, "fumetto a capo, senza ellissi")
    ok("tex_fumetto_crema.png" in uss and "tex_capsula_noce_48.png" not in uss, "fumetto crema nello sprite; niente sprite di luce")
    ok("color: #3B2A20 !important" in uss and "tex_capsula_noce_32.png" in uss, "testo del fumetto bruno, Muoviti sulla capsula 32")
    ok("height: auto !important" in uss and "max-height: none !important" in uss and "padding-bottom: 20px !important" in uss, "fumetto senza tetto, padding che lascia i 4 px di ombra")
    ok("text-overflow: clip !important" in uss and "overflow: visible !important" in uss, "testo intero, non tagliato")
    ok("-unity-slice-bottom: 24px !important" in uss and "-unity-slice-bottom: 20px !important" in uss, "lo slice basso include l'ombra")
    ok("ombra-portata" not in uss and "border-radius: 0 !important" in uss, "niente elemento ombra: la forma è l'alpha dello sprite")
    ok("-unity-text-outline-width: 0 !important" in uss and "color: #F3E3C3 !important" in uss, "Muoviti crema, senza contorno chiaro")
    ok("right: 78px" in uss and "#9E7873" in uss, "vuoto prima dell'ingranaggio")
    ada = text("Assets/_Game/UI/Runtime/AdaNoteView.cs")
    ok("ada-carta" not in ada and "ombra-portata" not in ada and "note.Add(face)" in ada, "il fumetto non ha figli ombra")
    hud_uxml = text("Assets/_Game/UI/UXML/Hud.uxml")
    ok("objective-pill" in hud_uxml and "ombra-portata" not in hud_uxml, "Muoviti senza elemento ombra")
    tut = text("Assets/_Game/Scripts/Gameplay/TutorialManager.cs")
    ok("ombra-portata" not in tut and "_hintSrc = null" in tut, "suggerimento senza elemento ombra, cache #191")
    hud = text("Assets/_Game/UI/USS/HudV2.uss")
    ok("white-space: nowrap !important" not in hud, "HudV2 non forza più la riga unica")
    tokens = text("Assets/_Game/UI/Tokens.uss")
    ok("rgba(0, 0, 0, 0)" in tokens and "tex_cardboard_panel.png" in tokens, "il fix #185 sui pannelli è ancora in Tokens")
    pad = text("Assets/_Game/UI/USS/PadTag.uss").upper()
    ok("#FFFFFF" not in pad and "#5E7F86" not in pad and "#3E5A61" not in pad, "cartellino pad senza bianco né teal")

    src = text("Assets/_Game/Scripts/Gameplay/InteractionPoint.cs")
    ok("backgroundColor = Kind == PoiKind.Acquisto ? Giallo" not in src, "niente pillola gialla in linea")
    ok("unityFontDefinition = FontDefinition.FromFont" not in src, "niente font dinamico sul prezzo")
    ok("insegna-noce" in src and "poi-chip__valore" in src, "il prezzo usa l'insegna")

    png = Image.open(ROOT / "Assets/_Game/UI/Textures/tex_insegna_noce.png").convert("RGBA")
    bianchi = freddi = 0
    raw = png.tobytes()
    for i in range(0, len(raw), 4):
        r, g, b = raw[i], raw[i + 1], raw[i + 2]
        if r >= 250 and g >= 250 and b >= 250:
            bianchi += 1
        if (b > r + 18 and b > g + 8 and b > 90) or (r > 140 and b > 140 and b > g + 30 and r > g + 25):
            freddi += 1
    ok(bianchi == 0 and freddi == 0, "texture insegna senza bianco, blu o viola")
    ok(png.getchannel("A").getextrema()[0] < 255, "insegna con alpha davvero usato (il test #185)")

    for name, altezza in (
        ("tex_capsula_noce_32.png", 32),
        ("tex_capsula_noce_40.png", 40),
        ("tex_capsula_noce_48.png", 48),
        ("tex_capsula_terracotta_32.png", 32),
    ):
        im = Image.open(ROOT / "Assets/_Game/UI/Textures" / name).convert("RGBA")
        raggio = altezza // 2
        meta = text(f"Assets/_Game/UI/Textures/{name}.meta")
        bordo = f"spriteBorder: {{x: {raggio}, y: {raggio + 4}, z: {raggio}, w: {raggio}}}"
        ok(im.size[1] == altezza + 4 and bordo in meta, f"capsula {name} corpo {altezza} più ombra 4")
        angoli = [im.getpixel((0, 0)), im.getpixel((im.width - 1, 0)), im.getpixel((0, im.height - 1)), im.getpixel((im.width - 1, im.height - 1))]
        ombra = im.getpixel((im.width // 2, im.height - 2))
        ok(all(p[3] < 40 for p in angoli) and im.getpixel((im.width // 2, im.height // 2))[3] > 240, f"angoli vuoti {name}")
        ok(ombra == (158, 120, 115, 153), f"ombra cotta {name}")
        bianchi = freddi = 0
        raw = im.tobytes()
        for i in range(0, len(raw), 4):
            r, g, b, a = raw[i], raw[i + 1], raw[i + 2], raw[i + 3]
            if a < 20:
                continue
            if r >= 250 and g >= 250 and b >= 250:
                bianchi += 1
            if (b > r + 18 and b > g + 8 and b > 90) or (r > 140 and b > 140 and b > g + 30 and r > g + 25):
                freddi += 1
        ok(bianchi == 0 and freddi == 0, f"colori caldi {name}")

    bub = Image.open(ROOT / "Assets/_Game/UI/Textures/tex_fumetto_crema.png").convert("RGBA")
    bmeta = text("Assets/_Game/UI/Textures/tex_fumetto_crema.png.meta")
    ok(bub.size == (80, 52) and "spriteBorder: {x: 20, y: 24, z: 20, w: 20}" in bmeta, "fumetto 9-slice con l'ombra nello slice basso")
    ok("spriteMeshType: 0" in bmeta and "alphaIsTransparency: 1" in bmeta and "textureFormat: 4" in bmeta, "fumetto Full Rect, alpha tenuto anche su WebGL")
    ok(bub.getpixel((40, 26)) == (243, 227, 195, 255), "centro del fumetto crema opaco #F3E3C3")
    ok(bub.getpixel((40, 50)) == (158, 120, 115, 153), "ombra del fumetto #9E7873 al 60%")
    angoli_b = [bub.getpixel((0, 0)), bub.getpixel((79, 0)), bub.getpixel((0, 51)), bub.getpixel((79, 51))]
    ok(all(p[3] == 0 for p in angoli_b) and bub.getchannel("A").getextrema()[0] < 255, "angoli del fumetto vuoti, canale alpha usato")

    if FAIL:
        print(f"\n{len(FAIL)} controlli falliti")
        return 1
    print("\nTutti i controlli veloci sono verdi")
    return 0


if __name__ == "__main__":
    sys.exit(main())
