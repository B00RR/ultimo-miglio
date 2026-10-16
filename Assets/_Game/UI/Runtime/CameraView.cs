using UnityEngine;

namespace UltimoMiglio.UI
{
    /// <summary>
    /// #205 telecamera regolabile: stato condiviso tra pannello Impostazioni (UI), pinch e DepotCameraFit (Gameplay).
    /// Valori del giocatore: Zoom 0,7–1,4 (1,0 = vista attuale; moltiplica l'ortho size), Rot −45°..+45° a scatti di 15°
    /// (yaw rispetto all'iso di base), Tilt 35°–60° a scatti di 5° (pitch). Durante il tutorial (Tutorial = true) lo zoom
    /// effettivo resta in 0,85–1,2 e rotazione/inclinazione sono bloccate sui predefiniti.
    /// PlayerPrefs: cam_zoom, cam_rot, cam_tilt, cam_ver=1; versione diversa → predefiniti. La vecchia cam_zoom_v3 (ortho) viene eliminata.
    /// Nessuna allocazione: solo campi statici e calcoli su float.
    /// </summary>
    public static class CameraView
    {
        public const string PrefZoom = "cam_zoom", PrefRot = "cam_rot", PrefTilt = "cam_tilt", PrefVer = "cam_ver", LegacyPrefZoom = "cam_zoom_v3";
        public const int Version = 1;
        public const float ZoomMin = 0.7f, ZoomMax = 1.4f, ZoomStep = 0.05f, ZoomDefault = 1f;
        public const float RotMin = -45f, RotMax = 45f, RotStep = 15f, RotDefault = 0f;
        public const float TiltMin = 35f, TiltMax = 60f, TiltStep = 5f;
        public const float TutZoomMin = 0.85f, TutZoomMax = 1.2f;
        public const float ResetSec = 0.3f, PinchSmoothSec = 0.15f, PinchOvershoot = 0.06f;

        /// <summary>Inclinazione predefinita = quella della scena (impostata da DepotCameraFit allo Start), clampata ai limiti.</summary>
        public static float DefaultTilt { get; private set; } = 50f;
        public static float Zoom { get; private set; } = ZoomDefault;
        public static float Rot { get; private set; } = RotDefault;
        public static float Tilt { get; private set; } = 50f;
        /// <summary>true mentre TutorialManager.Current &lt; Fine (aggiornato da DepotCameraFit ogni frame).</summary>
        public static bool Tutorial;
        /// <summary>Pinch in corso: il valore di zoom può superare i limiti di PinchOvershoot (rimbalzo), il joystick è sospeso.</summary>
        public static bool Pinching;
        /// <summary>Pannello Impostazioni aperto: niente pinch in gioco.</summary>
        public static bool UiOpen;
        /// <summary>Time.unscaledTime dell'ultima richiesta di reset (transizione 0,3 s); &lt; 0 = nessuna.</summary>
        public static float ResetRequestedAt { get; private set; } = -1f;
        /// <summary>Contatore incrementato a ogni modifica: chi applica i valori sa se qualcosa è cambiato.</summary>
        public static int Revision { get; private set; }

        public static float Snap(float v, float step, float min) => step > 0f ? min + Mathf.Round((v - min) / step) * step : v;
        public static float ClampZoom(float z) => Mathf.Clamp(z, ZoomMin, ZoomMax);
        public static float SnapZoom(float z) => ClampZoom(Snap(z, ZoomStep, ZoomMin));
        public static float SnapRot(float r) => Mathf.Clamp(Snap(r, RotStep, RotMin), RotMin, RotMax);
        public static float SnapTilt(float t) => Mathf.Clamp(Snap(t, TiltStep, TiltMin), TiltMin, TiltMax);

