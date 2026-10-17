using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>#256: punto di passaggio navigabile (varco, arco della Cassa...) per la guida del tutorial. Nessuna NavMesh a runtime (WebGL):
    /// la mano di OnboardingHints punta al prossimo varco da attraversare quando la linea retta verso il bersaglio è chiusa da un solido.
    /// I marker li piazza il layout (SliceSceneBuilder / DepotKit, PR del 3D Artist); senza marker la guida resta in linea retta.</summary>
    public sealed class VarcoMarker : MonoBehaviour
    {
        static readonly List<VarcoMarker> _tutti = new();
        /// <summary>Marker attivi in scena (lista condivisa: non modificarla).</summary>
        public static IReadOnlyList<VarcoMarker> Tutti => _tutti;
        /// <summary>Distanza (m) entro cui il varco si considera attraversato e la guida passa oltre.</summary>
        public float RaggioPassaggio = 0.8f;
        void OnEnable() { if (!_tutti.Contains(this)) _tutti.Add(this); }
        void OnDisable() { _tutti.Remove(this); }

        static readonly RaycastHit[] Hits = new RaycastHit[16];
        /// <summary>Raggio della sonda (Kaia r 0,30, un filo meno per non chiudere i varchi stretti).</summary>
        const float Sonda = 0.25f;

        /// <summary>True se tra a e b (piano XZ, altezza busto) c'è un solido statico che chiude il passaggio. Ignora trigger, personaggi,
        /// rigidbody e i collider sotto <paramref name="ignora"/> (il bersaglio stesso). Gli ultimi <paramref name="margineFine"/> m non contano.</summary>
        public static bool Chiuso(Vector3 a, Vector3 b, Transform ignora, float margineFine = 0.6f)
        {
            a.y = b.y = 0.8f; var d = b - a; float len = d.magnitude - margineFine; if (len <= 0.05f) return false;
            int n = Physics.SphereCastNonAlloc(a, Sonda, d / d.magnitude, Hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = Hits[i].collider; if (!c || c.attachedRigidbody || AgentMove.IsAgent(c)) continue;
                if (ignora && c.transform.IsChildOf(ignora)) continue;
                if (Hits[i].distance <= 0f)
                {   // già a contatto (Kaia contro un muro): chiude solo se il solido sta davanti, nella direzione del bersaglio
                    var cp = c.ClosestPoint(a) - a; cp.y = 0;
                    if (cp.sqrMagnitude > 1e-6f && Vector3.Dot(cp, d) <= 0f) continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>Prossimo punto da raggiungere verso <paramref name="bersaglio"/>: il bersaglio stesso se la linea retta è libera
        /// (o non ci sono marker), altrimenti il varco non ancora attraversato, raggiungibile in linea retta, con il giro più corto
        /// (da → varco → bersaglio). <paramref name="passati"/>: bit dei varchi già attraversati verso questo bersaglio (azzerarlo quando cambia),
        /// così la guida non torna indietro su un varco appena passato.</summary>
        public static Vector3 ProssimaTappa(Vector3 da, Vector3 bersaglio, Transform ignora, ref int passati, out VarcoMarker varco)
        {
            varco = null;
            for (int i = 0; i < _tutti.Count && i < 32; i++)
            { var m = _tutti[i]; if (!m) continue; var fl = m.transform.position - da; fl.y = 0; if (fl.sqrMagnitude < m.RaggioPassaggio * m.RaggioPassaggio) passati |= 1 << i; }
            if (_tutti.Count == 0 || !Chiuso(da, bersaglio, ignora)) return bersaglio;
            float best = float.MaxValue, riserva = float.MaxValue; VarcoMarker vRiserva = null;
            for (int i = 0; i < _tutti.Count && i < 32; i++)
            {
                var m = _tutti[i]; if (!m || (passati & (1 << i)) != 0) continue; var p = m.transform.position;
                var fl = p - da; fl.y = 0;
                float costo = fl.magnitude + Flat(p, bersaglio);
                if (Chiuso(da, p, null, 0f)) { if (costo < riserva) { riserva = costo; vRiserva = m; } continue; }
                if (costo < best) { best = costo; varco = m; }
            }
            if (!varco) varco = vRiserva; // nessun varco in vista: meglio il varco più conveniente (la scivolata sui muri ci porta) che dritto contro il muro
            return varco ? varco.transform.position : bersaglio;
        }
        static float Flat(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
    }
}
