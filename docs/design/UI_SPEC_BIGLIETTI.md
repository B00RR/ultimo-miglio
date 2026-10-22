> **Nota:** documento originale recuperato dal backup del 16/10/2026, con i nomi aggiornati a quelli attuali (Bokobay, Kaia Rio, Sunn Depot, GrayBox…). Può differire da quanto implementato oggi nel gioco.

# UI_SPEC_BIGLIETTI - Biglietti di Oma Tilde, HUD senza sovrapposizioni, API game feel
Autore: UI/UX · 15/10/2026 16:15 · per il Tech Lead (ramo `webgl-min`, HUD v2 con `Tokens.uss` / `HudV2.uss`).
Regola: nessuna modifica in `Assets/_Game/UI` da parte della UI finché il TL non manda la sua versione. Questo file si applica **sul ramo del TL**.
Codice di questo documento: compilato con .NET 8 contro le DLL UnityEngine 6000.3.26f1 (nessun errore), non ancora eseguito in Unity.

## 0. Fonti verificate
- Testi definitivi: `data/ch1/onboarding_testi.json` (7 passi + `fine`, voce Oma Tilde "dai suoi biglietti", max 70 caratteri; il più lungo ne ha 58).
- Passi/trigger: `data/ch1/onboarding.json`, `onboarding.md`, 04_UI §4 (fallback: 5 s fermi → mano fantasma + freccia).
- Codice TL: `Scripts/Gameplay/TutorialManager.cs` (passi Muoviti→Nonno→Fine, banner `.ada-banner` aggiunto a `hud.Root`, `EndSeconds = 6`), `UI/USS/HudV2.uss` (oggi: banner **in basso al centro**, `max-width 64%`), `UI/Tokens.uss`.
- Problema attuale: il banner in basso al centro sta **dentro l'area del pollice/joystick** e, con il pulsante telefono ora in basso a destra (`HudV2`), a 960x600 i due si avvicinano. Il nome `--c-terracotta` su `--c-carta` ha contrasto **3,69:1** (sotto 4,5:1 per testo piccolo).

## 1. Biglietti di Oma Tilde
### 1.1 Contenuto
- **Una sola nota alla volta**; le altre in **coda FIFO**. Il testo del passo corrente sostituisce la coda (il passo vince sulle note a tempo).
- Testo: **max 90 caratteri, max 2 righe** a ogni risoluzione (i testi attuali stanno in 1-2 righe). Oltre 90 il componente tronca con "…" e logga un warning: il testo si corregge nei dati, non nel codice.
- Struttura: tondo "TILDE" (o ritratto) a sinistra · riga 1 `Oma Tilde` (titolo) · riga 2 il testo. Niente pulsanti, niente X: la nota **non intercetta input** (`PickingMode.Ignore`), quindi non blocca mai il joystick.

