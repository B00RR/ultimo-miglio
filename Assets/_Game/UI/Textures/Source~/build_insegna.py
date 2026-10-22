#!/usr/bin/env python3
"""Texture HUD direzione C (insegna di noce). Ombre piene #9E7873, niente blur.
Genera i 9-slice, le icone ritagliate dall'atlante e le anteprime 390x844 / 360x800.
Unity ignora questa cartella (Source~).
"""
import os
import uuid
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TEX = os.path.join(ROOT, "Textures")
ICONS = os.path.join(ROOT, "Icons")
FONTS = os.path.join(ROOT, "Fonts")
OUT_PREV = os.path.abspath(os.path.join(ROOT, "..", "..", "..", "docs", "qa", "hud_insegna_c"))

INK = (59, 42, 32, 255)
WALNUT = (122, 74, 46, 255)
WALNUT_HI = (166, 112, 74, 255)
WALNUT_DK = (74, 42, 26, 255)
CREAM = (243, 227, 195, 255)
CREAM_HI = (236, 216, 176, 255)
GOLD = (242, 181, 58, 255)
GOLD_DK = (184, 122, 36, 255)
GOLD_HI = (255, 214, 120, 255)
SUN = (244, 196, 48, 255)
TERR = (200, 99, 59, 255)
TERR_DK = (142, 63, 34, 255)
TERR_HI = (224, 132, 90, 255)
CARD = (201, 154, 98, 255)
CARD_HI = (224, 186, 136, 255)
CARD_DK = (168, 122, 72, 255)
SHADOW = (158, 120, 115, 255)
TUFF = (226, 208, 176, 255)
TUFF_HI = (240, 228, 204, 255)
TUFF_DK = (176, 156, 124, 255)
RECESS = (48, 30, 20, 255)
MINT = (143, 201, 168, 255)

S = 4  # supersample: l'antialias resta nella texture, niente blur a runtime


def blank(w, h):
    return Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))


def down(im, w, h):
    return im.resize((w, h), Image.Resampling.LANCZOS)


def rr(d, box, r, fill=None, outline=None, width=0):
    x0, y0, x1, y1 = box
    d.rounded_rectangle((x0 * S, y0 * S, x1 * S, y1 * S), radius=r * S, fill=fill, outline=outline, width=width)


def panel(w, h, radius, fill, hi, dk, border=4, shadow=(4, 5), grain=None):
    im = blank(w, h)
    d = ImageDraw.Draw(im)
    sx, sy = shadow
    # ombra piena, offset, niente sfocatura
    rr(d, (sx, sy, w - 2 + sx, h - 2 + sy), radius, fill=SHADOW)
    # bevel: chiaro in alto a sinistra, scuro in basso a destra
    rr(d, (0, 0, w - 2, h - 3), radius, fill=dk)
    rr(d, (1, 1, w - 4, h - 6), radius, fill=hi)
    inset = border
    rr(d, (inset, inset + 1, w - inset - 2, h - inset - 4), max(2, radius - 2), fill=fill)
    rr(d, (0, 0, w - 2, h - 3), radius, outline=INK, width=max(2, border - 1) * S)
    if grain:
        g = ImageDraw.Draw(im)
        for y in range(inset + 6, h - inset - 6, 5):
            g.line([(inset + 8) * S, y * S, (w - inset - 10) * S, y * S], fill=grain, width=S)
    return down(im, w, h)


