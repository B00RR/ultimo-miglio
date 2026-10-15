using UnityEditor;
using UnityEngine;

namespace UltimoMiglio.EditorTools
{
    /// <summary>Applies Assets/Audio/README.md import settings. WAV masters are excluded from builds (only OGG referenced).</summary>
    public sealed class AudioImportSettings : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            bool music = assetPath.Contains("/Music/");
            if (music)
            {
                imp.forceToMono = false; imp.loadInBackground = true;
                s.loadType = AudioClipLoadType.Streaming; s.compressionFormat = AudioCompressionFormat.Vorbis; s.quality = 0.7f; s.preloadAudioData = false;
            }
            else
            {
                imp.forceToMono = true; imp.loadInBackground = false;
                bool longer = assetPath.Contains("upgrade_tick_loop") || assetPath.Contains("nonno_start");
                bool adpcm = assetPath.Contains("sfx_step_") || assetPath.Contains("parcel_pickup");
                s.loadType = longer ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = adpcm ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis; s.quality = 0.7f;
                s.preloadAudioData = true;
                s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate; s.sampleRateOverride = 22050;
            }
            imp.defaultSampleSettings = s;
        }
    }
}