### 1.2 Tipografia e contrasto
| Elemento | Token | Minimo reale | Colore su `--c-carta` (#FFF4D6) | Contrasto |
|---|---|---|---|---|
| Testo | `--fs-body` (32 px) | **≥ 16 dp** | `--c-ink` #3B2F2A | **11,8:1** ✅ |
| Nome "Oma Tilde" | `--fs-label` (26 px) | **≥ 20 dp** | `--c-terracotta-dk` #873A1E (non `--c-terracotta`) | **7,2:1** ✅ (terracotta: 3,69 ❌) |
| Tondo "TILDE" | `--fs-label` | grande, bold | `--c-carta` su `--c-terracotta` | 3,69:1 (ok solo come testo grande bold ≥ 18,7 px reali) |
Regola dp: la scala del pannello (`ResponsivePanel.Scale`) a 960x600 vale 0,5, quindi 32 px di riferimento = 16 px reali, al limite. Il componente forza `fontSize = max(32, 16/scale)` per il corpo e `max(26, 20/scale)` per il titolo, così a scala minima (0,42) il testo resta a 16/20 dp.

### 1.3 Posizione (mai negli angoli HUD, mai nell'area joystick)
- **Banda medio-alta**, sotto la safe area. Tutta la metà bassa dello schermo è del pollice (il joystick nasce dove si tocca): la nota non scende mai sotto il 40% dell'altezza.
- **Portrait** (telefono, 1080x1920 / 1170x2532): `top = max(safe top, fondo delle pillole valute) + 16`, `left/right 32`, larghezza piena.
- **WebGL landscape** (1920x1080, 1366x768, 960x600): in alto al centro, **fra** il bordo destro delle pillole valute e il pulsante telefono, se questo sta in alto (con `HudV2` sta in basso: in quel caso il lato destro libero è simmetrico al sinistro). `top = safe top + 16`, `max-width = min(spazio fra gli angoli, 900 px)`. Il testo va a capo entro 2 righe.
- Ordine di priorità se lo spazio non basta: 1) la nota va a capo (2 righe), 2) si nasconde il toast "La Cassa ha X LN" mentre la nota è visibile, 3) la mini-etichetta stanza scende sotto la nota (`top-center` + altezza nota + 8).
- La freccia a bordo schermo e il marker a terra restano il segnale spaziale; la nota è solo testo.

### 1.4 Tempi e chiusura
| Evento | Regola |
|---|---|
| Nuovo passo | la nota entra con slide-down 8 px + fade 200 ms; resta **finché il passo non è completato** (sticky) |
| Passo completato | resta **almeno 1,5 s** da quando è apparsa, poi fade 250 ms, poi la nota successiva |
| Passi completati di fila (es. Arrivi istantaneo) | la nota nuova va in coda e parte dopo il minimo di 1,5 s: nessuna nota lampeggia |
| 5 s fermi sullo stesso passo | `Nudge()`: pulse 1,04 per 220 ms + la mano fantasma esistente; il testo non cambia |
| Fine tutorial | nota `fine` per 6 s (`EndSeconds`), poi sparisce; niente modale |
| Telefono aperto | la nota resta (è nel pannello HUD, sotto il telefono): nessuna pausa |
| Salta tutorial (dopo il passo combo, 04_UI §4) | svuota la coda e chiude subito |