def coin(n=128):
    im = blank(n, n)
    d = ImageDraw.Draw(im)
    m = 6
    d.ellipse((m * S, (m + 3) * S, (n - m) * S, (n - m + 3) * S), fill=SHADOW)
    d.ellipse((m * S, m * S, (n - m) * S, (n - m) * S), fill=GOLD_DK)
    d.ellipse(((m + 4) * S, (m + 3) * S, (n - m - 2) * S, (n - m - 3) * S), fill=GOLD_HI)
    d.ellipse(((m + 7) * S, (m + 7) * S, (n - m - 7) * S, (n - m - 7) * S), fill=GOLD)
    d.ellipse((m * S, m * S, (n - m) * S, (n - m) * S), outline=INK, width=3 * S)
    # faro inciso (bruno, non un buco)
    cx, cy = n // 2, n // 2 + 4
    tower = [(cx - 7, cy + 22), (cx + 7, cy + 22), (cx + 4, cy - 6), (cx - 4, cy - 6)]
    d.polygon([(p[0] * S, p[1] * S) for p in tower], fill=INK)
    d.rectangle(( (cx - 10) * S, (cy + 20) * S, (cx + 10) * S, (cy + 26) * S), fill=INK)
    d.rectangle(((cx - 6) * S, (cy - 14) * S, (cx + 6) * S, (cy - 4) * S), fill=INK)
    d.polygon([((cx - 8) * S, (cy - 14) * S), ((cx + 8) * S, (cy - 14) * S), (cx * S, (cy - 24) * S)], fill=INK)
    # fascio corto, crema
    d.polygon([((cx + 6) * S, (cy - 12) * S), ((cx + 22) * S, (cy - 20) * S), ((cx + 20) * S, (cy - 8) * S)], fill=CREAM)
    # strisce del faro
    d.rectangle(((cx - 5) * S, (cy + 2) * S, (cx + 5) * S, (cy + 5) * S), fill=GOLD_HI)
    d.rectangle(((cx - 5) * S, (cy + 10) * S, (cx + 5) * S, (cy + 13) * S), fill=GOLD_HI)
    return down(im, n, n)


def spark(n=64):
    im = blank(n, n)
    d = ImageDraw.Draw(im)
    c = n // 2
    d.polygon([(c * S, 4 * S), ((c + 6) * S, c * S), (c * S, (n - 4) * S), ((c - 6) * S, c * S)], fill=CREAM)
    d.polygon([(4 * S, c * S), (c * S, (c - 5) * S), ((n - 4) * S, c * S), (c * S, (c + 5) * S)], fill=CREAM)
    d.ellipse(((c - 4) * S, (c - 4) * S, (c + 4) * S, (c + 4) * S), fill=(255, 236, 190, 255))
    return down(im, n, n)


def sun_face(n=128):
    im = blank(n, n)
    d = ImageDraw.Draw(im)
    cx = cy = n // 2
    # raggi
    for i in range(12):
        import math
        a = i * math.pi / 6
        x1 = cx + math.cos(a) * 28
        y1 = cy + math.sin(a) * 28
        x2 = cx + math.cos(a) * 58
        y2 = cy + math.sin(a) * 58
        d.line([(x1 * S, y1 * S), (x2 * S, y2 * S)], fill=SUN, width=6 * S)
    d.ellipse(((cx - 32) * S, (cy - 30) * S, (cx + 32) * S, (cy + 34) * S), fill=SHADOW)
    d.ellipse(((cx - 34) * S, (cy - 34) * S, (cx + 34) * S, (cy + 34) * S), fill=SUN)
    d.ellipse(((cx - 34) * S, (cy - 34) * S, (cx + 34) * S, (cy + 34) * S), outline=INK, width=3 * S)
    # faccina bruna
    d.ellipse(((cx - 14) * S, (cy - 10) * S, (cx - 6) * S, (cy - 1) * S), fill=INK)
    d.ellipse(((cx + 6) * S, (cy - 10) * S, (cx + 14) * S, (cy - 1) * S), fill=INK)
    d.arc(((cx - 14) * S, (cy - 2) * S, (cx + 14) * S, (cy + 18) * S), 20, 160, fill=INK, width=3 * S)
    return down(im, n, n)


def box_icon(n=128):
    im = blank(n, n)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((18 * S, 36 * S, 110 * S, 108 * S), radius=8 * S, fill=CARD_DK)
    d.rounded_rectangle((18 * S, 28 * S, 110 * S, 100 * S), radius=8 * S, fill=CARD)
    d.rounded_rectangle((18 * S, 28 * S, 110 * S, 100 * S), radius=8 * S, outline=INK, width=4 * S)
    d.rectangle((58 * S, 28 * S, 70 * S, 100 * S), fill=GOLD)
    d.rectangle((18 * S, 56 * S, 110 * S, 66 * S), fill=GOLD)
    d.rectangle((58 * S, 28 * S, 70 * S, 100 * S), outline=INK, width=2 * S)
    d.polygon([(18 * S, 28 * S), (64 * S, 8 * S), (110 * S, 28 * S)], fill=CARD_HI)
    d.line([(18 * S, 28 * S), (64 * S, 8 * S), (110 * S, 28 * S)], fill=INK, width=4 * S)
    return down(im, n, n)


