using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Pannello Impostazioni (ingranaggio HUD): sezione "Visuale" (#205: slider Zoom/Rotazione/Inclinazione con anteprima
    /// dal vivo e "Ripristina vista"), slider Musica/Suoni, Salva/Indietro.
    /// PlayerPrefs: cam_zoom, cam_rot, cam_tilt, cam_ver (CameraView, salvate al rilascio dello slider), vol_music, vol_sfx (0–1).
    /// Durante il tutorial gli slider Rotazione e Inclinazione sono disabilitati e lo zoom resta in 0,85–1,2.
    /// Chiama per nome (reflection, UI non referenzia Gameplay):
    ///   AudioManager.SetMusicVolume(float) / SetSfxVolume(float)
    /// Il Tech Lead crea/collega quei metodi in scena.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsController : MonoBehaviour
    {
        public const string PrefCamZoom = CameraView.PrefZoom;
        public const string PrefVolMusic = "vol_music";
        public const string PrefVolSfx = "vol_sfx";

        public event Action Opened, Closed;

        public bool IsOpen { get; private set; }

        VisualElement root, settingsRoot, scrim;
        Label camZoomValue, camRotValue, camTiltValue;
        Slider camZoom, camRot, camTilt;
        Button camReset;
        bool camDirty; int camRev = -1; bool camLockShown;
        Slider musicSlider, sfxSlider;
        float draftMusic, savedMusic;
        float draftSfx, savedSfx;
        bool bound;
        HudController hud;

        void OnEnable() { bound = false; TryBind(); ApplySavedOnBoot(); }
        void OnDisable()
        {
            if (hud != null) hud.SettingsButtonTapped -= Toggle;
        }

        void ApplySavedOnBoot()
        {
            // Applica i preferiti salvati all'avvio (anteprima = valore salvato).
            savedMusic = PlayerPrefs.GetFloat(PrefVolMusic, 0.6f);
            savedSfx = PlayerPrefs.GetFloat(PrefVolSfx, 1f);
            draftMusic = savedMusic; draftSfx = savedSfx;
            ApplyAudioLive(savedMusic, savedSfx);
        }

        bool TryBind()
        {
            if (bound) return true;
            var doc = GetComponent<UIDocument>();
            root = doc ? doc.rootVisualElement : null;
            if (root == null) return false;

            // Overlay può essere nel template Settings.uxml (UIDocument dedicato) o innestato in Hud.uxml
            settingsRoot = root.Q("settings-root");
            if (settingsRoot == null)
            {
                // Document dedicato: root = settings-root content
                settingsRoot = root.Q("settings-scrim") != null ? root : null;
            }
            if (settingsRoot == null) return false;

            scrim = root.Q("settings-scrim") ?? settingsRoot;
            camZoom = root.Q<Slider>("slider-cam-zoom"); camRot = root.Q<Slider>("slider-cam-rot"); camTilt = root.Q<Slider>("slider-cam-tilt");
            camZoomValue = root.Q<Label>("cam-zoom-value"); camRotValue = root.Q<Label>("cam-rot-value"); camTiltValue = root.Q<Label>("cam-tilt-value");
            camReset = root.Q<Button>("cam-reset");
            musicSlider = root.Q<Slider>("slider-music");
            sfxSlider = root.Q<Slider>("slider-sfx");

            camZoom?.RegisterValueChangedCallback(e => { CameraView.SetZoom(e.newValue); camDirty = true; RefreshCam(); });
            camRot?.RegisterValueChangedCallback(e => { CameraView.SetRot(e.newValue); camDirty = true; RefreshCam(); });
            camTilt?.RegisterValueChangedCallback(e => { CameraView.SetTilt(e.newValue); camDirty = true; RefreshCam(); });
            camReset?.RegisterCallback<ClickEvent>(_ => { CameraView.ResetView(); camDirty = false; RefreshCam(); });
            musicSlider?.RegisterValueChangedCallback(e => { draftMusic = e.newValue; ApplyAudioLive(draftMusic, draftSfx); });
            sfxSlider?.RegisterValueChangedCallback(e => { draftSfx = e.newValue; ApplyAudioLive(draftMusic, draftSfx); });
            root.Q<Button>("settings-save")?.RegisterCallback<ClickEvent>(_ => Save());
            root.Q<Button>("settings-back")?.RegisterCallback<ClickEvent>(_ => CancelAndClose());
            scrim?.RegisterCallback<ClickEvent>(e =>
            {
                if (e.target == scrim) CancelAndClose();
            });

            // Ingranaggio sul HUD (stesso document o HudController vicino)
            var gear = root.Q("settings-button");
            if (gear == null)
            {
                hud = FindFirstObjectByType<HudController>();
                gear = hud != null && hud.Root != null ? hud.Root.Q("settings-button") : null;
            }
            // Un solo listener per tap (evita apri+chiudi nello stesso click): evento HUD se c'è, altrimenti click diretto.
            if (hud == null) hud = FindFirstObjectByType<HudController>();
            if (hud != null) { hud.SettingsButtonTapped -= Toggle; hud.SettingsButtonTapped += Toggle; }
            else gear?.RegisterCallback<ClickEvent>(_ => Toggle());

            SetOpen(false);
            RefreshCam();
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(savedMusic);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(savedSfx);
            bound = true;
            return true;
        }

        void Update()
        {
            if (!bound) TryBind();
            if (!bound) return;
            if (IsOpen && (camRev != CameraView.Revision || camLockShown != CameraView.RotTiltLocked)) RefreshCam();
            // #205: salvataggio al rilascio dello slider, non a ogni frame
            if (camDirty) { var ptr = UnityEngine.InputSystem.Pointer.current; if (ptr == null || !ptr.press.isPressed) FlushCam(); }
        }

        void FlushCam() { if (!camDirty) return; camDirty = false; CameraView.Save(); }

        /// <summary>Allinea slider ed etichette della sezione Visuale a CameraView (con i blocchi del tutorial).</summary>
        void RefreshCam()
        {
            camRev = CameraView.Revision; bool locked = CameraView.RotTiltLocked; camLockShown = locked;
            float z = CameraView.EffectiveZoom, r = CameraView.EffectiveRot, t = CameraView.EffectiveTilt;
            if (camZoom != null) { camZoom.lowValue = CameraView.ActiveZoomMin; camZoom.highValue = CameraView.ActiveZoomMax; camZoom.SetValueWithoutNotify(z); }
            if (camRot != null) { camRot.SetValueWithoutNotify(r); camRot.SetEnabled(!locked); }
            if (camTilt != null) { camTilt.SetValueWithoutNotify(t); camTilt.SetEnabled(!locked); }
            if (camZoomValue != null) camZoomValue.text = z.ToString("0.00").Replace('.', ',') + "×";
            if (camRotValue != null) camRotValue.text = (r > 0.5f ? "+" : "") + Mathf.RoundToInt(r) + "°";
            if (camTiltValue != null) camTiltValue.text = Mathf.RoundToInt(t) + "°";
        }

        public void Open()
        {
            if (!TryBind()) return;
            // snapshot pre-Salva
            savedMusic = PlayerPrefs.GetFloat(PrefVolMusic, 0.6f);
            savedSfx = PlayerPrefs.GetFloat(PrefVolSfx, 1f);
            draftMusic = savedMusic; draftSfx = savedSfx;
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(draftMusic);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(draftSfx);
            RefreshCam();
            SetOpen(true);
            Opened?.Invoke();
        }

        /// <summary>Tap sull'ingranaggio: apre, o se già aperto chiude ripristinando (come Indietro). QA P3 (qa/playtest_web_live_p0.md, S01).</summary>
        public void Toggle() { if (IsOpen) CancelAndClose(); else Open(); }

        public void Close() { SetOpen(false); Closed?.Invoke(); }

        void SetOpen(bool on)
        {
            IsOpen = on; CameraView.UiOpen = on;
            if (!on) FlushCam();
            if (settingsRoot != null)
            {
                // se settings-root è il layer dedicato
                var layer = root.Q("settings-root") ?? settingsRoot;
                layer.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                layer.pickingMode = on ? PickingMode.Position : PickingMode.Ignore;
            }
        }

        void Save()
        {
            savedMusic = draftMusic; savedSfx = draftSfx;
            PlayerPrefs.SetFloat(PrefVolMusic, savedMusic);
            PlayerPrefs.SetFloat(PrefVolSfx, savedSfx);
            PlayerPrefs.Save();
            ApplyAudioLive(savedMusic, savedSfx);
            Close();
        }

        /// <summary>Indietro: annulla solo i volumi; la visuale è già salvata al rilascio degli slider (#205).</summary>
        void CancelAndClose()
        {
            draftMusic = savedMusic; draftSfx = savedSfx;
            ApplyAudioLive(savedMusic, savedSfx);
            Close();
        }

        void ApplyAudioLive(float music, float sfx)
        {
            InvokeVoid("AudioManager", "SetMusicVolume", music);
            InvokeVoid("AudioManager", "SetSfxVolume", sfx);
        }

        static void InvokeVoid(string typeName, string method, float arg)
        {
            var mb = FindByTypeName(typeName);
            if (mb == null) return;
            var m = mb.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (m == null) return;
            if (m.IsStatic) m.Invoke(null, new object[] { arg });
            else m.Invoke(mb, new object[] { arg });
        }

        static MonoBehaviour FindByTypeName(string typeName)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (mb != null && mb.GetType().Name == typeName) return mb;
            return null;
        }
    }
}