### 1.5 Componente pronto da incollare
`Assets/_Game/UI/Runtime/AdaNoteView.cs` (namespace `UltimoMiglio.UI`, solo UnityEngine/UIElements):
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Biglietti di Nonna Ada: UNO alla volta, coda FIFO, banda medio-alta sotto la safe area e sotto le pillole valute,
    /// mai negli angoli HUD né nella metà bassa (joystick). Non intercetta input (PickingMode.Ignore).
    /// Uso: AdaNoteView.Attach(hudRoot); view.Show(text, sticky:true) per i passi; view.Enqueue(text, 4f) per note a tempo.
    /// </summary>
    public sealed class AdaNoteView
    {
        public const int MaxChars = 90;
        public const float MinVisibleSec = 1.5f, FadeSec = 0.25f, MinBodyDp = 16f, MinTitleDp = 20f;

        struct Item { public string text; public float seconds; public bool sticky; }
        readonly Queue<Item> queue = new Queue<Item>();
        readonly VisualElement root, band, note; readonly Label who, body;
        Item current; bool hasCurrent; float shownAt, hideAt = -1;

        public static AdaNoteView Attach(VisualElement hudRoot, string speaker = "Nonna Ada") => new AdaNoteView(hudRoot, speaker);

        AdaNoteView(VisualElement hudRoot, string speaker)
        {
            root = hudRoot;
            band = new VisualElement { name = "ada-band", pickingMode = PickingMode.Ignore }; band.AddToClassList("ada-band");
            note = new VisualElement { name = "ada-note", pickingMode = PickingMode.Ignore }; note.AddToClassList("ada-note");
            var face = new Label("ADA") { pickingMode = PickingMode.Ignore }; face.AddToClassList("ada-face");
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.AddToClassList("ada-col");
            who = new Label(speaker) { pickingMode = PickingMode.Ignore }; who.AddToClassList("ada-name");
            body = new Label { pickingMode = PickingMode.Ignore }; body.AddToClassList("ada-text");
            col.Add(who); col.Add(body); note.Add(face); note.Add(col); band.Add(note);
            band.style.display = DisplayStyle.None;
            root.Add(band);
        }

        /// <summary>Nota del passo corrente: resta finché non chiami Complete() (min 1,5 s a schermo).</summary>
        public void Show(string text, bool sticky = true, float seconds = 0f)
        {
            queue.Clear();
            if (hasCurrent && Time.unscaledTime - shownAt < MinVisibleSec) { queue.Enqueue(new Item { text = text, sticky = sticky, seconds = seconds }); hideAt = shownAt + MinVisibleSec; return; }
            Present(new Item { text = text, sticky = sticky, seconds = seconds });
        }

        /// <summary>Nota a tempo in coda (es. fine tutorial 6 s, suggerimenti).</summary>
        public void Enqueue(string text, float seconds) { queue.Enqueue(new Item { text = text, seconds = seconds }); if (!hasCurrent) Next(); }

        /// <summary>Passo completato: chiude la nota sticky rispettando il minimo di 1,5 s.</summary>
        public void Complete() { if (!hasCurrent) return; hideAt = Mathf.Max(Time.unscaledTime, shownAt + MinVisibleSec); }

        /// <summary>5 s fermi sul passo: la nota "bussa" (pulse) senza cambiare testo.</summary>
        public void Nudge() { note.AddToClassList("ada-note--nudge"); note.schedule.Execute(() => note.RemoveFromClassList("ada-note--nudge")).StartingIn(220); }

        /// <summary>Chiamare ogni frame (es. da TutorialManager.Update) con la scala del pannello (ResponsivePanel.Scale).</summary>
        public void Tick(float panelScale)
        {
            Layout(panelScale);
            if (hasCurrent && !current.sticky && current.seconds > 0 && hideAt < 0) hideAt = shownAt + Mathf.Max(MinVisibleSec, current.seconds);
            if (hideAt >= 0 && Time.unscaledTime >= hideAt) { hideAt = -1; FadeOut(); }
        }

        void Present(Item it)
        {
            string t = it.text ?? "";
            if (t.Length > MaxChars) { Debug.LogWarning($"[AdaNote] {t.Length} caratteri > {MaxChars}: tagliato. Accorciare il testo nei dati."); t = t.Substring(0, MaxChars - 1) + "…"; }
            current = it; hasCurrent = true; shownAt = Time.unscaledTime; hideAt = -1;
            body.text = t;
            band.style.display = DisplayStyle.Flex;
            note.RemoveFromClassList("ada-note--out"); note.AddToClassList("ada-note--in");
            note.schedule.Execute(() => note.RemoveFromClassList("ada-note--in")).StartingIn(16); // USS transition: from in → rest
        }

        void FadeOut()
        {
            note.AddToClassList("ada-note--out");
            note.schedule.Execute(() => { hasCurrent = false; band.style.display = DisplayStyle.None; Next(); }).StartingIn((long)(FadeSec * 1000));
        }

        void Next() { if (queue.Count > 0) Present(queue.Dequeue()); }

        /// <summary>Banda: top = max(fondo safe area, fondo delle pillole valute) + 16 px; larghezza fra gli angoli HUD.
        /// Font: corpo ≥ 16 dp, titolo ≥ 20 dp anche a scala minima (WebGL 960x600).</summary>
        void Layout(float scale)
        {
            float W = root.layout.width; if (float.IsNaN(W) || W <= 0) return;
            bool landscape = root.ClassListContains("um-landscape");
            var pill = root.Q("currency-pill"); var phone = root.Q("phone-button"); var safe = root.Q("safe-root");
            float safeTop = safe != null ? safe.resolvedStyle.paddingTop : 0f;
            float pillBottom = pill != null && pill.resolvedStyle.display != DisplayStyle.None ? pill.worldBound.yMax - root.worldBound.yMin : safeTop;
            float pillRight = pill != null ? pill.worldBound.xMax - root.worldBound.xMin : 0f;
            float phoneLeft = W; if (phone != null && phone.worldBound.yMin - root.worldBound.yMin < root.layout.height * 0.5f) phoneLeft = phone.worldBound.xMin - root.worldBound.xMin;
            const float gap = 16f;
            if (landscape)
            {   // fra le pillole (sx) e il pulsante telefono se in alto (dx), centrato, in alto
                float side = Mathf.Max(pillRight, W - phoneLeft) + gap;
                band.style.top = safeTop + gap; band.style.left = side; band.style.right = side;
                note.style.maxWidth = Mathf.Min(W - 2 * side, 900f);
            }
            else
            {   // portrait: sotto le pillole valute, quasi tutta la larghezza
                band.style.top = Mathf.Max(safeTop, pillBottom) + gap; band.style.left = 32f; band.style.right = 32f;
                note.style.maxWidth = Length.Percent(100);
            }
            float s = Mathf.Max(0.01f, scale);
            body.style.fontSize = Mathf.Max(32f, MinBodyDp / s);   // 32 = --fs-body
            who.style.fontSize = Mathf.Max(26f, MinTitleDp / s);   // titolo/speaker ≥ 20 dp
        }
    }
}
```
**Cablaggio in `TutorialManager` (3 righe, al posto di `EnsureBanner` e del blocco `_bannerText`):**
```csharp
// campo
UltimoMiglio.UI.AdaNoteView _note; UltimoMiglio.UI.ResponsivePanel _rp; float _idle;
// Update(), dopo GameRoot.Ready:
if (_note == null && _hud && _hud.Root != null) { _note = UltimoMiglio.UI.AdaNoteView.Attach(_hud.Root, Speaker); _rp = _hud.GetComponent<UltimoMiglio.UI.ResponsivePanel>(); if (CurrentText != null) _note.Show(CurrentText, Current < Step.Fine, Current == Step.Fine ? EndSeconds : 0); }
_note?.Tick(_rp ? _rp.Scale : 1f);
// in Enter(s), al posto del testo del banner:
_note?.Complete(); if (CurrentText != null) _note?.Show(CurrentText, s < Step.Fine, s == Step.Fine ? EndSeconds : 0);
// idle: se Nina è ferma da 5 s sullo stesso passo → _note?.Nudge();
```

### 1.6 USS (variabili di `Tokens.uss`), da mettere in `HudV2.uss` al posto di `.ada-banner*`
```css
/* Biglietto di Nonna Ada: banda medio-alta, posizione calcolata da AdaNoteView.Layout() */
.ada-band { position: absolute; top: var(--sp-3); left: var(--sp-4); right: var(--sp-4); flex-direction: row; justify-content: center; }
.ada-note { flex-direction: row; align-items: center; flex-shrink: 1;
    background-color: var(--c-carta); border-width: var(--bw); border-bottom-width: var(--lip); border-color: var(--c-ink); border-radius: var(--r-card);
    padding: var(--sp-2) var(--sp-3) var(--sp-2) var(--sp-2);
    rotate: -1deg;                                   /* biglietto appuntato, appena storto */
    transition-property: opacity, translate, scale; transition-duration: 0.2s, 0.2s, 0.12s; }