def joy_ring(n=256):
    im = blank(n, n)
    d = ImageDraw.Draw(im)
    import math
    d.ellipse((10 * S, 16 * S, (n - 6) * S, (n - 2) * S), fill=SHADOW)
    d.ellipse((6 * S, 6 * S, (n - 6) * S, (n - 6) * S), fill=(243, 227, 195, 210))
    d.ellipse((6 * S, 6 * S, (n - 6) * S, (n - 6) * S), outline=INK, width=6 * S)
    d.ellipse((28 * S, 28 * S, (n - 28) * S, (n - 28) * S), outline=(59, 42, 32, 180), width=3 * S)
    cx = cy = n // 2
    for i in range(24):
        a = i * math.pi / 12
        inner = 78 if i % 2 == 0 else 88
        x1 = cx + math.cos(a) * inner
        y1 = cy + math.sin(a) * inner
        x2 = cx + math.cos(a) * 104
        y2 = cy + math.sin(a) * 104
        d.line([(x1 * S, y1 * S), (x2 * S, y2 * S)], fill=INK, width=(4 if i % 2 == 0 else 2) * S)
    # buco centrale trasparente (il pomello sta sopra)
    hole = Image.new("RGBA", im.size, (0, 0, 0, 0))
    ImageDraw.Draw(hole).ellipse((70 * S, 70 * S, (n - 70) * S, (n - 70) * S), fill=(255, 255, 255, 255))
    im = down(im, n, n)
    mask = hole.resize((n, n), Image.Resampling.LANCZOS).getchannel("A")
    # togli il centro
    r, g, b, a = im.split()
    from PIL import ImageChops
    a = ImageChops.subtract(a, mask)
    im = Image.merge("RGBA", (r, g, b, a))
    return im


def atlas(icons):
    """Griglia 256: moneta 128, scintilla 64, sole 128, scatola 128. File di riferimento."""
    im = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    im.paste(icons["coin"].resize((96, 96), Image.Resampling.LANCZOS), (0, 0))
    im.paste(icons["spark"].resize((48, 48), Image.Resampling.LANCZOS), (96, 0))
    im.paste(icons["sun"].resize((96, 96), Image.Resampling.LANCZOS), (0, 96))
    im.paste(icons["box"].resize((80, 80), Image.Resampling.LANCZOS), (112, 96))
    return im


def save(im, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path, "PNG")
    print("wrote", path, im.size)


META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {bl}, y: {bb}, z: {br}, w: {bt}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_meta(path, border):
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    bl = bb = br = bt = border
    text = META.format(guid=uuid.uuid4().hex, bl=bl, bb=bb, br=br, bt=bt)
    with open(meta, "w", newline="\n") as f:
        f.write(text)


def nine(src, dw, dh, b):
    sw, sh = src.size
    out = Image.new("RGBA", (dw, dh), (0, 0, 0, 0))
    def crop(x, y, w, h):
        return src.crop((x, y, x + w, y + h))
    # angoli
    out.paste(crop(0, 0, b, b), (0, 0))
    out.paste(crop(sw - b, 0, b, b), (dw - b, 0))
    out.paste(crop(0, sh - b, b, b), (0, dh - b))
    out.paste(crop(sw - b, sh - b, b, b), (dw - b, dh - b))
    # lati
    if dw - 2 * b > 0:
        top = crop(b, 0, sw - 2 * b, b).resize((dw - 2 * b, b), Image.Resampling.BILINEAR)
        bot = crop(b, sh - b, sw - 2 * b, b).resize((dw - 2 * b, b), Image.Resampling.BILINEAR)
        out.paste(top, (b, 0))
        out.paste(bot, (b, dh - b))
    if dh - 2 * b > 0:
        left = crop(0, b, b, sh - 2 * b).resize((b, dh - 2 * b), Image.Resampling.BILINEAR)
        right = crop(sw - b, b, b, sh - 2 * b).resize((b, dh - 2 * b), Image.Resampling.BILINEAR)
        out.paste(left, (0, b))
        out.paste(right, (dw - b, b))
    if dw - 2 * b > 0 and dh - 2 * b > 0:
        mid = crop(b, b, sw - 2 * b, sh - 2 * b).resize((dw - 2 * b, dh - 2 * b), Image.Resampling.BILINEAR)
        out.paste(mid, (b, b))
    return out


