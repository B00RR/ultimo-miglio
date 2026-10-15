using UnityEngine;
using UnityEngine.AI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>NONNO forklift (01 §5): state machine Arrivals → shelf restock at restockPpm, plus self-service sales at the Bancone
    /// (selfServicePpm) for customers Nina is not serving. NavMesh agent, repath only on state change.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NonnoRobot : MonoBehaviour
    {
        public ArrivalsZone Arrivals; public Shelf[] Shelves; public Bancone Bancone; public CustomerQueue Queue;
        public int CarryMax = 3;
        enum St { Idle, ToArrivals, Loading, ToShelf, Unloading }
        St _st; NavMeshAgent _agent; int _carry; ParcelColor _color; Shelf _target; float _timer, _selfServeAcc;
        bool _announced;

        void Awake() { _agent = GetComponent<NavMeshAgent>(); _agent.speed = 2.2f; _agent.angularSpeed = 360; }

        void Update()
        {
            var eco = GameRoot.I.Eco;
            bool hired = eco.S.NonnoLevel >= 0;
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = hired;
            if (!hired) return;
            if (!_announced) { _announced = true; AudioManager.I?.Play(AudioManager.Sfx.NonnoStart); }

            // self-service sales (no coins on floor: they go to the Cassa logic only offline; online they drop at the counter)
            _selfServeAcc += (float)(eco.NonnoSelfServicePpm / 60.0) * Time.deltaTime;
            if (_selfServeAcc >= 1f && Queue.Waiting > 1) { _selfServeAcc -= 1f; var c = Queue.Head; if (c != null && c.AtCounter) Bancone.Serve(c); }

            // restock rate: a full cycle moves CarryMax parcels; cadence limited to restockPpm
            float perParcel = 60f / (float)eco.NonnoRestockPpm;
            switch (_st)
            {
                case St.Idle: Go(Arrivals.transform.position, St.ToArrivals); break;
                case St.ToArrivals: if (Arrived()) { _st = St.Loading; _timer = 0; } break;
                case St.Loading:
                    if ((_timer += Time.deltaTime) >= perParcel) { _timer = 0; if (Arrivals.TakeForRobot(out var col)) { if (_carry == 0) _color = col; _carry++; } }
                    if (_carry >= CarryMax) { _target = Pick(); Go(_target.transform.position, St.ToShelf); }
                    break;
                case St.ToShelf: if (Arrived()) { _st = St.Unloading; _timer = 0; } break;
                case St.Unloading:
                    if ((_timer += Time.deltaTime) >= 0.3f) { _timer = 0; _target.Add(1); _carry--; if (Random.value < 0.15f) AudioManager.I?.Play(AudioManager.Sfx.NonnoMumble); }
                    if (_carry <= 0) _st = St.Idle; break;
            }
        }
        Shelf Pick() { Shelf best = Shelves[0]; foreach (var s in Shelves) if (s.Count < best.Count) best = s; return best; }
        void Go(Vector3 p, St next) { _agent.SetDestination(p); _st = next; }
        bool Arrived() => !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.6f;
    }
}
