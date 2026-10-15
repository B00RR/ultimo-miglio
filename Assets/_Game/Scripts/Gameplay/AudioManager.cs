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
        public enum Sfx { StackMax, Deposit, CoinDrop, CoinPickup, Bell, Served, UpgradeComplete, ComboX15, ComboX2, NonnoStart, NonnoMumble, Step }

        [Header("Music")] public AudioClip[] MusicStems = new AudioClip[3]; public AudioClip MusicFull; public bool UseStems = true;
        [Range(0, 1)] public float MusicVolume = 0.6f;
        [Header("SFX")] public AudioClip[] Pickup = new AudioClip[8]; public AudioClip[] Deposit, CoinDrop, CoinPickup, NonnoMumble, Steps;
        public AudioClip StackMax, Bell, Served, UpgradeLoop, UpgradeComplete, ComboX15, ComboX2, NonnoStart;
        public AudioMixerGroup MusicGroup, SfxGroup;
        public int MaxVoices = 6;

        AudioSource[] _music, _voices; AudioSource _loop; int _next;

        void Awake()
        {
            I = this;
            _voices = new AudioSource[MaxVoices];
            for (int i = 0; i < MaxVoices; i++) _voices[i] = NewSource("sfx" + i, SfxGroup, false);
            _loop = NewSource("upgradeLoop", SfxGroup, true); _loop.clip = UpgradeLoop;
        }
        void Start() => StartMusic();

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
        public void SetStemVolume(int i, float v) { if (_music != null && i < _music.Length && _music[i]) _music[i].volume = v * MusicVolume; }

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
                case Sfx.Step: One(Pick(Steps), 0.5f); break;
            }
        }
        public void StartUpgradeLoop() { if (_loop.clip && !_loop.isPlaying) _loop.Play(); }
        public void StopUpgradeLoop() { if (_loop.isPlaying) _loop.Stop(); }

        static AudioClip Pick(AudioClip[] a) => a == null || a.Length == 0 ? null : a[Random.Range(0, a.Length)];
        void One(AudioClip c, float vol = 1f)
        {
            if (!c) return;
            var s = _voices[_next = (_next + 1) % _voices.Length]; // voice cap (README: coins cap 6 voices)
            s.pitch = 1f + Random.Range(-0.03f, 0.03f); s.PlayOneShot(c, vol);
        }
    }
}