def build_textures():
    files = {}
    spec = {
        "tex_sign_walnut.png": (panel(160, 96, 18, WALNUT, WALNUT_HI, WALNUT_DK, grain=(90, 52, 32, 90)), 22),
        "tex_tag_paper.png": (panel(96, 128, 10, CREAM, CREAM_HI, (196, 170, 130, 255), grain=(180, 150, 110, 70)), 16),
        "tex_ticket_cardboard.png": (panel(192, 72, 14, CARD, CARD_HI, CARD_DK, grain=(150, 100, 60, 80)), 18),
        "tex_chip_cardboard.png": (panel(128, 64, 16, CARD, CARD_HI, CARD_DK), 16),
        "tex_plate_tuff.png": (panel(160, 80, 12, TUFF, TUFF_HI, TUFF_DK, grain=(160, 140, 110, 60)), 16),
        "tex_pad_terracotta.png": (panel(160, 128, 26, TERR, TERR_HI, TERR_DK), 28),
    }
    for name, (im, border) in spec.items():
        path = os.path.join(TEX, name)
        save(im, path)
        write_meta(path, border)
        files[name] = im
    icons = {
        "coin": coin(128),
        "spark": spark(64),
        "sun": sun_face(128),
        "box": box_icon(128),
    }
    icon_names = {
        "coin": "ic_coin_faro.png",
        "spark": "ic_spark.png",
        "sun": "ic_sun_face.png",
        "box": "ic_box_order.png",
    }
    for k, name in icon_names.items():
        path = os.path.join(ICONS, name)
        save(icons[k], path)
        write_meta(path, 0)
        files[name] = icons[k]
    joy = joy_ring(256)
    path = os.path.join(TEX, "tex_joy_ring.png")
    save(joy, path)
    write_meta(path, 0)
    files["tex_joy_ring.png"] = joy
    atl = atlas(icons)
    # L'atlante vive in Source~ (Unity non lo importa): le icone in gioco sono le celle.
    save(atl, os.path.join(os.path.dirname(__file__), "atlas_hud_c.png"))
    return files


def font(size, bold=True):
    name = "Baloo2-Bold.ttf" if bold else "Baloo2-SemiBold.ttf"
    path = os.path.join(FONTS, name)
    if not os.path.exists(path):
        path = "/tmp/fonts/" + name
    return ImageFont.truetype(path, size)


def text(d, xy, s, fnt, fill):
    d.text(xy, s, font=fnt, fill=fill)


def preview(files):
    os.makedirs(OUT_PREV, exist_ok=True)
    shots = [
        (390, 844, True, "hud_390x844.png"),
        (390, 844, False, "hud_390x844_icona.png"),
        (360, 800, False, "hud_360x800.png"),
        (360, 800, True, "hud_360x800_aperto.png"),
    ]
    for w, h, open_order, name in shots:
        im = render_hud(files, w, h, open_order)
        save(im, os.path.join(OUT_PREV, name))


