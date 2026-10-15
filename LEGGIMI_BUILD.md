# LEGGIMI — Build WebGL, test e link giocabile

> Issue: #12. Unity **6000.3.26f1** (URP). Ultimo aggiornamento: 2026-10-15 16:40 (Europe/Rome).

## 1. File della pipeline
| File | Cosa fa |
|---|---|
| `Assets/_Game/Scripts/Editor/WebGLBuild.cs` | Build WebGL in batchmode (`WebGLBuild.BuildCI`): compressione **disabilitata** (GitHub Pages non invia `Content-Encoding` per `.br/.gz`), `.nojekyll`, template a finestra intera, exit code 0/1. Voce menu *Ultimo Miglio → Build → WebGL (GitHub Pages)*. |
| `tools/build_webgl.sh` | Lancia la build, log in `Logs/webgl_build.log`, output in `Builds/WebGL`. |
| `tools/run_tests.sh` | Test EditMode/PlayMode, XML NUnit in `Logs/*-results.xml`, riepilogo in `Logs/test-summary.txt`. |
| `tools/pr_comment.sh` | Legge XML + log della build e scrive/aggiorna **un solo** commento di esito sulla PR (`gh`). |
| `tools/deploy_pages.sh` | Copia la build (file sciolti, niente zip) sul repo `B00RR/ultimo-miglio-web`. |
| `.github/workflows/unity-ci.yml` | CI con game-ci (test + build + commento). **Si attiva solo con la licenza Unity nei secret**, altrimenti viene saltata con un avviso. |

`WebGLMinBuild.Build` (esistente) resta invariato; `WebGLBuild` è la versione parametrica per CI.

## 2. Uso sulla box (flusso locale, senza licenza in CI)
Non lanciare due istanze Unity sullo stesso progetto né due build insieme (RAM: swap 8 GB).
```bash
export UNITY=/home/box/Unity/Hub/Editor/6000.3.26f1/Editor/Unity
tools/run_tests.sh all          # exit 0 = tutti verdi
tools/build_webgl.sh            # Builds/WebGL
tools/pr_comment.sh <N_PR> https://b00rr.github.io/ultimo-miglio-web/
tools/deploy_pages.sh           # pubblica il link giocabile
```
Variabili: `UM_DEV=1` (build development), `UM_SKIP_SCENE=1` (non rigenera la scena cap. 1).

Exit code: `0` ok · `1` test/build falliti · `2` configurazione (Unity mancante, istanza già aperta, build compressa).

## 3. GitHub Actions (game-ci)
Servono i secret del repo: `UNITY_LICENSE` (licenza Personal, file `.ulf`) **oppure** `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL` (Pro). Attenzione: gli asset sono in **Git LFS** → la CI consuma banda LFS; i runner standard hanno 7 GB di RAM, una build IL2CPP WebGL può essere lenta (20–40 min con cache vuota).

## 4. Proposta link giocabile (GitHub Pages)
- Repo dedicato **`B00RR/ultimo-miglio-web`** (pubblico, già creato con segnaposto, Pages attivo su `main` /root).
- URL: **https://b00rr.github.io/ultimo-miglio-web/**
- Perché un repo separato: il repo di gioco resta privato/pulito, niente binari pesanti nella storia del codice, deploy indipendente.
- Deploy: file WebGL **sciolti** (niente zip) + `.nojekyll` + `VERSIONE.txt` con il commit sorgente.
- Limiti Pages: sito ≤ 1 GB, file ≤ 100 MB, niente header personalizzati → per questo la compressione è disabilitata (in alternativa *Decompression Fallback*, più lento all'avvio).
- Passi: 1) `tools/run_tests.sh` verde → 2) `tools/build_webgl.sh` → 3) `tools/deploy_pages.sh` → 4) aprire l'URL (1–2 min) → 5) incollare il link nella PR/messaggio a Lorenzo.

## 5. Da provare con il Tech Lead (dopo le 18:00)
- [ ] Prima compilazione di `WebGLBuild.cs` (non ancora compilato con Unity).
- [ ] `tools/run_tests.sh all` → XML presenti, EditMode + PlayMode 4/4.
- [ ] `tools/build_webgl.sh` → `Builds/WebGL/index.html` senza file `.br/.gz`.
- [ ] `tools/deploy_pages.sh` e verifica nel browser (desktop + telefono).
- [ ] `tools/pr_comment.sh` sulla PR della pipeline.
- [ ] Decidere se attivare la CI (licenza nei secret).
