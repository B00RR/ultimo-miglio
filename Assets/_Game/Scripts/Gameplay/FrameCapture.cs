using System.Collections;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Standalone capture: fixed 30 fps game time, full screen incl. UI Toolkit HUD, N frames to Dir/f_00000.png, then quits.</summary>
    public sealed class FrameCapture : MonoBehaviour
    {
        public string Dir; public int Fps = 30, Frames = 900, Warmup = 30; int _f;
        void Start() { System.IO.Directory.CreateDirectory(Dir); Time.captureFramerate = Fps; StartCoroutine(Run()); }
        IEnumerator Run()
        {
            for (int i = 0; i < Warmup; i++) yield return new WaitForEndOfFrame();
            while (_f < Frames)
            {
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Dir, $"f_{_f++:00000}.png"));
            }
            yield return null; yield return null;
            Debug.Log("[Capture] done " + _f); Application.Quit();
        }
    }
}
