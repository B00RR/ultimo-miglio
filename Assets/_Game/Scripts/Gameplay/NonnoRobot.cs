using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    // Run before NavMeshAgent.OnEnable soft-fails on WebGL without baked mesh
    /// <summary>NONNO forklift without NavMeshAgent (WebGL has no baked NavMesh at runtime).
    /// Pure Transform movement: Arrivals → shelf restock + Bancone self-service.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class NonnoRobot : MonoBehaviour
    {
        public ArrivalsZone Arrivals; public Shelf[] Shelves; public Bancone Bancone; public CustomerQueue Queue;
        public int CarryMax = 3; public Animator Animator;
        public enum WorkState { Idle, ToArrivals, Loading, ToShelf, Unloading }
        public WorkState State { get; private set; }
        public int CarryCount => _carry;
        public bool IsWorking => GameRoot.Ready && GameRoot.I.Eco.S.NonnoLevel >= 0 && State != WorkState.Idle;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BootStripAgents()
        {
            // Prima del load scena: niente da fare su oggetti scena; AfterSceneLoad sotto.
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneStripAgents() => StripAllNavMeshAgentsInScene();

        int _carry; Shelf _target; float _timer, _selfServeAcc; bool _announced; Vector3 _dest;
        static readonly int SpeedId = Animator.StringToHash("Speed"), LiftId = Animator.StringToHash("Lift"), DropId = Animator.StringToHash("Drop"), HappyId = Animator.StringToHash("Happy");
        Renderer[] _renderers; bool _shown = true;
        const float WalkSpeed = 2.6f;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (Animator) Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            StripNavMeshAgents(gameObject);
        }
        void LateUpdate()
        {
            if (!Animator || !Animator.isActiveAndEnabled) return;
            float spd = (_dest - transform.position).sqrMagnitude > 0.08f && State is WorkState.ToArrivals or WorkState.ToShelf ? WalkSpeed : 0f;
            Animator.SetFloat(SpeedId, spd); Animator.SetBool(LiftId, _carry > 0);
        }
        public void Happy() { if (Animator) Animator.SetTrigger(HappyId); }

        void Update()
        {
            if (!GameRoot.Ready || !Arrivals || !Queue || !Bancone || Shelves == null || Shelves.Length == 0) return;
            var eco = GameRoot.I.Eco;
            bool hired = eco.S.NonnoLevel >= 0;
            if (_shown != hired) { _shown = hired; foreach (var r in _renderers) if (r) r.enabled = hired; }
            if (!hired) { State = WorkState.Idle; return; }
            if (!_announced) { _announced = true; Happy(); AudioManager.I?.Play(AudioManager.Sfx.NonnoStart); State = WorkState.Idle; }

            _selfServeAcc += (float)(eco.NonnoSelfServicePpm / 60.0) * Time.deltaTime;
            if (_selfServeAcc >= 1f && Queue.Waiting > 1) { _selfServeAcc -= 1f; var c = Queue.Head; if (c != null && c.AtCounter) Bancone.Serve(c); }

            float perParcel = 60f / Mathf.Max(0.1f, (float)eco.NonnoRestockPpm);
            switch (State)
            {
                case WorkState.Idle: Go(Arrivals.transform.position, WorkState.ToArrivals); break;
                case WorkState.ToArrivals: if (StepMove()) { State = WorkState.Loading; _timer = 0; } break;
                case WorkState.Loading:
                    if ((_timer += Time.deltaTime) >= perParcel)
                    {
                        _timer = 0;
                        if (Arrivals.TakeForRobot(out _)) _carry++;
                    }
                    if (_carry >= CarryMax || (_carry > 0 && Arrivals.Stock <= 0))
                    { _target = Pick(); Go(_target.transform.position, WorkState.ToShelf); }
                    break;
                case WorkState.ToShelf: if (StepMove()) { State = WorkState.Unloading; _timer = 0; } break;
                case WorkState.Unloading:
                    if ((_timer += Time.deltaTime) >= 0.22f)
                    {
                        _timer = 0; _target.Add(1); _carry--;
                        if (Animator) Animator.SetTrigger(DropId);
                        if (Random.value < 0.12f) AudioManager.I?.Play(AudioManager.Sfx.NonnoMumble);
                    }
                    if (_carry <= 0) State = WorkState.Idle;
                    break;
            }
        }

        Shelf Pick() { Shelf best = Shelves[0]; foreach (var s in Shelves) if (s.Count < best.Count) best = s; return best; }
        void Go(Vector3 p, WorkState next) { _dest = p; State = next; }
        bool StepMove()
        {
            var d = _dest - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.4f) return true;
            transform.position += Vector3.ClampMagnitude(d.normalized * WalkSpeed * Time.deltaTime, d.magnitude);
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 12f * Time.deltaTime);
            return false;
        }

        /// <summary>Remove any NavMeshAgent on this hierarchy. Never enable AI pathfinding on WebGL.</summary>
        public static void StripNavMeshAgents(GameObject root)
        {
            if (!root) return;
            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var tn = c.GetType().Name;
                if (tn != "NavMeshAgent") continue;
                if (c is Behaviour b) b.enabled = false;
                Destroy(c);
            }
        }
        public static void StripAllNavMeshAgentsInScene()
        {
            // Una sola passata su tutti i Component (anche inattivi). Nessuna API UnityEngine.AI.
            foreach (var c in Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c == null) continue;
                if (c.GetType().Name != "NavMeshAgent") continue;
                if (c is Behaviour b) b.enabled = false;
                Object.Destroy(c);
            }
        }

    }
}
