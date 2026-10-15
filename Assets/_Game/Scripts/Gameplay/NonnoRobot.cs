using UnityEngine;
using UnityEngine.AI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>NONNO forklift: restocks Arrivals→shelf. Uses NavMesh when available; otherwise walks with Transform
    /// so WebGL builds without a valid bake still work after hire.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NonnoRobot : MonoBehaviour
    {
        public ArrivalsZone Arrivals; public Shelf[] Shelves; public Bancone Bancone; public CustomerQueue Queue;
        public int CarryMax = 3; public Animator Animator;
        public enum WorkState { Idle, ToArrivals, Loading, ToShelf, Unloading }
        public WorkState State { get; private set; }
        public int CarryCount => _carry;
        public bool IsWorking => GameRoot.Ready && GameRoot.I.Eco.S.NonnoLevel >= 0 && State != WorkState.Idle;

        NavMeshAgent _agent; int _carry; ParcelColor _color; Shelf _target; float _timer, _selfServeAcc;
        bool _announced, _warped; Vector3 _dest; bool _useNav;
        static readonly int SpeedId = Animator.StringToHash("Speed"), LiftId = Animator.StringToHash("Lift"), DropId = Animator.StringToHash("Drop"), HappyId = Animator.StringToHash("Happy");
        Renderer[] _renderers; bool _shown = true;
        const float WalkSpeed = 2.4f;

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>(); _agent.speed = WalkSpeed; _agent.angularSpeed = 360; _agent.stoppingDistance = 0.45f;
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (Animator) Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        void LateUpdate()
        {
            if (!Animator || !Animator.isActiveAndEnabled) return;
            float spd = _useNav && _agent.enabled && _agent.isOnNavMesh ? _agent.velocity.magnitude : (_dest - transform.position).magnitude > 0.05f ? WalkSpeed : 0f;
            Animator.SetFloat(SpeedId, spd); Animator.SetBool(LiftId, _carry > 0);
        }
        public void Happy() { if (Animator) Animator.SetTrigger(HappyId); }

        void EnsureOnMesh()
        {
            if (_warped) return; _warped = true;
            if (NavMesh.SamplePosition(transform.position, out var hit, 4f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position); _useNav = _agent.isOnNavMesh;
            }
            else { _useNav = false; if (_agent.enabled) _agent.enabled = false; }
        }

        void Update()
        {
            if (!GameRoot.Ready || !Arrivals || !Queue || !Bancone || Shelves == null || Shelves.Length == 0) return;
            var eco = GameRoot.I.Eco;
            bool hired = eco.S.NonnoLevel >= 0;
            if (_shown != hired) { _shown = hired; foreach (var r in _renderers) if (r) r.enabled = hired; }
            if (!hired) { State = WorkState.Idle; return; }
            EnsureOnMesh();
            if (!_announced) { _announced = true; Happy(); AudioManager.I?.Play(AudioManager.Sfx.NonnoStart); State = WorkState.Idle; }

            _selfServeAcc += (float)(eco.NonnoSelfServicePpm / 60.0) * Time.deltaTime;
            if (_selfServeAcc >= 1f && Queue.Waiting > 1) { _selfServeAcc -= 1f; var c = Queue.Head; if (c != null && c.AtCounter) Bancone.Serve(c); }

            float perParcel = 60f / Mathf.Max(0.1f, (float)eco.NonnoRestockPpm);
            switch (State)
            {
                case WorkState.Idle: Go(Arrivals.transform.position, WorkState.ToArrivals); break;
                case WorkState.ToArrivals:
                    if (StepMove()) { State = WorkState.Loading; _timer = 0; }
                    break;
                case WorkState.Loading:
                    if ((_timer += Time.deltaTime) >= perParcel)
                    {
                        _timer = 0;
                        if (Arrivals.TakeForRobot(out var col)) { if (_carry == 0) _color = col; _carry++; }
                        else if (_carry == 0) { /* nothing to take yet: wait */ }
                    }
                    if (_carry >= CarryMax || (_carry > 0 && _timer > perParcel * 2f && Arrivals.Stock <= 0))
                    { _target = Pick(); Go(_target.transform.position, WorkState.ToShelf); }
                    break;
                case WorkState.ToShelf:
                    if (StepMove()) { State = WorkState.Unloading; _timer = 0; }
                    break;
                case WorkState.Unloading:
                    if ((_timer += Time.deltaTime) >= 0.25f)
                    {
                        _timer = 0; _target.Add(1); _carry--;
                        if (Animator) Animator.SetTrigger(DropId);
                        if (Random.value < 0.15f) AudioManager.I?.Play(AudioManager.Sfx.NonnoMumble);
                    }
                    if (_carry <= 0) State = WorkState.Idle;
                    break;
            }
        }

        Shelf Pick() { Shelf best = Shelves[0]; foreach (var s in Shelves) if (s.Count < best.Count) best = s; return best; }

        void Go(Vector3 p, WorkState next)
        {
            _dest = p; State = next;
            if (_useNav && _agent.enabled)
            {
                if (!_agent.isOnNavMesh) { EnsureOnMesh(); }
                if (_agent.isOnNavMesh) { _agent.isStopped = false; _agent.SetDestination(p); return; }
            }
            _useNav = false; if (_agent && _agent.enabled) _agent.enabled = false;
        }

        bool StepMove()
        {
            if (_useNav && _agent.enabled && _agent.isOnNavMesh)
                return !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.55f;
            var d = _dest - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.35f) return true;
            transform.position += Vector3.ClampMagnitude(d.normalized * WalkSpeed * Time.deltaTime, d.magnitude);
            if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
            return false;
        }
    }
}
