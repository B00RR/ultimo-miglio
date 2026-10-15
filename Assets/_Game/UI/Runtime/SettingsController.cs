using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// Pannello Impostazioni (ingranaggio HUD): zoom con anteprima live, slider Musica/Suoni, Salva/Indietro.
    /// PlayerPrefs: cam_zoom, vol_music, vol_sfx (0–1).
    /// Chiama per nome (reflection, UI non referenzia Gameplay):
    ///   DepotCameraFit.SetOrthoSize(float) / GetOrthoSize()
    ///   AudioManager.SetMusicVolume(float) / SetSfxVolume(float)
    /// Il Tech Lead crea/collega quei metodi in scena.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsController : MonoBehaviour
    {
        public const string PrefCamZoom = "cam_zoom";
        public const string PrefVolMusic = "vol_music";
        public const string PrefVolSfx = "vol_sfx";

        public event Action Opened, Closed;

        public bool IsOpen { get; private set; }

        [SerializeField] float zoomStep = 0.5f;
        [SerializeField] float zoomMin = 5.5f;
        [SerializeField] float zoomMax = 28f;
        [SerializeField] float defaultZoom = 12f;

        VisualElement root, settingsRoot, scrim;
        Label zoomValue;
        Slider musicSlider, sfxSlider;
        float draftZoom, savedZoom;
        float draftMusic, savedMusic;
        float draftSfx, savedSfx;
        bool bound;
        HudController hud;

        void OnEnable() { bound = false; TryBind(); ApplySavedOnBoot(); }
        void OnDisable()
        {
            if (hud != null) hud.SettingsButtonTapped -= Open;
        }

        void ApplySavedOnBoot()
        {
            // Applica i preferiti salvati all'avvio (anteprima = valore salvato).
            savedZoom = PlayerPrefs.GetFloat(PrefCamZoom, defaultZoom);
            savedMusic = PlayerPrefs.GetFloat(PrefVolMusic, 0.6f);
            savedSfx = PlayerPrefs.GetFloat(PrefVolSfx, 1f);
            draftZoom = savedZoom; draftMusic = savedMusic; draftSfx = savedSfx;
            ApplyZoomLive(savedZoom);
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
            zoomValue = root.Q<Label>("zoom-value");
            musicSlider = root.Q<Slider>("slider-music");
            sfxSlider = root.Q<Slider>("slider-sfx");

            root.Q<Button>("zoom-minus")?.RegisterCallback<ClickEvent>(_ => ChangeZoom(-zoomStep));
            root.Q<Button>("zoom-plus")?.RegisterCallback<ClickEvent>(_ => ChangeZoom(+zoomStep));
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
            gear?.RegisterCallback<ClickEvent>(_ => Open());
            if (hud == null) hud = FindFirstObjectByType<HudController>();
            if (hud != null) hud.SettingsButtonTapped += Open;

            SetOpen(false);
            RefreshZoomLabel();
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(savedMusic);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(savedSfx);
            bound = true;
            return true;
        }

        void Update()
        {
            if (!bound) TryBind();
        }

        public void Open()
        {
            if (!TryBind()) return;
            // snapshot pre-Salva
            savedZoom = PlayerPrefs.GetFloat(PrefCamZoom, GetOrthoOrDefault());
            savedMusic = PlayerPrefs.GetFloat(PrefVolMusic, 0.6f);
            savedSfx = PlayerPrefs.GetFloat(PrefVolSfx, 1f);
            draftZoom = GetOrthoOrDefault();
            if (Mathf.Approximately(draftZoom, 0f)) draftZoom = savedZoom;
            draftMusic = savedMusic; draftSfx = savedSfx;
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(draftMusic);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(draftSfx);
            RefreshZoomLabel();
            SetOpen(true);
            Opened?.Invoke();
        }

        public void Close() { SetOpen(false); Closed?.Invoke(); }

        void SetOpen(bool on)
        {
            IsOpen = on;
            if (settingsRoot != null)
            {
                // se settings-root è il layer dedicato
                var layer = root.Q("settings-root") ?? settingsRoot;
                layer.style.display = on ? DisplayStyle.Flex : DisplayStyle.None;
                layer.pickingMode = on ? PickingMode.Position : PickingMode.Ignore;
            }
        }

        void ChangeZoom(float delta)
        {
            draftZoom = Mathf.Clamp(draftZoom + delta, zoomMin, zoomMax);
            RefreshZoomLabel();
            ApplyZoomLive(draftZoom);
        }

        void RefreshZoomLabel()
        {
            if (zoomValue != null) zoomValue.text = draftZoom.ToString("0.0");
        }

        void Save()
        {
            savedZoom = draftZoom; savedMusic = draftMusic; savedSfx = draftSfx;
            PlayerPrefs.SetFloat(PrefCamZoom, savedZoom);
            PlayerPrefs.SetFloat(PrefVolMusic, savedMusic);
            PlayerPrefs.SetFloat(PrefVolSfx, savedSfx);
            PlayerPrefs.Save();
            ApplyZoomLive(savedZoom);
            ApplyAudioLive(savedMusic, savedSfx);
            Close();
        }

        void CancelAndClose()
        {
            draftZoom = savedZoom; draftMusic = savedMusic; draftSfx = savedSfx;
            ApplyZoomLive(savedZoom);
            ApplyAudioLive(savedMusic, savedSfx);
            Close();
        }

        float GetOrthoOrDefault()
        {
            float v = InvokeFloat("DepotCameraFit", "GetOrthoSize", defaultZoom);
            return v > 0.01f ? v : defaultZoom;
        }

        void ApplyZoomLive(float ortho) => InvokeVoid("DepotCameraFit", "SetOrthoSize", ortho);

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

        static float InvokeFloat(string typeName, string method, float fallback)
        {
            var mb = FindByTypeName(typeName);
            if (mb == null) return fallback;
            var m = mb.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (m == null) return fallback;
            object r = m.IsStatic ? m.Invoke(null, null) : m.Invoke(mb, null);
            return r is float f ? f : fallback;
        }

        static MonoBehaviour FindByTypeName(string typeName)
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (mb != null && mb.GetType().Name == typeName) return mb;
            return null;
        }
    }
}