        /// <summary>Limiti di zoom attivi (tutorial: 0,85–1,2).</summary>
        public static float ActiveZoomMin => Tutorial ? TutZoomMin : ZoomMin;
        public static float ActiveZoomMax => Tutorial ? TutZoomMax : ZoomMax;
        public static float EffectiveZoom => Mathf.Clamp(Zoom, Pinching ? ActiveZoomMin - PinchOvershoot : ActiveZoomMin, Pinching ? ActiveZoomMax + PinchOvershoot : ActiveZoomMax);
        public static float EffectiveRot => Tutorial ? RotDefault : Rot;
        public static float EffectiveTilt => Tutorial ? DefaultTilt : Tilt;
        public static bool RotTiltLocked => Tutorial;

        public static void SetDefaultTilt(float sceneTilt)
        {
            float d = Mathf.Clamp(sceneTilt, TiltMin, TiltMax);
            if (Mathf.Approximately(d, DefaultTilt)) return;
            bool wasDefault = Mathf.Approximately(Tilt, DefaultTilt);
            DefaultTilt = d; if (wasDefault) Tilt = d; Revision++;
        }

        /// <summary>Slider zoom: scatti di 0,05, limiti attivi.</summary>
        public static void SetZoom(float z) { Zoom = Mathf.Clamp(SnapZoom(z), ActiveZoomMin, ActiveZoomMax); Revision++; }
        /// <summary>Pinch: valore continuo con rimbalzo oltre i limiti (riportato nei limiti da EndPinch).</summary>
        public static void SetZoomPinch(float z) { Zoom = Mathf.Clamp(z, ActiveZoomMin - PinchOvershoot, ActiveZoomMax + PinchOvershoot); Revision++; }
        public static void EndPinch() { Pinching = false; Zoom = Mathf.Clamp(Zoom, ActiveZoomMin, ActiveZoomMax); Revision++; Save(); }
        public static void SetRot(float r) { if (RotTiltLocked) return; Rot = SnapRot(r); Revision++; }
        public static void SetTilt(float t) { if (RotTiltLocked) return; Tilt = SnapTilt(t); Revision++; }

        /// <summary>"Ripristina vista": predefiniti con transizione di 0,3 s (applicata da DepotCameraFit) e salvataggio.</summary>
        public static void ResetView() { Zoom = ZoomDefault; Rot = RotDefault; Tilt = DefaultTilt; ResetRequestedAt = Time.unscaledTime; Revision++; Save(); }
        /// <summary>Doppio tocco con due dita: solo zoom a 1,0.</summary>
        public static void ResetZoom() { Zoom = ZoomDefault; ResetRequestedAt = Time.unscaledTime; Revision++; Save(); }

        public static void Save()
        {
            PlayerPrefs.SetFloat(PrefZoom, Zoom); PlayerPrefs.SetFloat(PrefRot, Rot); PlayerPrefs.SetFloat(PrefTilt, Tilt);
            PlayerPrefs.SetInt(PrefVer, Version); PlayerPrefs.Save();
        }

        /// <summary>Carica da PlayerPrefs; cam_ver diversa da 1 (o assente) → predefiniti. Elimina la vecchia cam_zoom_v3.</summary>
        public static void Load()
        {
            if (PlayerPrefs.HasKey(LegacyPrefZoom)) { PlayerPrefs.DeleteKey(LegacyPrefZoom); PlayerPrefs.Save(); }
            Pinching = false;
            if (PlayerPrefs.GetInt(PrefVer, 0) != Version) { Zoom = ZoomDefault; Rot = RotDefault; Tilt = DefaultTilt; Revision++; return; }
            Zoom = SnapZoom(PlayerPrefs.GetFloat(PrefZoom, ZoomDefault));
            Rot = SnapRot(PlayerPrefs.GetFloat(PrefRot, RotDefault));
            Tilt = SnapTilt(PlayerPrefs.GetFloat(PrefTilt, DefaultTilt));
            Revision++;
        }

        /// <summary>Solo test e catture: imposta direttamente i valori (con scatti e clamp, ignora il blocco del tutorial).</summary>
        public static void SetForTest(float zoom, float rot, float tilt) { Zoom = SnapZoom(zoom); Rot = SnapRot(rot); Tilt = SnapTilt(tilt); ResetRequestedAt = -1f; Revision++; }
    }
}
