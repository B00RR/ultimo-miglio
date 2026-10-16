#!/usr/bin/env python3
"""Controlli veloci #186, senza Unity. Esce 0 se tutto torna."""
import subprocess
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
    ok("-unity-slice" not in uss and "tex_wood_panel" not in uss and "tex_cardboard_panel" not in uss, "niente 9-slice della #185")
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

    diff = subprocess.check_output(["git", "diff", "--name-only", "70118057864710977b3951be5b7ad2fc57e60b4e"], cwd=ROOT, text=True)
    vietati = [
        "Assets/_Game/UI/Textures/tex_wood_panel.png",
        "Assets/_Game/UI/Textures/tex_cardboard_panel.png",
        "Assets/_Game/UI/Tokens.uss",
        "Assets/_Game/UI/USS/TutorialHud.uss",
        "Assets/_Game/UI/USS/LegnoCartone.uss",
    ]
    toccati = [v for v in vietati if v in diff.splitlines()]
    ok(not toccati, "file 9-slice della #185 intatti" + ("" if not toccati else " " + ",".join(toccati)))

    if FAIL:
        print(f"\n{len(FAIL)} controlli falliti")
        return 1
    print("\nTutti i controlli veloci sono verdi")
    return 0


if __name__ == "__main__":
    sys.exit(main())