def render_hud(files, w, h, open_order):
    im = Image.new("RGBA", (w, h), (244, 232, 208, 255))
    d = ImageDraw.Draw(im)
    # cordini e chiodi
    sign_l, sign_t, sign_r, sign_h = 14, 30, 92, 58
    sign_w = w - sign_l - sign_r
    d.line([(sign_l + 28, 8), (sign_l + 36, sign_t + 4)], fill=INK, width=3)
    d.line([(sign_l + sign_w - 28, 8), (sign_l + sign_w - 36, sign_t + 4)], fill=INK, width=3)
    d.ellipse((sign_l + 24, 4, sign_l + 32, 12), fill=(201, 162, 122, 255), outline=INK)
    d.ellipse((sign_l + sign_w - 32, 4, sign_l + sign_w - 24, 12), fill=(201, 162, 122, 255), outline=INK)
    sign = nine(files["tex_sign_walnut.png"], sign_w, sign_h, 22)
    im.alpha_composite(sign, (sign_l, sign_t))
    coin_i = files["ic_coin_faro.png"].resize((50, 50), Image.Resampling.LANCZOS)
    im.alpha_composite(coin_i, (sign_l + 4, sign_t + 4))
    sp = files["ic_spark.png"].resize((14, 14), Image.Resampling.LANCZOS)
    im.alpha_composite(sp, (sign_l + 6, sign_t + 2))
    # incasso: il numero e «LN» stanno uno accanto all'altro, mai sovrapposti
    f_num = font(26, True)
    f_ln = font(14, True)
    num = "1.240"
    nw = d.textlength(num, font=f_num)
    recess_l = sign_l + 58
    recess_r = sign_l + sign_w - 46
    d.rounded_rectangle((recess_l, sign_t + 12, recess_r, sign_t + 46), radius=8, fill=RECESS)
    d.text((recess_l + 8, sign_t + 14), num, font=f_num, fill=CREAM)
    d.text((recess_l + 8 + nw + 6, sign_t + 24), "LN", font=f_ln, fill=SUN)
    # ingranaggio semplice crema
    gx, gy = sign_l + sign_w - 36, sign_t + 16
    d.ellipse((gx, gy, gx + 26, gy + 26), outline=CREAM, width=3)
    d.ellipse((gx + 8, gy + 8, gx + 18, gy + 18), outline=CREAM, width=2)
    # cartellino giorno
    tag = nine(files["tex_tag_paper.png"], 66, 70, 16)
    tx, ty = w - 8 - 66, 24
    d.line([(tx + 33, 6), (tx + 33, ty + 6)], fill=INK, width=2)
    im.alpha_composite(tag, (tx, ty))
    f_k = font(11, False)
    f_day = font(28, True)
    d.text((tx + 10, ty + 14), "GIORNO", font=f_k, fill=INK)
    d.text((tx + 22, ty + 30), "3", font=f_day, fill=INK)
    # ordine
    if open_order:
        tw = w - 32
        ticket = nine(files["tex_ticket_cardboard.png"], tw, 36, 18)
        im.alpha_composite(ticket, (16, 86))
        d.ellipse((24, 96, 38, 110), fill=GOLD, outline=INK)
        f_ord = font(10, True)
        f_tx = font(14, False)
        d.text((44, 96), "ORDINE", font=f_ord, fill=INK)
        msg = "Porta 3 pacchi al Bancone"
        mx = 96
        max_w = w - 16 - 48 - mx
        # taglia il testo prima del contatore (niente ellissi disegnata: in gioco la fa UI Toolkit)
        while msg and d.textlength(msg + "…", font=f_tx) > max_w and len(msg) > 4:
            msg = msg[:-1]
        d.text((mx, 94), msg + ("…" if msg != "Porta 3 pacchi al Bancone" else ""), font=f_tx, fill=INK)
        d.rounded_rectangle((w - 16 - 44, 92, w - 22, 114), radius=10, fill=MINT, outline=INK, width=2)
        d.text((w - 54, 94), "0/3", font=font(13, True), fill=INK)
    else:
        chip = nine(files["tex_chip_cardboard.png"], 78, 31, 16)
        im.alpha_composite(chip, (16, 88))
        bx = files["ic_box_order.png"].resize((22, 22), Image.Resampling.LANCZOS)
        im.alpha_composite(bx, (22, 92))
        d.rounded_rectangle((48, 92, 86, 114), radius=10, fill=MINT, outline=INK, width=2)
        d.text((54, 93), "0/3", font=font(13, True), fill=INK)
    # joystick
    joy = files["tex_joy_ring.png"].resize((122, 122), Image.Resampling.LANCZOS)
    im.alpha_composite(joy, (24, h - 24 - 122))
    kx = 24 + 61 - 29
    ky = h - 24 - 122 + 61 - 29
    d.ellipse((kx, ky, kx + 58, ky + 58), fill=TERR, outline=INK, width=3)
    # pad
    px, py = w - 24 - 122, h - 24 - 135
    pad = nine(files["tex_pad_terracotta.png"], 122, 97, 28)
    im.alpha_composite(pad, (px, py))
    sun = files["ic_sun_face.png"].resize((52, 52), Image.Resampling.LANCZOS)
    im.alpha_composite(sun, (px + 35, py + 16))
    plate = nine(files["tex_plate_tuff.png"], 114, 47, 16)
    im.alpha_composite(plate, (px + 4, py + 88))
    title = "PRENDI"
    cargo = "CARICO 2/4"
    ft, fc = font(15, True), font(11, False)
    twid = d.textlength(title, font=ft)
    cwid = d.textlength(cargo, font=fc)
    d.text((px + 4 + (114 - twid) / 2, py + 90), title, font=ft, fill=INK)
    d.text((px + 4 + (114 - cwid) / 2, py + 112), cargo, font=fc, fill=INK)
    # combo di esempio, solo nella fascia centrale
    d.text((int(w * 0.62), int(h * 0.46)), "+12", font=font(22, True), fill=SUN,
           stroke_width=2, stroke_fill=INK)
    return im


if __name__ == "__main__":
    files = build_textures()
    preview(files)
