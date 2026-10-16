using UnityEngine;
using UnityEngine.Audio;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Audio per Assets/Audio/README.md: music = 3 stems (base/perc/mel) started with one PlayScheduled(dspTime)
    /// so they stay sample-aligned (the "full" mix is the fallback for tier Base); pickup pitch steps 01..08 by stack height;
    /// upgrade tick loop while a pad fills. Clips are wired by the scene builder (Editor/SliceSceneBuilder).</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }
        public enum Sfx { StackMax, Deposit, CoinDrop, CoinPickup, Bell, Served, UpgradeComplete, ComboX15, ComboX2, NonnoStart, NonnoMumble, Step, UiTap, TutorialArrow, TutorialStep }
        public const string PrefMusic = "vol_music", PrefSfx = "vol_sfx"; // stesse chiavi di SettingsPanel

        [Header("Music")] public AudioClip[] MusicStems = new AudioClip[3]; public AudioClip MusicFull; public bool UseStems = true;
        [Range(0, 1)] public float MusicVolume = 0.6f;
        [Header("SFX")] public AudioClip[] Pickup = new AudioClip[8]; public AudioClip[] Deposit, CoinDrop, CoinPickup, NonnoMumble, Steps;
        [Header("UI / onboarding")] public AudioClip[] UiTap; public AudioClip TutorialArrow, TutorialStep;
        [Header("Passi")] public float StepDistance = 0.85f;
        public AudioClip StackMax, Bell, Served, UpgradeLoop, UpgradeComplete, ComboX15, ComboX2, NonnoStart;
        public AudioMixerGroup MusicGroup, SfxGroup;
        public int MaxVoices = 6;

        AudioSource[] _music, _voices; AudioSource _loop; int _next;

        void Awake()
        {
            I = this;
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusic, MusicVolume)); // valori salvati applicati anche senza aprire le Impostazioni
            _sfxMul = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfx, 1f));
            _voices = new AudioSource[MaxVoices];
            for (int i = 0; i < MaxVoices; i++) _voices[i] = NewSource("sfx" + i, SfxGroup, false);
            _loop = NewSource("upgradeLoop", SfxGroup, true); _loop.clip = UpgradeLoop;
        }
        public static bool Unlocked; // browsers: audio only after the first user gesture
        void OnEnable() { TutorialManager.StepChanged += OnTutorialStep; }
        void OnDisable() { TutorialManager.StepChanged -= OnTutorialStep; if (I == this) I = null; }
        void Start() { if (Unlocked && _music == null) StartMusic(); } // es. scena ricaricata dopo lo sblocco
        void Update()
        {
            TickFootsteps();
            if (Unlocked) return;
            var kb = UnityEngine.InputSystem.Keyboard.current; var ms = UnityEngine.InputSystem.Mouse.current; var ts = UnityEngine.InputSystem.Touchscreen.current;
            if ((kb != null && kb.anyKey.wasPressedThisFrame) || (ms != null && ms.leftButton.wasPressedThisFrame) || (ts != null && ts.primaryTouch.press.wasPressedThisFrame))
            { Unlocked = true; if (_music == null) StartMusic(); }
        }

        AudioSource NewSource(string n, AudioMixerGroup g, bool loop)
        {
            var s = new GameObject(n).AddComponent<AudioSource>(); s.transform.SetParent(transform);
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0; s.outputAudioMixerGroup = g; return s;
        }

        public void StartMusic()
        {
            var clips = UseStems && MusicStems.Length == 3 && MusicStems[0] ? MusicStems : new[] { MusicFull };
            _music = new AudioSource[clips.Length];
            double t = AudioSettings.dspTime + 0.2; // all stems on the same DSP tick
            for (int i = 0; i < clips.Length; i++)
            {
                if (!clips[i]) continue;
                var s = _music[i] = NewSource("music_" + clips[i].name, MusicGroup, true);
                s.clip = clips[i]; s.volume = MusicVolume; s.PlayScheduled(t);
            }
        }
        /// <summary>Layer fade (e.g. mel only after NONNO). index 0 base, 1 perc, 2 mel.</summary>
        public void SetStemVolume(int i, float v) { if (i >= 0 && i < _stemMul.Length) _stemMul[i] = Mathf.Clamp01(v); ApplyMusicVolume(); }
        float _sfxMul = 1f; readonly float[] _stemMul = { 1f, 1f, 1f };
        public float SfxVolume => _sfxMul;
        public void SetMusicVolume(float v) { MusicVolume = Mathf.Clamp01(v); ApplyMusicVolume(); }
        public void SetSfxVolume(float v) { _sfxMul = Mathf.Clamp01(v); if (_loop) _loop.volume = _sfxMul; }
        void ApplyMusicVolume() { if (_music != null) for (int i = 0; i < _music.Length; i++) if (_music[i]) _music[i].volume = MusicVolume * (i < _stemMul.Length ? _stemMul[i] : 1f); }
        public bool MusicPlaying { get { if (_music == null) return false; foreach (var s in _music) if (s && s.isPlaying) return true; return false; } }

        // Passi: un passo ogni StepDistance metri percorsi da Nina (pavimento in lastre di pietra del deposito).
        Vector3 _lastPos; float _walked; bool _hasPos;
        void TickFootsteps()
        {
            var nina = GameRoot.I ? GameRoot.I.Nina : null;
            if (!nina) { _hasPos = false; return; }
            var p = nina.transform.position; p.y = 0;
            if (!_hasPos) { _lastPos = p; _hasPos = true; return; }
            float d = Vector3.Distance(p, _lastPos); _lastPos = p;
            if (d > 2f) return; // teletrasporto/clamp: niente raffica di passi
            _walked += d;
            if (_walked >= StepDistance) { _walked = 0; Play(Sfx.Step); }
        }

        // Onboarding: "step completato" al passaggio di step, poi "freccia/evidenziazione" se il nuovo step ha un obiettivo.
        bool _tutSeen;
        void OnTutorialStep(TutorialManager.Step s)
        {
            if (!_tutSeen) { _tutSeen = true; if (s < TutorialManager.Step.Fine) Invoke(nameof(PlayArrow), 0.1f); return; } // primo Enter (ripristino) = solo freccia
            Play(Sfx.TutorialStep);
            if (s < TutorialManager.Step.Fine) Invoke(nameof(PlayArrow), 0.45f);
        }
        void PlayArrow() { var tm = TutorialManager.I; if (!tm || tm.CurrentTarget) Play(Sfx.TutorialArrow); }

        public void PlayPickup(int stackHeight)
        {
            if (Pickup == null || Pickup.Length == 0) return;
            One(Pickup[Mathf.Clamp(stackHeight - 1, 0, Pickup.Length - 1)], 1f);
        }
        public void Play(Sfx k)
        {
            switch (k)
            {
                case Sfx.StackMax: One(StackMax); break;
                case Sfx.Deposit: One(Pick(Deposit)); break;
                case Sfx.CoinDrop: One(Pick(CoinDrop), 0.8f); break;
                case Sfx.CoinPickup: One(Pick(CoinPickup), 0.9f); break;
                case Sfx.Bell: One(Bell); break;
                case Sfx.Served: One(Served); break;
                case Sfx.UpgradeComplete: One(UpgradeComplete); break;
                case Sfx.ComboX15: One(ComboX15); break;
                case Sfx.ComboX2: One(ComboX2); break;
                case Sfx.NonnoStart: One(NonnoStart); break;
                case Sfx.NonnoMumble: One(Pick(NonnoMumble)); break;
                case Sfx.Step: One(Pick(Steps), 0.45f); break;
                case Sfx.UiTap: One(Pick(UiTap), 0.8f, 0.02f); break;
                case Sfx.TutorialArrow: One(TutorialArrow, 0.7f, 0f); break;
                case Sfx.TutorialStep: One(TutorialStep, 0.9f, 0f); break;
            }
        }
        public void StartUpgradeLoop() { if (Unlocked && _loop.clip && !_loop.isPlaying) { _loop.volume = _sfxMul; _loop.Play(); } }
        public void StopUpgradeLoop() { if (_loop.isPlaying) _loop.Stop(); }

        static AudioClip Pick(AudioClip[] a) => a == null || a.Length == 0 ? null : a[Random.Range(0, a.Length)];
        void One(AudioClip c, float vol = 1f, float pitchJitter = 0.05f)
        {
            if (!c || !Unlocked || _sfxMul <= 0f) return;
            var s = _voices[_next = (_next + 1) % _voices.Length]; // voice cap (README: coins cap 6 voices)
            s.pitch = 1f + Random.Range(-pitchJitter, pitchJitter); s.PlayOneShot(c, vol * _sfxMul);
        }
    }
}
