using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>#58/#59: movimento Transform con sweep (scivola sui collider statici) e facing dalla velocità piana effettiva.</summary>
    public static class AgentMove
    {
        static readonly RaycastHit[] Hits = new RaycastHit[8];

        /// <summary>Muove t di step (piano XZ) senza attraversare collider solidi; ritorna lo spostamento applicato.</summary>
        public static Vector3 Slide(Transform t, Vector3 step, float radius = 0.35f)
        {
            step.y = 0; var start = t.position; var applied = Vector3.zero;
            for (int it = 0; it < 2 && step.sqrMagnitude > 1e-8f; it++)
            {
                var o = start + applied + Vector3.up * 0.6f; float len = step.magnitude; var dir = step / len;
                int n = Physics.SphereCastNonAlloc(o, radius, dir, Hits, len + 0.02f, ~0, QueryTriggerInteraction.Ignore);
                float best = len + 1; Vector3 nrm = Vector3.zero;
                for (int i = 0; i < n; i++)
                {
                    var h = Hits[i]; if (h.collider.attachedRigidbody || h.collider is CharacterController || h.collider.transform.IsChildOf(t)) continue;
                    if (h.distance <= 0f) continue; // già sovrapposto: non bloccare (evita incastri)
                    if (h.distance < best) { best = h.distance; nrm = h.normal; }
                }
                if (best > len) { applied += step; break; }
                float go = Mathf.Max(0, best - 0.02f); applied += dir * go;
                nrm.y = 0; if (nrm.sqrMagnitude < 1e-6f) break; nrm.Normalize();
                var rest = dir * (len - go); step = rest - Vector3.Dot(rest, nrm) * nrm;
            }
            t.position = start + applied; return applied;
        }

        /// <summary>rotation = LookRotation(velocità piana) con smoothing.</summary>
        public static void Face(Transform t, Vector3 applied, float dt, float rate = 12f)
        {
            applied.y = 0; if (applied.sqrMagnitude < 1e-8f) return;
            t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(applied), 1f - Mathf.Exp(-rate * dt));
        }
    }
}
