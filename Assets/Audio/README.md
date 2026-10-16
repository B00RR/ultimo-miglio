# Bokobay – Audio Sprint 1 (placeholder)

Generated procedurally by `/workspace/ultimo-miglio/audio/tools/gen_sprint1.py` (numpy/scipy, Karplus-Strong, additive/subtractive synthesis). No third-party samples. Re-run the script to regenerate.

## License
Original procedural work, owned by the Bokobay project and released as **CC0 1.0** (public domain dedication). No attribution required.

## SFX/ (mono, 44.1 kHz, WAV 16-bit + OGG Vorbis q4, peak -1 dBFS, HPF 120 Hz)
| File(s) | Use |
|---|---|
| sfx_step_stone_01..04, sfx_step_wood_01..04 | Kaia footsteps, 2 surfaces × 4 variants |
| sfx_parcel_pickup_01..08 | pickup tick; use index = stack height (rising pentatonic steps up to MAX) |
| sfx_parcel_deposit_01..04 | shelf deposit soft thud, pitch falls 01→04 |
| sfx_stack_max | double spring click |
| sfx_coin_drop_01..04, sfx_coin_pickup_01..04 | LN coins (cap 6 voices) |
| sfx_customer_bell, sfx_customer_stamp, sfx_customer_served | door bell, counter stamp, combined |
| sfx_upgrade_tick_loop | 0.6 s seamless loop while paying on upgrade pad (set Loop) |
| sfx_upgrade_complete | purchase complete chord (D major) |
| sfx_nonno_start, sfx_nonno_mumble_01..03 | OLDO forklift cough/start + grammelot mumbles |
| sfx_combo_x1_5, sfx_combo_x2 | zither arpeggios |

## Music/ (stereo 44.1 kHz, WAV 16-bit masters + OGG Vorbis 120 kbps)
`mus_day_vela_e_figli_{base,perc,mel,full}` – "Sunn Depot", 100 BPM, D major, 32 bars = 76.800 s = 3,386,880 samples, all stems identical length, sample-aligned, seamless loop (reverb/release tails wrapped into the start). Same limiter gain applied to all stems so base+perc+mel = full. Full mix -16 LUFS integrated, peak -1.2 dBFS.
Layers: base (bass pizz, nylon guitar, bellows organ pad, soft analog pad), perc (cardboard box kick, crate rim, shaker), mel (Karplus-Strong zither; sparse bars 1-8, full from 9, octave sparkle from 17).

## Unity import settings
- SFX (<1 s): Force To Mono, Load Type **Decompress On Load**, Compression **Vorbis** quality ~70 (or ADPCM for footsteps/pickup), Sample Rate Override 22050 optional, Preload Audio Data on.
- sfx_upgrade_tick_loop / sfx_nonno_start (≥0.6 s): Decompress On Load OK (short) or Compressed In Memory.
- Music: Load Type **Streaming**, Vorbis quality ~70 (≈120 kbps), Preload off, Load In Background on. Play the 3 stems on 3 AudioSources started with the same `PlayScheduled(dspTime)`; fade layers via AudioMixer. Use the `.ogg` files in-project; the `.wav` are masters (move them out of Assets/ or exclude from build if size matters).
- Mixer groups: Music / SFX / UI / Voices per 06_AUDIO.md.
