#!/usr/bin/env python3
"""Sprite capsula #186. Raggio = metà altezza, angoli trasparenti, luce in alto.
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


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def capsule_dist(x, y, w, h):
    r = h / 2
    cx = min(max(x, r), w - r)
    return ((x - cx) ** 2 + (y - r) ** 2) ** 0.5


def paint(w, h, top, body, bottom):
    r = h / 2
    buf = bytearray(w * h * 4)
    for y in range(h):
        t = y / (h - 1)
        if t < 0.28:
            col = lerp(top, body, t / 0.28)
        else:
            col = lerp(body, bottom, (t - 0.28) / 0.72)
        for x in range(w):
            d = capsule_dist(x + 0.5, y + 0.5, w, h)
            a = max(0.0, min(1.0, r - d + 0.6))
            if a <= 0:
                continue
            # venatura calda, niente bianco
            n = ((x * 13 + y * 29) ^ (x * 7)) & 7
            rgb = tuple(max(0, min(244, col[i] + (n - 3))) for i in range(3))
            edge = r - d
            if 1.1 < edge < 2.6 and y > h * 0.55:
                rgb = lerp(rgb, GOLD, 0.9)
            i = (y * w + x) * 4
            buf[i:i + 4] = bytes((rgb[0], rgb[1], rgb[2], int(a * 255)))
    return bytes(buf)


def png(w, h, rgba):
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    raw = b"".join(b"\x00" + rgba[y * w * 4:(y + 1) * w * 4] for y in range(h))
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")


def meta(name, w, h, guid):
    r = h // 2
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
  spriteBorder: {{x: {r}, y: {r}, z: {r}, w: {r}}}
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
      border: {{x: {r}, y: {r}, z: {r}, w: {r}}}
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


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, w, h, cols, guid in SPRITES:
        rgba = paint(w, h, *cols)
        (OUT / name).write_bytes(png(w, h, rgba))
        (OUT / (name + ".meta")).write_text(meta(name, w, h, guid), encoding="utf-8")
        print(name, w, h, "raggio", h // 2)


if __name__ == "__main__":
    main()
