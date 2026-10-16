using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    // Run before NavMeshAgent.OnEnable soft-fails on WebGL without baked mesh
    /// <summary>NONNO forklift without NavMeshAgent (WebGL has no baked NavMesh at runtime).
    /// Pure Transform movement: Arrivals → shelf restock + Bancone self-service.
    /// Load until capacity OR timeout, then deliver — never idle forever on Arrivi.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class NonnoRobot : MonoBehaviour
    {
        public ArrivalsZone Arrivals; public Shelf[] Shelves; public Bancone Bancone; public CustomerQueue Queue;
        /// <summary>#111: raggio del corpo (CapsuleCollider e sweep).</summary>
        public const float BodyRadius = 0.45f;
        public int CarryMax = 3; public Animator Animator;
        public enum WorkState { Idle, ToArrivals, Loading, ToShelf, Unloading }
        public WorkState State { get; private set; }
        public int CarryCount => _carry;
        public bool IsWorking => GameRoot.Ready && GameRoot.I.Eco.S.NonnoLevel >= 0 && State != WorkState.Idle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BootStripAgents() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneStripAgents() => StripAllNavMeshAgentsInScene();

        int _carry; Shelf _target; float _timer, _selfServeAcc, _loadElapsed, _stuckT; bool _announced; Vector3 _dest, _lastPos;
        static readonly int SpeedId = Animator.StringToHash("Speed"), LiftId = Animator.StringToHash("Lift"), DropId = Animator.StringToHash("Drop"), HappyId = Animator.StringToHash("Happy");
        Renderer[] _renderers; bool _shown = true;
        public const float WalkSpeed = 2.6f;
        float _curSpeed; // #59: velocità reale -> Animator Speed
        /// <summary>Max seconds loading at Arrivi before departing with whatever is carried (forces progress).</summary>
        const float LoadTimeout = 6f;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (Animator) { Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; Animator.applyRootMotion = false; }
            StripNavMeshAgents(gameObject);
        }
        void LateUpdate()
        {
            if (!Animator || !Animator.isActiveAndEnabled) return;
            float spd = (_dest - transform.position).sqrMagnitude > 0.08f && State is WorkState.ToArrivals or WorkState.ToShelf ? Mathf.Max(_curSpeed, 0.5f) : 0f;
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

            // #188: banco self-service = 1 pacco venduto per cliente. Prima: (1) l'accumulatore cresceva senza limite a coda vuota e,
            // al ritorno dei clienti, NONNO ne serviva uno per frame; (2) la vendita non consumava nessun pacco (monete senza consegna).
            _selfServeAcc = Mathf.Min(1f, _selfServeAcc + (float)(eco.NonnoSelfServicePpm / 60.0) * Time.deltaTime);
            if (_selfServeAcc >= 1f && Queue.Waiting > 1)
            {
                var c = Queue.Head;
                if (Bancone.InAttesa(c) && TakeFromShelf()) { _selfServeAcc = 0f; Bancone.Serve(c); }
            }

            float perParcel = 60f / Mathf.Max(0.1f, (float)eco.NonnoRestockPpm);
            // Cap per-parcel so a full CarryMax never exceeds LoadTimeout under normal ppm
            perParcel = Mathf.Min(perParcel, LoadTimeout / Mathf.Max(1, CarryMax));
            switch (State)
            {
                case WorkState.Idle:
                    Go(Arrivals.transform.position, WorkState.ToArrivals);
                    break;
                case WorkState.ToArrivals:
                    if (StepMove()) { State = WorkState.Loading; _timer = 0; _loadElapsed = 0; }
                    break;
                case WorkState.Loading:
                    _loadElapsed += Time.deltaTime;
                    if ((_timer += Time.deltaTime) >= perParcel)
                    {
                        _timer = 0;
                        if (Arrivals.TakeForRobot(out _)) _carry++;
                    }
                    bool full = _carry >= CarryMax;
                    bool stockOut = _carry > 0 && Arrivals.Stock <= 0;
                    bool timedOut = _loadElapsed >= LoadTimeout;
                    if (timedOut && _carry == 0 && Arrivals.Stock > 0 && Arrivals.TakeForRobot(out _))
                        _carry = 1; // force at least one parcel so we leave Arrivi
                    if (full || stockOut || (timedOut && _carry > 0))
                    {
                        _target = Pick();
                        if (_target != null)
                            Go(ShelfApproach(_target), WorkState.ToShelf);
                        else
                            State = WorkState.Idle;
                    }
                    else if (timedOut && _carry == 0)
                    {
                        // Arrivi empty — wait for refill via Idle→retry (don't spin forever in Loading)
                        State = WorkState.Idle;
                    }
                    break;
                case WorkState.ToShelf:
                    if (StepMove()) { State = WorkState.Unloading; _timer = 0; }
                    break;
                case WorkState.Unloading:
                    if (_target == null) { _carry = 0; State = WorkState.Idle; break; }
                    if ((_timer += Time.deltaTime) >= 0.22f)
                    {
                        _timer = 0;
                        if (_carry > 0)
                        {
                            _target.Add(1); _carry--;
                            if (Animator) Animator.SetTrigger(DropId);
                            GameRoot.I?.Qa?.Log("nonno_restock", string.Format("\"shelf\":\"{0}\",\"count\":{1},\"carry\":{2}", _target.Color, _target.Count, _carry));
                            Debug.Log(string.Format("[NONNO] restock shelf={0} count={1} carry={2}", _target.Color, _target.Count, _carry));
                        }
                        if (Random.value < 0.12f) AudioManager.I?.Play(AudioManager.Sfx.NonnoMumble);
                    }
                    if (_carry <= 0) State = WorkState.Idle;
                    break;
            }
        }

        /// <summary>#188: il pacco venduto da NONNO esce da uno scaffale (quello più pieno); niente pacco = niente vendita.</summary>
        bool TakeFromShelf()
        {
            Shelf best = null;
            foreach (var s in Shelves) if (s && s.Count > 0 && (best == null || s.Count > best.Count)) best = s;
            return best != null && best.Take();
        }

        Shelf Pick()
        {
            Shelf best = null;
            foreach (var s in Shelves)
            {
                if (!s) continue;
                if (best == null || s.Count < best.Count) best = s;
            }
            return best;
        }
        static Vector3 ShelfApproach(Shelf s)
        {
            // Front of shelf zone (toward Arrivi / +Z side of shelf pickup area)
            var p = s.transform.position;
            return new Vector3(p.x, p.y, p.z + 0.6f);
        }
        void Go(Vector3 p, WorkState next) { _dest = p; State = next; _stuckT = 0; _lastPos = transform.position; }
        bool StepMove()
        {
            var d = _dest - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.35f) { _stuckT = 0; return true; }
            var applied = AgentMove.Slide(transform, Vector3.ClampMagnitude(d.normalized * WalkSpeed * Time.deltaTime, d.magnitude), BodyRadius);
            if (AgentMove.LastBlockedByAgent) { _stuckT = 0; _lastPos = transform.position; _curSpeed = 0f; if (d.sqrMagnitude < 2.25f) return true; return false; } // #111: Nina sulla meta: carica/scarica da vicino, mai attraverso
            AgentMove.Face(transform, applied.sqrMagnitude > 1e-8f ? applied : d, Time.deltaTime); // #59
            _curSpeed = Time.deltaTime > 0 ? applied.magnitude / Time.deltaTime : 0f;
            // Anti-stallo #111: incastrato su un solido statico per 2 s aggira di lato con collisioni (niente teletrasporto, mai attraverso i solidi)
            if ((transform.position - _lastPos).sqrMagnitude < 1e-5f) _stuckT += Time.deltaTime;
            else if (applied.sqrMagnitude > 1e-8f) _stuckT = 0;
            if (_stuckT > 2f && d.sqrMagnitude < 1.44f) { _stuckT = 0; return true; } // a ridosso della meta ma fermato da un solido (corpo 0,45 m): carica/scarica da lì
            if (_stuckT > 2f) { var step = Vector3.ClampMagnitude(d.normalized * WalkSpeed * Time.deltaTime, d.magnitude); var sd = AgentMove.Sidestep(transform, step, BodyRadius); if (sd.sqrMagnitude > 1e-10f) { _stuckT = 0; _curSpeed = sd.magnitude / Mathf.Max(Time.deltaTime, 1e-5f); } }
            _lastPos = transform.position;
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
                #if UNITY_EDITOR
                Object.DestroyImmediate(c);
                #else
                Object.Destroy(c);
                #endif
            }
        }
        public static void StripAllNavMeshAgentsInScene()
        {
            foreach (var c in Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c == null) continue;
                if (c.GetType().Name != "NavMeshAgent") continue;
                if (c is Behaviour b) b.enabled = false;
                #if UNITY_EDITOR
                Object.DestroyImmediate(c);
                #else
                Object.Destroy(c);
                #endif
            }
        }
    }
}
