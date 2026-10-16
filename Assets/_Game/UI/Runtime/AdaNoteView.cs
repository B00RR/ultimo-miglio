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
        public const int MaxChars = 70; // testi Narrative (PR #62): max 70 caratteri, mai ellissi
        public const float MinVisibleSec = 1.5f, FadeSec = 0.25f, MinBodyDp = 14f, MinTitleDp = 12f, TopDp = 68f;

        struct Item { public string text; public float seconds; public bool sticky; }
        readonly Queue<Item> queue = new Queue<Item>();
        readonly VisualElement root, band, note; readonly Label who, body;
        Item current; bool hasCurrent; float shownAt, hideAt = -1;

        public static AdaNoteView Attach(VisualElement hudRoot, string speaker = "Oma Tilde") => new AdaNoteView(hudRoot, speaker);

        AdaNoteView(VisualElement hudRoot, string speaker)
        {
            root = hudRoot;
            band = new VisualElement { name = "ada-band", pickingMode = PickingMode.Ignore }; band.AddToClassList("ada-band");
            var wrap = new VisualElement { name = "ada-wrap", pickingMode = PickingMode.Ignore }; wrap.AddToClassList("ada-wrap");
            note = new VisualElement { name = "ada-note", pickingMode = PickingMode.Ignore }; note.AddToClassList("ada-note");
            var face = new VisualElement { name = "ada-face", pickingMode = PickingMode.Ignore };
            face.AddToClassList("ada-face"); face.AddToClassList("ada-face--portrait"); // ritratto tex_ada_portrait.png via USS
            var col = new VisualElement { pickingMode = PickingMode.Ignore }; col.AddToClassList("ada-col");
            who = new Label(UltimoMiglio.Core.DisplayNames.Apply(speaker)) { pickingMode = PickingMode.Ignore }; who.AddToClassList("ada-name");
            body = new Label { pickingMode = PickingMode.Ignore }; body.AddToClassList("ada-text");
            who.style.display = DisplayStyle.None;
            // L'ombra è nel PNG del fumetto: un figlio si disegnerebbe sopra il crema.
            note.Add(face); col.Add(body); note.Add(col); wrap.Add(note); band.Add(wrap);
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
            if (t.Length > MaxChars) Debug.LogWarning($"[AdaNote] {t.Length} caratteri > {MaxChars}: il fumetto va a capo, accorciare il testo nei dati.");
            current = it; hasCurrent = true; shownAt = Time.unscaledTime; hideAt = -1;
            body.text = t; // #UI: niente taglio a 42 caratteri: 2 righe compatte
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

        /// <summary>Fumetto sotto la riga alta (safe area + 68 dp). Crema opaco nello sprite, alto quanto le righe, senza tetto. Stili in Etichette.uss.</summary>
        void Layout(float scale)
        {
            var safe = root.Q("safe-root");
            float safeTop = safe != null ? safe.resolvedStyle.paddingTop : 0f;
            band.style.position = Position.Absolute;
            band.style.left = 12; band.style.right = 12; band.style.top = safeTop + TopDp;
            band.style.height = StyleKeyword.Auto;
            band.style.maxHeight = StyleKeyword.None;
            band.style.overflow = Overflow.Visible;
            note.style.height = StyleKeyword.Auto;
            note.style.maxHeight = StyleKeyword.None;
            note.style.overflow = Overflow.Visible;
            body.style.height = StyleKeyword.Auto;
            body.style.maxHeight = StyleKeyword.None;
            body.style.overflow = Overflow.Visible;
            if (who != null) who.style.display = DisplayStyle.None;
        }
    }
}
