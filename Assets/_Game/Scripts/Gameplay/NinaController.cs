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
        static readonly int SpeedId = Animator.StringToHash("Speed"), CarryId = Animator.StringToHash("Carry");

        void Awake() { _cc = GetComponent<CharacterController>(); }

        void Update()
        {
            var eco = GameRoot.I.Eco;
            Vector3 dir = ScriptedInput.sqrMagnitude > 0 ? Vector3.ClampMagnitude(ScriptedInput, 1) : (Joystick ? Joystick.WorldDirection : Vector3.zero);
#if UNITY_EDITOR
            if (dir == Vector3.zero) { var k = new Vector3(Input2.H, 0, Input2.V); dir = Quaternion.Euler(0, 45, 0) * Vector3.ClampMagnitude(k, 1); }
#endif
            float max = (float)eco.NinaSpeed(Stack ? Stack.Count : 0);
            Vector3 target = dir * max;
            float rate = (target.sqrMagnitude > _vel.sqrMagnitude ? max / AccelTime : max / BrakeTime) * Time.deltaTime;
            _vel = Vector3.MoveTowards(_vel, target, rate);
            _vy = _cc.isGrounded ? -1f : _vy + Gravity * Time.deltaTime;
            _cc.Move((_vel + Vector3.up * _vy) * Time.deltaTime);
            if (_vel.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_vel), 1 - Mathf.Exp(-TurnRate * Time.deltaTime));
            if (Animator) { Animator.SetFloat(SpeedId, _vel.magnitude); Animator.SetBool(CarryId, Stack && Stack.Count > 0); }
        }
    }

#if UNITY_EDITOR
    static class Input2 // editor keyboard fallback via Input System
    {
        public static float H { get { var k = UnityEngine.InputSystem.Keyboard.current; return k == null ? 0 : (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0); } }
        public static float V { get { var k = UnityEngine.InputSystem.Keyboard.current; return k == null ? 0 : (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0); } }
    }
#endif
}
