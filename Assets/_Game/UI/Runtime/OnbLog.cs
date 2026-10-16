using UnityEngine;

namespace UltimoMiglio.UI
{
    /// <summary>Log dell'onboarding "[Onb] …" (gate, aiuti, comparse, camera, combo). Spento di default nelle build di rilascio
    /// (WebGL: non intasa la console); acceso in Editor e nelle build di sviluppo. QA: OnbLog.Enabled = true.</summary>
    public static class OnbLog
    {
        public static bool Enabled = Application.isEditor || Debug.isDebugBuild;
        public static void Write(string msg) { if (Enabled) Debug.Log("[Onb] " + msg); }
    }
}
