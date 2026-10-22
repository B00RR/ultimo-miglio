> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# Analisi caricamento WebGL mobile – Bokobay
Data: 16/10/2026 · Build analizzata: `WebGL_min` (copia locale `/workspace/um-recovery/build/WebGL_p2`, pubblicata su https://b00rr.github.io/ultimo-miglio-web/) · Unity 6000.3.26f1 URP, compressione Disabled.
Script e dati grezzi in questa cartella: `load.js`/`load.json` (Playwright), `c.js`/`r.json` (compressione + decompressione JS).

## 1. Dimensioni e gzip di GitHub Pages
| File | Grezzo | Pages gzip (trasferito) | gzip -9 locale | brotli q11 locale |
|---|---:|---:|---:|---:|
| WebGL_min.wasm | 34,84 MB | 10,79 MB | 10,61 MB | **7,64 MB** |
| WebGL_min.data | 21,75 MB | 13,67 MB | 13,61 MB | **12,27 MB** |
| WebGL_min.framework.js | 461 KB | 92 KB | 91 KB | 76 KB |
| WebGL_min.loader.js | 19 KB | 7 KB | – | – |
| **Totale** | **57,07 MB** | **24,56 MB** | 24,31 MB | **19,99 MB** |

- Pages risponde `Content-Encoding: gzip` se la richiesta ha `Accept-Encoding: gzip`; con `br` soltanto risponde **non compresso** (niente Brotli lato server). Il `.wasm` è servito come `application/wasm`, quindi la compilazione in streaming funziona.
- Pages non serve `.br`/`.gz` precompressi con Content-Encoding: Gzip/Brotli di Unity richiedono il **Decompression Fallback** (file `.unityweb`, decompressione in JS).
- Il `.data` si comprime poco (63 %): con buona probabilità contiene già texture/audio compressi o mesh pesanti. Il `.wasm` da 34,8 MB grezzi è molto grande per una vertical slice: c'è spazio per lo stripping.

## 2. Tempi misurati (Playwright, 390x844 touch, DPR 3, CPU 4x, cache disattivata, 3 ripetizioni)
Tempo misurato da `goto` alla rimozione di `#l` (cioè alla risoluzione di `createUnityInstance`).
| Profilo | Rip. 1 | Rip. 2 | Rip. 3 | Media | Byte di rete |
|---|---:|---:|---:|---:|---:|
| 4G (9 Mbps, 170 ms) | 26,5 s | 25,6 s | 26,1 s | **26,1 s** | 24,57 MB |
| 3G veloce (1,6 Mbps, 150 ms) | 126,3 s | 126,2 s | 126,6 s | **126,4 s** | 24,57 MB |

Il loader sparisce ~20-60 ms dopo la fine dell'ultimo download: oggi il caricamento è **limitato al 100 % dalla rete** (la compilazione wasm in streaming si sovrappone al download). Costo osservato: ~1,06 s/MB su 4G e ~5,14 s/MB su 3G veloce.
Nota: rendering via SwiftShader in headless, quindi il primo frame dopo il loader non è rappresentativo; il dato rilevante è il tempo fino al loader.

## 3. Stima comparativa
Decompressione JS (decoder pako/brotli-JS, simili a quelli del fallback Unity) misurata sulla box: gzip 0,88 s, brotli 2,31 s → con CPU mobile ~4x: **gzip ≈ 3,5 s, brotli ≈ 9,3 s**. Con il fallback si perde anche la compilazione wasm in streaming (lieve costo aggiuntivo non incluso).

| Variante | Trasferito | 4G stimato | 3G veloce stimato |
|---|---:|---:|---:|
| Disabled + gzip di Pages (attuale, misurato) | 24,6 MB | **26,1 s** | 126,4 s |
| Gzip + Decompression Fallback | 24,3 MB | ~29 s (25,8 + 3,5) | ~128 s |
| Brotli + Decompression Fallback | 20,0 MB | ~31 s (21,2 + 9,3) | **~112 s** |
| (rif.) Brotli con Content-Encoding vero (es. Cloudflare/Netlify) | 20,0 MB | ~21 s | ~103 s |

Conclusione: su Pages **Gzip+fallback non conviene mai**; **Brotli+fallback** conviene solo su reti lente (3G: −14 s) e peggiora il 4G (+5 s). Il guadagno vero non viene dal formato di compressione ma dalla **riduzione del contenuto** (wasm e data) e dalla cache per le visite successive.

## 4. Raccomandazioni Player Settings
- **Compression Format**: tenere `Disabled` su GitHub Pages (Pages fa gzip al volo, streaming wasm preservato). Brotli + fallback solo se il target principale è 3G/rete scarsa. Se si passa a un host con Content-Encoding corretto: Brotli **senza** fallback.
- **Decompression Fallback**: off con Disabled; obbligatorio se Gzip/Brotli su Pages.
- **Code Optimization** (`UserBuildSettings.codeOptimization`): release `DiskSizeLTO` (wasm più piccolo); QA `BuildTimes` (Shorter Build Time).
- **IL2CPP Code Generation**: `OptimizeSize`; Compiler Configuration `Master` per release, `Release` per QA.
- **Strip Engine Code**: on (già 1 nel ProjectSettings).
- **Managed Stripping Level**: oggi non impostato (`managedStrippingLevel: {}` = default Minimal) → release `High` (con `link.xml` per tipi usati via reflection, es. `HudSetup` è solo editor quindi ok), QA `Minimal`.
- **Exception Support**: release `None` (riduce il wasm), QA `ExplicitlyThrownExceptionsOnly`.
- **Texture compression**: WebGL mobile → `ASTC` (Build Settings › WebGL › Texture Compression, oppure `EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC`); fallback ETC2 per Android vecchi. In Unity 6 si possono generare build multiple (DXT desktop + ASTC mobile) e scegliere nell'index in base a `WEBGL_compressed_texture_astc`. Ridurre Max Size (1024/512) per le texture del diorama e audio in Vorbis/mono a bassa qualità.
- **Data Caching**: **on** (IndexedDB): le visite successive scaricano 0 byte di `.data`. Con file non hashati serve `webGLNameFilesAsHashes = 1` per invalidare correttamente.
- **Name Files As Hashes**: on in release (cache busting; `pubblica.sh` già gestisce nomi arbitrari tramite `*.loader.js`).

## Modifiche proposte agli script (non applicate)
**WebGLBuild.cs / WebGLMinBuild.cs**: estrarre un metodo `ApplyProfile(bool release)` condiviso e un argomento `-profile qa|release`:
```csharp
static void ApplyProfile(bool release, bool brotliFallback)
{
    var nbt = NamedBuildTarget.WebGL;
    PlayerSettings.WebGL.compressionFormat = brotliFallback ? WebGLCompressionFormat.Brotli : WebGLCompressionFormat.Disabled;
    PlayerSettings.WebGL.decompressionFallback = brotliFallback;
    PlayerSettings.WebGL.dataCaching = true;
    PlayerSettings.WebGL.nameFilesAsHashes = release;
    PlayerSettings.stripEngineCode = true;
    PlayerSettings.SetManagedStrippingLevel(nbt, release ? ManagedStrippingLevel.High : ManagedStrippingLevel.Minimal);
    PlayerSettings.SetIl2CppCodeGeneration(nbt, Il2CppCodeGeneration.OptimizeSize);
    PlayerSettings.SetIl2CppCompilerConfiguration(nbt, release ? Il2CppCompilerConfiguration.Master : Il2CppCompilerConfiguration.Release);
    PlayerSettings.WebGL.exceptionSupport = release ? WebGLExceptionSupport.None : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
    UnityEditor.WebGL.UserBuildSettings.codeOptimization = release ? UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO : UnityEditor.WebGL.WasmCodeOptimization.BuildTimes;
    EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
}
```
- `WebGLBuild.cs` oggi forza `dataCaching=false` e non imposta stripping/codeOptimization: usare `ApplyProfile`. Valutare `BuildOptions.CleanBuildCache` solo in release.
- `WebGLMinBuild.cs`: chiama `EditorApplication.Exit` sempre (anche da menu) e duplica la logica; farlo delegare a `WebGLBuild.Build` col profilo QA. Il `try{}catch{}` vuoto sul codeOptimization nasconde errori: loggare.
- Verificare che gli script ripristinino le PlayerSettings modificate (oggi le salvano in ProjectSettings → diff indesiderati nel repo).

**pubblica.sh**:
- Già corretto il rifiuto di `.br/.gz`. Aggiungere: avviso se si trovano `.unityweb` (fallback attivo) con stampa dei byte; misura post-deploy con `curl -H 'Accept-Encoding: gzip' -w '%{size_download}'` per file e totale (regressioni di peso); controllo che `index.html` non sia servito da cache (già via `?v=SHA`).
- Con `nameFilesAsHashes` i nomi cambiano: lo script li ricava già da `pick`, ma `rm -rf Build` va mantenuto per non accumulare file vecchi.

## Profili proposti
| | QA veloce (tempo di build minimo) | Release (download minimo) |
|---|---|---|
| Compression | Disabled (gzip di Pages) | Disabled su Pages; Brotli+fallback se target 3G; Brotli puro su host con Content-Encoding |
| Code Optimization | Shorter Build Time (`BuildTimes`) | Disk Size with LTO |
| IL2CPP config | Release, OptimizeSize | Master, OptimizeSize |
| Managed stripping | Minimal | High (+ link.xml) |
| Strip engine code | on | on |
| Exceptions | Explicitly thrown | None |
| Texture | ASTC (niente rebuild doppio) | ASTC (+ eventuale build DXT desktop) , max size ridotta |
| Data caching / hash | on / off | on / on |
| Development | opzionale `-development` | off |

Prossimo passo suggerito: una build release con questo profilo e rimisura con `load.js` (stesso protocollo) per quantificare il calo di wasm/data.
