using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>#58/#59: movimento Transform con sweep (scivola sui collider statici) e facing dalla velocità piana effettiva.</summary>
    public static class AgentMove
    {
        static readonly RaycastHit[] Hits = new RaycastHit[8];
        static readonly Collider[] Over = new Collider[8];
        /// <summary>#111: true se l'ultimo Slide è stato fermato da un altro personaggio (Nina, cliente, NONNO): niente teletrasporto anti-stallo.</summary>
        public static bool LastBlockedByAgent; public static Collider LastBlocker;
        /// <summary>#111: Nina (CharacterController) e i PNG (Customer, NonnoRobot) sono "corpi": non si compenetrano.</summary>
        public static bool IsAgent(Collider c) => c is CharacterController || c.GetComponentInParent<Customer>() || c.GetComponentInParent<NonnoRobot>();

        /// <summary>#111: se t è già dentro un altro personaggio (es. Nina gli è arrivata addosso), il PNG si sposta fuori sul piano XZ.</summary>
        static void Separate(Transform t)
        {
            var self = t.GetComponent<Collider>(); if (!self) return;
            int n = Physics.OverlapSphereNonAlloc(self.bounds.center, Mathf.Max(self.bounds.extents.x, self.bounds.extents.z) + 0.05f, Over, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var o = Over[i]; if (o == self || o.transform.IsChildOf(t) || !IsAgent(o)) continue;
                if (Physics.ComputePenetration(self, self.transform.position, self.transform.rotation, o, o.transform.position, o.transform.rotation, out var dir, out var dist))
                { dir.y = 0; if (dir.sqrMagnitude > 1e-6f) { var mv = dir.normalized * Mathf.Min(dist, 0.03f); if (!StaticAhead(t, self, mv)) t.position += mv; } } // mai spinto dentro un muro
            }
        }

        static readonly RaycastHit[] SepHits = new RaycastHit[8];
        static bool StaticAhead(Transform t, Collider self, Vector3 mv)
        {
            var c = self.bounds.center; float r = Mathf.Max(self.bounds.extents.x, self.bounds.extents.z);
            int n = Physics.SphereCastNonAlloc(c, r, mv.normalized, SepHits, mv.magnitude + 0.02f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) { var h = SepHits[i].collider; if (h == self || h.transform.IsChildOf(t) || IsAgent(h) || h.attachedRigidbody) continue; if (SepHits[i].distance > 0f) return true; }
            return false;
        }

        /// <summary>#111: aggiramento di un solido statico: prova uno spostamento laterale (sinistra, poi destra) con Slide, quindi sempre con collisioni.</summary>
        public static Vector3 Sidestep(Transform t, Vector3 step, float radius)
        {
            step.y = 0; var side = new Vector3(-step.z, 0, step.x);
            var a = Slide(t, side, radius); if (a.sqrMagnitude > 1e-10f) return a;
            return Slide(t, -side, radius);
        }

        /// <summary>Muove t di step (piano XZ) senza attraversare collider solidi; ritorna lo spostamento applicato.</summary>
        public static Vector3 Slide(Transform t, Vector3 step, float radius = 0.35f)
        {
            LastBlockedByAgent = false; Separate(t);
            step.y = 0; var start = t.position; var applied = Vector3.zero;
            for (int it = 0; it < 2 && step.sqrMagnitude > 1e-8f; it++)
            {
                var o = start + applied + Vector3.up * 0.6f; float len = step.magnitude; var dir = step / len;
                int n = Physics.SphereCastNonAlloc(o, radius, dir, Hits, len + 0.02f, ~0, QueryTriggerInteraction.Ignore);
                float best = len + 1; Vector3 nrm = Vector3.zero;
                for (int i = 0; i < n; i++)
                {
                    var h = Hits[i]; if (h.collider.transform.IsChildOf(t)) continue;
                    bool agent = IsAgent(h.collider);
                    if (!agent && h.collider.attachedRigidbody) continue;
                    if (h.distance <= 0f)
                    {
                        // già sovrapposto: un solido statico non blocca (evita incastri); un personaggio sì, se ci si va incontro (#111)
                        if (!agent || Vector3.Dot(h.collider.bounds.center - o, dir) <= 0f) continue;
                        best = 0f; nrm = Vector3.zero; LastBlockedByAgent = true; LastBlocker = h.collider; break;
                    }
                    if (h.distance < best) { best = h.distance; nrm = h.normal; LastBlockedByAgent = agent; LastBlocker = h.collider; }
                }
                if (best > len) { applied += step; break; }
                float go = Mathf.Max(0, best - 0.02f); applied += dir * go;
                if (LastBlockedByAgent) break; // davanti a un personaggio si aspetta, non si gira attorno spingendo
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