.ada-note--in  { opacity: 0; translate: 0 -8px; }
.ada-note--out { opacity: 0; translate: 0 -8px; transition-duration: 0.25s, 0.25s, 0.12s; }
.ada-note--nudge { scale: 1.04 1.04; }
.ada-face { width: 88px; height: 88px; border-radius: var(--r-pill); background-color: var(--c-terracotta); border-width: var(--bw); border-color: var(--c-ink);
    color: var(--c-carta); font-size: var(--fs-label); -unity-text-align: middle-center; margin-right: var(--sp-2); flex-shrink: 0; }
.ada-col { flex-shrink: 1; min-width: 0; }
.ada-name { font-size: var(--fs-label); color: var(--c-terracotta-dk); }          /* 7,2:1 */
.ada-text { font-size: var(--fs-body); color: var(--c-ink); white-space: normal; } /* 11,8:1; min 16 dp forzato da C# */
```

## 2. Checklist sovrapposizioni HUD (WebGL landscape)
Scala `ResponsivePanel` (landscape = H/1200): 1920x1080 → 0,90 · 1366x768 → 0,64 · 960x600 → 0,50. Spazio logico del pannello: 2133x1200 · 2134x1200 · 1920x1200 (quasi uguale: il layout è lo stesso, cambia solo la nitidezza).
Per ogni risoluzione, nella build WebGL (finestra del browser ridimensionata, zoom 100%), stato "peggiore": pillole valute + chip Pedaggio + toast + etichetta stanza + biglietto di Tilde + scheda Cassa/pad vicino a Kaia + freccia a bordo + telefono chiuso/aperto.

| # | Controllo | 1920x1080 | 1366x768 | 960x600 |
|---|---|---|---|---|
| 1 | Pillole valute in alto a sx, dentro la safe area, nessun numero tagliato (valori a 5 cifre, "12.345") | ☐ | ☐ | ☐ |
| 2 | Pulsante telefono sempre visibile intero (≥ 56 px reali di lato) e non coperto dal biglietto | ☐ | ☐ | ☐ |
| 3 | Biglietto di Tilde: in alto al centro, non tocca pillole né telefono, max 2 righe, testo ≥ 16 px reali | ☐ | ☐ | ☐ |
| 4 | Etichetta stanza: non sotto il biglietto (scende sotto se il biglietto è visibile) | ☐ | ☐ | ☐ |
| 5 | Toast "La Cassa ha X LN": sotto le pillole, mai sopra il biglietto (nascosto mentre la nota è visibile) | ☐ | ☐ | ☐ |
| 6 | Chip Pedaggio: accanto/sotto le pillole, mai sopra il telefono | ☐ | ☐ | ☐ |
| 7 | Schede Cassa/pad: clampate dentro lo schermo **e fuori** dai rettangoli HUD (`HudFeel.ClampCard`) | ☐ | ☐ | ☐ |
| 8 | Freccia a bordo: dentro la safe area con margine 24 dp, non sopra le pillole (se lo è, scivola lungo il bordo) | ☐ | ☐ | ☐ |
| 9 | Joystick (click-drag): nasce dove clicchi, non parte se clicchi su pillole/telefono, nessun elemento HUD nella metà bassa salvo il telefono | ☐ | ☐ | ☐ |
| 10 | Telefono aperto: colonna 9:16 centrata, ≤ 90% altezza, si apre/chiude con TAB, ESC = indietro | ☐ | ☐ | ☐ |
| 11 | Scheda Cassaforte: in basso al centro (92%, max 780 px), non copre il telefono | ☐ | ☐ | ☐ |
| 12 | Popup "+8,5 LN": non resta sopra il biglietto più di 0,9 s | ☐ | ☐ | ☐ |
Rapido da editor/batchmode: `HudCapture.Run` (in `wt/ui`, sotto) disegna tutti i contestuali insieme alle 5 risoluzioni; serve solo come controllo interno (nessuna immagine va a Lorenzo).

## 3. API game feel (da incollare)
`Assets/_Game/UI/Runtime/HudFeel.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>Game feel HUD: punto d'atterraggio monete sull'ICONA LN, pulse + conteggio del numero, clamp schede che evita gli angoli HUD.</summary>
    public static class HudFeel
    {
        /// <summary>Centro dell'icona LN (.icon-coin dentro #currency-pill) in coordinate SCHERMO (origine in basso a sx),
        /// da passare a Camera.ScreenToWorldPoint come in CoinSpawner. Fallback: angolo alto-sx.</summary>
        public static Vector2 LnIconScreenPos(VisualElement hudRoot)
        {
            var icon = hudRoot?.Q("currency-pill")?.Q(className: "icon-coin");
            var panel = icon?.panel;
            if (icon == null || panel == null) return new Vector2(80, Screen.height - 50);
            var root = panel.visualTree.layout;
            if (root.width <= 0 || root.height <= 0) return new Vector2(80, Screen.height - 50);
            Vector2 c = icon.worldBound.center;
            return new Vector2(c.x * Screen.width / root.width, Screen.height - c.y * Screen.height / root.height);
        }

        // ---- pulse + count-up del valore LN ----
        sealed class Counter { public double shown, target; public IVisualElementScheduledItem job; }
        static readonly Dictionary<Label, Counter> counters = new Dictionary<Label, Counter>();

        /// <summary>Chiamare all'ARRIVO di ogni moneta (callback di CoinSpawner/fx_coin_flyer), non al credito logico.
        /// Il numero conta verso il target in ~0,25 s; l'icona fa squash 1,25→1 in 120 ms. Nessun numero di bilancio qui.</summary>
        public static void PulseLn(VisualElement hudRoot, double newTotal, System.Func<double, string> format)
        {
            var label = hudRoot?.Q<Label>("ln-value"); var icon = hudRoot?.Q("currency-pill")?.Q(className: "icon-coin");
            if (icon != null)
            {
                icon.style.scale = new Scale(new Vector3(1.25f, 1.25f, 1));
                icon.schedule.Execute(() => icon.style.scale = new Scale(Vector3.one)).StartingIn(120); // con transition: scale 0.12s in USS
            }
            if (label == null) return;
            if (!counters.TryGetValue(label, out var c)) { c = new Counter { shown = newTotal }; counters[label] = c; }
            c.target = newTotal;
            c.job?.Pause();
            c.job = label.schedule.Execute(() =>
            {
                double d = c.target - c.shown;
                c.shown = System.Math.Abs(d) < 1 ? c.target : c.shown + d * 0.35; // ~0,25 s a 60 fps
                label.text = format(System.Math.Floor(c.shown));
                if (c.shown == c.target) c.job.Pause();
            }).Every(16);
        }

        /// <summary>Clamp di una scheda world-anchored (Cassa, pad): dentro il pannello con margine e FUORI dai rettangoli HUD
        /// (pillole valute, chip Pedaggio, pulsante telefono, biglietto di Ada). p = punto di ancoraggio (centro-basso, USS translate -50% -100%).</summary>
        public static Vector2 ClampCard(VisualElement card, Vector2 p, IList<Rect> avoidPanelRects, float margin = 12f)
        {
            var panel = card.panel; if (panel == null) return p;
            var pr = panel.visualTree.layout; float w = card.layout.width, h = card.layout.height;
            if (float.IsNaN(w) || pr.width <= 0) return p;
            p.x = Mathf.Clamp(p.x, w * 0.5f + margin, Mathf.Max(w * 0.5f + margin, pr.width - w * 0.5f - margin));
            p.y = Mathf.Clamp(p.y, h + margin, Mathf.Max(h + margin, pr.height - margin));
            if (avoidPanelRects == null) return p;
            for (int i = 0; i < avoidPanelRects.Count; i++)
            {
                var r = avoidPanelRects[i]; if (r.width <= 0) continue;
                var cr = new Rect(p.x - w * 0.5f, p.y - h, w, h);
                if (!cr.Overlaps(r)) continue;
                // spingi la scheda sotto il rettangolo (HUD in alto) o sopra (HUD in basso), scegliendo il lato più corto
                float down = r.yMax + margin + h - p.y, up = p.y - (r.yMin - margin);
                p.y = down <= up ? p.y + down : p.y - up;
                p.y = Mathf.Clamp(p.y, h + margin, Mathf.Max(h + margin, pr.height - margin));
            }
            return p;
        }

        /// <summary>Rettangoli HUD (coordinate pannello) da evitare; visibili soltanto.</summary>
        public static List<Rect> HudRects(VisualElement hudRoot, List<Rect> into = null)
        {
            into ??= new List<Rect>(); into.Clear();
            foreach (var n in new[] { "currency-pill", "toll-chip", "phone-button", "ada-note", "room-pill", "toast" })
            {
                var e = hudRoot?.Q(n);
                if (e != null && e.resolvedStyle.display != DisplayStyle.None && e.resolvedStyle.visibility == Visibility.Visible) into.Add(e.worldBound);
            }
            return into;
        }
    }
}
```
**Uso:**
```csharp
// CoinSpawner: bersaglio = icona LN, non il bordo della pillola
var sp = UltimoMiglio.UI.HudFeel.LnIconScreenPos(_hud.Root);
dst = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 4f));
// all'arrivo della moneta (u >= 1), dopo CollectActive:
UltimoMiglio.UI.HudFeel.PulseLn(_hud.Root, GameRoot.I.Eco.S.Wallet, UltimoMiglio.UI.HudFormat.Amount);
// WorldAnchoredElement.LateUpdate, al posto del clamp attuale (riga ~58):
static readonly List<Rect> _hudRects = new List<Rect>();
p = UltimoMiglio.UI.HudFeel.ClampCard(Element, p, UltimoMiglio.UI.HudFeel.HudRects(hud.Root, _hudRects));
```
Nota: `HudGameBridge.Refresh` oggi chiama `SetCurrencies` appena cambia il wallet, cioè prima che la moneta arrivi. Per il conteggio animato, aggiornare il numero LN **solo** da `PulseLn` (all'arrivo) e lasciare a `SetCurrencies` i Bulloni e il primo valore.

## 4. Già implementato in `wt/ui` (non committato, branch `ui` da `webgl-min` 3d43efb)
Il TL ha già unito in `webgl-min` `ResponsivePanel`, il bind con retry e il telefono 9:16. Restano in `wt/ui`:
- `Assets/_Game/UI/Runtime/ResponsivePanel.cs` - scala dal lato corto (portrait min(W/1080,H/1920), landscape H/1200), classi `um-landscape`/`um-portrait`.
- `Assets/_Game/UI/Runtime/HudController.cs` - `TryBind()` con retry, contestuali nascosti al bind, cache valute/badge, hint "TAB" solo desktop.
- `Assets/_Game/UI/Runtime/PhoneController.cs` - TAB apre/chiude, ESC indietro, `#phone-frame` 900x1600 scalato (≤ 90% H, ≤ 94% W).
- `Assets/_Game/UI/Runtime/FloatingJoystick.cs` - WASD/frecce + click-drag del mouse nello stesso `Value`, senza TouchSimulation.
- `Assets/_Game/UI/Runtime/SafeAreaUtil.cs` - DpToPx ≥ 1 sul desktop, padding zero sotto cattura.
- `Assets/_Game/UI/Editor/HudCapture.cs` - cattura batchmode a 5 risoluzioni con tutti i contestuali accesi.
- `Assets/_Game/UI/Editor/HudSetup.cs` - PanelSettings 1080x1920 + ResponsivePanel sui due UIDocument.
- `UXML/Hud.uxml`, `UXML/Phone.uxml`, `USS/DioramaSolare.uss` - layout flex (top-bar, `top-center`, `bottom-dock`), contestuali `display:none` di default.
- Icone: 24 Lucide (ISC) + 3 originali, `Icons/Source~/build_icons.py`, `LICENSE_Lucide_ISC.txt`; registrate in `LICENZE_ASSET.md`.
I due componenti di questo documento (`AdaNoteView`, `HudFeel`) **non** sono in `wt/ui`, per rispettare il blocco su `Assets/_Game/UI`: il loro codice è solo in questo file.
