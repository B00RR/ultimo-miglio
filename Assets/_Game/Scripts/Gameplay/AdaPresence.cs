using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>#118 – Nonna Ada 3D in scena (posata a fianco del Bancone da SliceSceneBuilder via AdaScenePlacement).
    /// Controller di #93 (chr_nonna_ada.controller): Speed (Idle 0 / Walk 1.2) e bool Talk. Default Idle.
    /// Hook al tutorial: TutorialManager.StepChanged → Talk per TalkSeconds e giro morbido verso Nina; a tutorial finito resta in Idle.
    /// TODO(Animator): Walk e pose dedicate (#154) — oggi Ada non si sposta (Speed resta 0); se servono percorsi, impostare Speed qui.</summary>
    public sealed class AdaPresence : MonoBehaviour
    {
        public Animator Animator;
        public float TalkSeconds = 2.5f;
        public float TurnDegPerSec = 180f;
        static readonly int SpeedId = Animator.StringToHash("Speed"), TalkId = Animator.StringToHash("Talk");
        float _talkUntil; Quaternion _rest; bool _hasTalk, _hasSpeed;

        void Awake()
        {
            if (!Animator) Animator = GetComponentInChildren<Animator>();
            _rest = transform.rotation;
            if (Animator && Animator.runtimeAnimatorController)
                foreach (var p in Animator.parameters) { if (p.nameHash == TalkId) _hasTalk = true; if (p.nameHash == SpeedId) _hasSpeed = true; }
            if (_hasSpeed) Animator.SetFloat(SpeedId, 0f);
        }
        void OnEnable() { TutorialManager.StepChanged += OnStep; }
        void OnDisable() { TutorialManager.StepChanged -= OnStep; }

        void OnStep(TutorialManager.Step s)
        {
            if (s >= TutorialManager.Step.Chiuso) { _talkUntil = 0; return; }
            _talkUntil = Time.time + TalkSeconds;
        }

        void Update()
        {
            bool talking = Time.time < _talkUntil;
            if (_hasTalk) Animator.SetBool(TalkId, talking);
            var target = _rest;
            if (talking && GameRoot.I && GameRoot.I.Nina)
            {
                var d = GameRoot.I.Nina.transform.position - transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f) target = Quaternion.LookRotation(d);
            }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnDegPerSec * Time.deltaTime);
        }
    }
}
