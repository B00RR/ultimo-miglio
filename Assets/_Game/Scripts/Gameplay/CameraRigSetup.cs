using Unity.Cinemachine;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Cinemachine 3 ortho 3/4 follow (02 §2b, 05_3D_PIPELINE): pitch 35.3°, yaw 45°, position composer damping 0.25 s,
    /// look-ahead (1.5 m at Nina's max speed), optional confiner per room. No player pan/zoom.</summary>
    public sealed class CameraRigSetup : MonoBehaviour
    {
        public Transform Target; public float OrthoSize = 4.5f, Distance = 30f, Pitch = 50f, Yaw = 45f;
        public Collider ConfinerVolume;

        public CinemachineCamera Build(Camera main)
        {
            main.orthographic = true; main.orthographicSize = OrthoSize; main.nearClipPlane = 0.3f; main.farClipPlane = 120f;
            if (!main.GetComponent<CinemachineBrain>()) main.gameObject.AddComponent<CinemachineBrain>().DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.6f);
            var go = new GameObject("CM_Follow"); go.transform.SetParent(transform);
            go.transform.rotation = Quaternion.Euler(Pitch, Yaw, 0);
            var cm = go.AddComponent<CinemachineCamera>();
            cm.Follow = Target; cm.Lens = new LensSettings { OrthographicSize = OrthoSize, NearClipPlane = 0.3f, FarClipPlane = 120f, ModeOverride = LensSettings.OverrideModes.Orthographic };
            var pc = go.AddComponent<CinemachinePositionComposer>();
            pc.CameraDistance = Distance; pc.Damping = new Vector3(0.25f, 0.25f, 0.25f);
            pc.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.5f, Smoothing = 5f, IgnoreY = true }; // 0.5 s × 3 m/s ≈ 1.5 m
            var fit = go.AddComponent<DepotCameraFit>(); fit.Cm = cm; fit.Nina = Target;
            if (ConfinerVolume) go.AddComponent<CinemachineConfiner3D>().BoundingVolume = ConfinerVolume;
            return cm;
        }
    }
}
