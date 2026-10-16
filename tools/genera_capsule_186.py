#!/usr/bin/env python3
"""Sprite #186 con l'ombra già nel PNG. Niente elemento ombra in UI Toolkit:
   un figlio si disegna sopra il fondo. Angoli esterni alpha 0.
   Non tocca i 9-slice della #185 (tex_cardboard_panel / tex_wood_panel)."""
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/_Game/UI/Textures"

# guid fissi: rieseguire lo script non cambia i riferimenti Unity
SPRITES = (
    # nome, larghezza, altezza, (top, corpo, fondo), guid
    ("tex_capsula_noce_32.png", 128, 32, ((236, 206, 156), (122, 74, 46), (62, 36, 22)), "a1c0320018618632a1c0320018618632"),
    ("tex_capsula_noce_40.png", 160, 40, ((236, 206, 156), (122, 74, 46), (62, 36, 22)), "a1c0400018618632a1c0400018618632"),
    ("tex_capsula_noce_48.png", 192, 48, ((236, 206, 156), (122, 74, 46), (62, 36, 22)), "a1c0480018618632a1c0480018618632"),
    ("tex_capsula_terracotta_32.png", 128, 32, ((244, 186, 140), (200, 99, 59), (110, 42, 22)), "a1c0c86318618632a1c0c86318618632"),
)
GOLD = (242, 181, 58)  # #F2B53A
SHADOW = 4
SHADOW_RGB = (158, 120, 115)  # #9E7873
SHADOW_A = 0.6
CREAM = (243, 227, 195)  # #F3E3C3
INK = (59, 42, 32)  # #3B2A20
# fumetto: corpo 48, raggio 20 (resta una fascia dritta da stirare), ombra nei 4 px in basso
BUBBLE = ("tex_fumetto_crema.png", 80, 48, 20, "a1c0f3e318632186a1c0f3e318632186")


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def cover(x, y, w, h, r):
    """Copertura 0..1 di un rettangolo stondato. r = h/2 è una capsula."""
    cx = min(max(x, r), w - r)
    cy = min(max(y, r), h - r)
    d = ((x - cx) ** 2 + (y - cy) ** 2) ** 0.5
    return max(0.0, min(1.0, r - d + 0.6))


def paint_body(w, h, top, body, bottom, radius=None, cream=False):
    r = h / 2 if radius is None else radius
    buf = bytearray(w * h * 4)
    for y in range(h):
        t = y / max(1, h - 1)
        if cream:
            col = CREAM
        elif t < 0.28:
            col = lerp(top, body, t / 0.28)
        else:
            col = lerp(body, bottom, (t - 0.28) / 0.72)
        for x in range(w):
            a = cover(x + 0.5, y + 0.5, w, h, r)
            if a <= 0:
                continue
            rgb = col
            if not cream:
                n = ((x * 13 + y * 29) ^ (x * 7)) & 7
                rgb = tuple(max(0, min(244, col[i] + (n - 3))) for i in range(3))
                # bordo del rettangolo stondato, non del solo semicerchio
                cx = min(max(x + 0.5, r), w - r)
                cy = min(max(y + 0.5, r), h - r)
                edge = r - (((x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2) ** 0.5)
                if 1.1 < edge < 2.6 and y > h * 0.55:
                    rgb = lerp(rgb, GOLD, 0.9)
            else:
                cx = min(max(x + 0.5, r), w - r)
                cy = min(max(y + 0.5, r), h - r)
                edge = r - (((x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2) ** 0.5)
                if 0 < edge < 2.2:
                    rgb = INK
            i = (y * w + x) * 4
            buf[i:i + 4] = bytes((rgb[0], rgb[1], rgb[2], int(round(a * 255))))
    return buf


def with_shadow(body, w, h):
    """Corpo in alto, stessa sagoma 4 px più in basso, #9E7873 al 60%. Il corpo copre l'ombra."""
    out_h = h + SHADOW
    out = bytearray(w * out_h * 4)
    for y in range(h):
        row = body[y * w * 4:(y + 1) * w * 4]
        sy = y + SHADOW
        for x in range(w):
            a = row[x * 4 + 3]
            if a == 0:
                continue
            i = (sy * w + x) * 4
            out[i:i + 4] = bytes((SHADOW_RGB[0], SHADOW_RGB[1], SHADOW_RGB[2], int(round(a * SHADOW_A))))
    for y in range(h):
        for x in range(w):
            s = (y * w + x) * 4
            ba = body[s + 3]
            if ba == 0:
                continue
            i = s  # il corpo sta a y invariato
            if ba == 255:
                out[i:i + 4] = body[s:s + 4]
                continue
            da = out[i + 3]
            inv = 255 - ba
            out[i] = (body[s] * ba + out[i] * inv) // 255
            out[i + 1] = (body[s + 1] * ba + out[i + 1] * inv) // 255
            out[i + 2] = (body[s + 2] * ba + out[i + 2] * inv) // 255
            out[i + 3] = ba + (da * inv) // 255
    return bytes(out), out_h


def png(w, h, rgba):
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    raw = b"".join(b"\x00" + rgba[y * w * 4:(y + 1) * w * 4] for y in range(h))
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")


def meta(name, w, h, guid, left, bottom, right, top):
    stem = name[:-4]
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: -186186186186186186
    second: {stem}_0
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
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {left}, y: {bottom}, z: {right}, w: {top}}}
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
    buildTarget: Standalone
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
    textureFormat: 4
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: {stem}_0
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: {w}
        height: {h}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: {left}, y: {bottom}, z: {right}, w: {top}}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {guid}
      internalID: -186186186186186186
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write_sprite(name, w, body_h, guid, rgba, img_h, left, bottom, right, top):
    (OUT / name).write_bytes(png(w, img_h, rgba))
    (OUT / (name + ".meta")).write_text(meta(name, w, img_h, guid, left, bottom, right, top), encoding="utf-8")
    print(name, w, img_h, "slice", left, bottom, right, top)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, w, h, cols, guid in SPRITES:
        body = paint_body(w, h, *cols)
        rgba, img_h = with_shadow(body, w, h)
        r = h // 2
        write_sprite(name, w, h, guid, rgba, img_h, r, r + SHADOW, r, r)
    bname, bw, bh, br, bguid = BUBBLE
    body = paint_body(bw, bh, CREAM, CREAM, CREAM, radius=br, cream=True)
    rgba, img_h = with_shadow(body, bw, bh)
    write_sprite(bname, bw, bh, bguid, rgba, img_h, br, br + SHADOW, br, br)


if __name__ == "__main__":
    main()
