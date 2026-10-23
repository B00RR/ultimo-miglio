using UnityEngine;
using UltimoMiglio.UI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Kinematic CharacterController (02 §2b): 3 m/s × Scarpe, −10% at MAX stack, 0→max in 0.1 s, brake 80 ms, slerp 12/s.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class NinaController : MonoBehaviour
    {
        public FloatingJoystick Joystick;
        public PackageStack Stack;
        public Animator Animator;            // params: Speed (float), Carry (bool)
        [Tooltip("Debug/capture: overrides joystick when non-zero")] public Vector3 ScriptedInput;
        const float AccelTime = 0.1f, BrakeTime = 0.08f, TurnRate = 12f, Gravity = -20f;
        CharacterController _cc; Vector3 _vel; float _vy;
        Vector3 _muro1, _muro2;
        static readonly int SpeedId = Animator.StringToHash("Speed"), CarryId = Animator.StringToHash("Carry"), DeliverId = Animator.StringToHash("Deliver"), CheerId = Animator.StringToHash("Cheer");
        public void Deliver() { if (Animator) Animator.SetTrigger(DeliverId); }
        public void Cheer() { if (Animator) Animator.SetTrigger(CheerId); }

        void Awake() { _cc = GetComponent<CharacterController>(); }
        public static readonly Vector3 SafeSpawn = new(0f, 0.05f, -1f);
        /// <summary>#60: allo start (anche dopo reset storage) Nina deve stare nell'area giocabile, mai dietro il muro N o dentro un collider.</summary>
        void Start() { EnsurePlayableSpawn(); }
        public void EnsurePlayableSpawn()
        {
            var p = transform.position;
            bool outside = p.x < -8.6f || p.x > 7.0f || p.z < -10.2f || p.z > 7.0f || p.y < -1f || p.y > 2f;
            if (!outside) return;
            bool was = _cc && _cc.enabled; if (_cc) _cc.enabled = false;
            transform.SetPositionAndRotation(SafeSpawn, Quaternion.identity); _vel = Vector3.zero; _vy = 0;
            if (_cc) _cc.enabled = was;
        }

        void Update()
        {
            if (!GameRoot.Ready) return;
            var eco = GameRoot.I.Eco;
            Vector3 dir = ScriptedInput.sqrMagnitude > 0 ? Vector3.ClampMagnitude(ScriptedInput, 1) : (Joystick ? Joystick.WorldDirection : Vector3.zero);
#if UM_NINA_OWN_KEYBOARD // FloatingJoystick (UI) now reads WASD/arrows/mouse: avoid double input
            if (dir == Vector3.zero) { var k = new Vector3(Input2.H, 0, Input2.V); dir = Quaternion.Euler(0, 45, 0) * Vector3.ClampMagnitude(k, 1); }
#endif
            float max = (float)eco.NinaSpeed(Stack ? Stack.Count : 0);
            Vector3 target = dir * max;
            float rate = (target.sqrMagnitude > _vel.sqrMagnitude ? max / AccelTime : max / BrakeTime) * Time.deltaTime;
            _vel = Vector3.MoveTowards(_vel, target, rate);
            _vy = _cc.isGrounded ? -1f : _vy + Gravity * Time.deltaTime;
            // #274: lo spigolo del BoxCollider (vaso tondo colpito in diagonale) azzera il Move.
            // La normale arriva dal frame prima; se questo Move resta fermo, un secondo passo scivola lungo il muro.
            Vector3 wish = _vel;
            if (_muro1.sqrMagnitude > 1e-8f)
                wish = Sblocca(_vel, _muro1, _muro2);
            _muro1 = _muro2 = Vector3.zero;
            Vector3 prima = transform.position;
            _cc.Move((wish + Vector3.up * _vy) * Time.deltaTime);
            Vector3 fatto = transform.position - prima; fatto.y = 0f;
            float atteso = new Vector3(_vel.x, 0f, _vel.z).magnitude * Time.deltaTime;
            if (atteso > 0.02f && fatto.sqrMagnitude < atteso * atteso * 0.12f && _muro1.sqrMagnitude > 1e-8f)
            {
                Vector3 slide = Sblocca(_vel, _muro1, _muro2);
                if (slide.sqrMagnitude > _vel.sqrMagnitude * 0.04f)
                    _cc.Move(new Vector3(slide.x, 0f, slide.z) * Time.deltaTime);
            }
            // Le normali di questo Move restano per il frame dopo (riga wish). In aria il Move non le riscrive e il frame dopo le azzera.
            // B3 P2: non uscire sotto l'HUD in alto — clamp playable (Arrivi z≈6 resta raggiungibile)
            var p = transform.position;
            float nx = Mathf.Clamp(p.x, -8.6f, 7.0f) /* #58: muro E reale a x 7.6 */;
            float nz = Mathf.Clamp(p.z, -10.2f, 7.0f);
            if (Mathf.Abs(nx - p.x) > 0.001f || Mathf.Abs(nz - p.z) > 0.001f)
            {
                bool was = _cc.enabled; _cc.enabled = false;
                transform.position = new Vector3(nx, p.y, nz);
                _cc.enabled = was;
                if (nx != p.x) _vel.x = 0;
                if (nz != p.z) _vel.z = 0;
            }
            if (_vel.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_vel), 1 - Mathf.Exp(-TurnRate * Time.deltaTime));
            if (Animator) { Animator.SetFloat(SpeedId, _vel.magnitude); Animator.SetBool(CarryId, Stack && Stack.Count > 0); }
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!hit.collider || hit.collider.isTrigger || hit.normal.y > 0.45f) return;
            Vector3 n = hit.normal; n.y = 0f;
            if (n.sqrMagnitude < 1e-8f) return;
            n.Normalize();
            if (_muro1.sqrMagnitude < 1e-8f) _muro1 = n;
            else if (Vector3.Dot(_muro1, n) < 0.85f) _muro2 = n;
        }

        /// <summary>
        /// #274: velocità orizzontale che non entra nel muro. Su una faccia la diagonale scivola e la spinta dritta si ferma.
        /// Su uno spigolo (normale obliqua, o due facce) se la proiezione è nulla Kaia prende un lato, invece di restare incastrata.
        /// </summary>
        public static Vector3 Sblocca(Vector3 velocita, Vector3 normale1, Vector3 normale2)
        {
            velocita.y = 0f;
            if (velocita.sqrMagnitude < 1e-8f) return velocita;
            Vector3 slide = Lungo(velocita, normale1);
            if (normale2.sqrMagnitude > 1e-8f && slide.sqrMagnitude < velocita.sqrMagnitude * 0.04f)
                slide = Lungo(velocita, normale2);
            if (slide.sqrMagnitude >= velocita.sqrMagnitude * 0.04f) return slide;
            if (!NormaleDiSpigolo(normale1) && !NormaleDiSpigolo(normale2)) return slide;
            Vector3 n = normale2.sqrMagnitude > 1e-8f ? normale1 + normale2 : normale1;
            n.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, n);
            side.y = 0f;
            if (side.sqrMagnitude < 1e-8f) return slide;
            return side.normalized * velocita.magnitude;
        }

        /// <summary>Normale di uno spigolo di box allineato agli assi: ha componente sia in X sia in Z.</summary>
        public static bool NormaleDiSpigolo(Vector3 n)
        {
            n.y = 0f;
            if (n.sqrMagnitude < 1e-8f) return false;
            n.Normalize();
            return Mathf.Abs(n.x) > 0.35f && Mathf.Abs(n.z) > 0.35f;
        }

        static Vector3 Lungo(Vector3 velocita, Vector3 normale)
        {
            normale.y = 0f;
            if (normale.sqrMagnitude < 1e-8f) return velocita;
            normale.Normalize();
            float into = Vector3.Dot(velocita, normale);
            if (into >= -0.02f) return velocita;
            return velocita - normale * into;
        }
    }

#if UNITY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE
    static class Input2 // keyboard (WASD + arrows) via Input System: editor + WebGL/desktop builds
    {
        public static float H { get { var k = UnityEngine.InputSystem.Keyboard.current; return k == null ? 0 : (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0); } }
        public static float V { get { var k = UnityEngine.InputSystem.Keyboard.current; return k == null ? 0 : (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0); } }
    }
#endif
}
