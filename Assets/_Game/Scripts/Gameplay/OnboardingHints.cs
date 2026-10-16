using UnityEngine;
using UnityEngine.UIElements;
using UltimoMiglio.UI;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Aiuti progressivi dell'onboarding (#94, ordine della PR #90). Il timer conta il tempo SENZA PROGRESSO verso il gate:
    /// progresso = avvicinarsi di almeno 1 m al bersaglio (senza bersaglio: muoversi) oppure completare l'azione (cambio passo).
    /// Si azzera a ogni progresso e a ogni cambio passo.
    ///   10 s → livello 1: ripete il biglietto di Ada + il bersaglio pulsa (InteractionPoint.HintPulse);
    ///   20 s → livello 2: freccia a bordo schermo se il bersaglio è fuori camera, altrimenti mano fantasma (zona joystick, sparisce al tocco);
    ///   40 s → livello 3: camera breve ≤ 1,5 s sul bersaglio (andata e ritorno incluse), joystick libero, UNA sola volta per passo.
    /// Ogni livello scrive un log "[Onb] hint step=… level=…". Le comparse (HudReveal.Revealed) chiedono la camera breve.</summary>
    public sealed class OnboardingHints : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public float Level1Sec = 10f, Level2Sec = 20f, Level3Sec = 40f, ProgressMeters = 1f, HintPeekSec = 1.5f;
        [Tooltip("Camera breve sugli elementi rivelati (s, 2,5–3)")] public float RevealPeekSec = 2.75f;

        TutorialManager _tut; EdgeArrow _edge; HudController _hud; InteractionPoint _ip;
        VisualElement _hand; float _idle, _best = float.MaxValue; int _level; bool _handOn, _peekDone; Vector3 _lastNina; bool _hasLast;

        void OnEnable() { Active = true; TutorialManager.StepChanged += OnStep; HudReveal.Revealed += OnRevealed; }
        void OnDisable() { Active = false; TutorialManager.StepChanged -= OnStep; HudReveal.Revealed -= OnRevealed; Clear(); }

        void OnStep(TutorialManager.Step s) { _idle = 0; _best = float.MaxValue; _peekDone = false; Clear(); }

        void OnRevealed(string id, Vector3? at)
        {
            if (at.HasValue && RevealPeekSec > 0f) CameraPeek.Request(at.Value, RevealPeekSec);
        }

        void Clear()
        {
            if (_level > 0) OnbLog.Write("hint reset");
            _level = 0;
            if (_ip) { _ip.HintPulse = false; _ip = null; }
            if (_edge) _edge.Clear();
            _handOn = false;
            if (_hand != null) _hand.RemoveFromClassList("ghost-hand--on");
            if (_hud && _hud.Root != null) _hud.Root.RemoveFromClassList("onb-hand");
        }

        void Update()
        {
            if (!GameRoot.Ready) return;
            if (!_tut) _tut = TutorialManager.I; if (!_tut) return;
            if (!_hud) _hud = FindAnyObjectByType<HudController>();
            if (!_edge) _edge = FindAnyObjectByType<EdgeArrow>(FindObjectsInactive.Include);
            if (_tut.Current >= TutorialManager.Step.Fine) { if (_level > 0) Clear(); return; }

            var nina = GameRoot.I.Nina; if (!nina) return;
            var target = _tut.CurrentTarget; var np = nina.transform.position;
            bool moved = _hasLast && (np - _lastNina).sqrMagnitude > 0.0004f; _lastNina = np; _hasLast = true;
            bool progress;
            if (target)
            {
                float d = Vector3.Distance(np, target.position);
                progress = d < _best - ProgressMeters; if (progress || _best == float.MaxValue) _best = d;
            }
            else progress = moved;
            if (progress) { _idle = 0; if (_level > 0) Clear(); return; }
            _idle += Time.deltaTime;

            if (_level < 1 && _idle >= Level1Sec)
            {
                _level = 1; Log();
                var txt = _tut.CurrentText; if (_hud && !string.IsNullOrEmpty(txt)) _hud.ShowAdaNote(txt); // testo ripetuto (ricompare con animazione)
                _ip = target ? target.GetComponentInChildren<InteractionPoint>() : null; if (_ip) _ip.HintPulse = true;
            }
            if (_level < 2 && _idle >= Level2Sec)
            {
                _level = 2;
                if (target && !OnScreen(target.position)) { if (_edge) _edge.SetTarget(target, _tut.CurrentObjectiveText); Log("freccia"); }
                else { EnsureHand(); _handOn = _hand != null; if (_handOn && _hud) _hud.Root.AddToClassList("onb-hand"); Log("mano"); }
            }
            if (_level < 3 && _idle >= Level3Sec)
            {
                _level = 3;
                if (target && !_peekDone) { _peekDone = true; CameraPeek.Request(target.position, HintPeekSec); Log("camera"); } else Log("camera già usata");
            }
            if (_handOn) AnimateHand(target, np);
        }

        void Log(string what = null) => OnbLog.Write($"hint step={_tut.Current} level={_level}{(what != null ? " " + what : "")} idle={_idle:0.0}s");

        static bool OnScreen(Vector3 w)
        {
            var cam = Camera.main; if (!cam) return false;
            var v = cam.WorldToViewportPoint(w);
            return v.z > 0 && v.x > 0.05f && v.x < 0.95f && v.y > 0.05f && v.y < 0.95f;
        }

        void EnsureHand()
        {
            if (_hand != null || !_hud || _hud.Root == null) return;
            _hand = new VisualElement { name = "ghost-hand", pickingMode = PickingMode.Ignore }; _hand.AddToClassList("ghost-hand");
            _hud.Root.Add(_hand);
        }

        /// <summary>Loop 1,4 s: la mano parte dal centro della zona joystick e trascina 56 dp verso il bersaglio; sparisce se il joystick è visibile (dito sullo schermo).</summary>
        void AnimateHand(Transform target, Vector3 ninaPos)
        {
            if (_hand == null) return;
            var joy = _hud.Root.Q("joystick-base");
            bool touching = joy != null && joy.resolvedStyle.display == DisplayStyle.Flex && joy.resolvedStyle.opacity > 0.05f;
            _hand.EnableInClassList("ghost-hand--on", !touching);
            Vector2 dir = Vector2.up;
            var cam = Camera.main;
            if (target && cam)
            {
                Vector3 a = cam.WorldToScreenPoint(ninaPos), b = cam.WorldToScreenPoint(target.position);
                var d = new Vector2(b.x - a.x, -(b.y - a.y)); // schermo → pannello (y verso il basso)
                if (d.sqrMagnitude > 1f) dir = d.normalized;
            }
            else dir = new Vector2(0f, -1f);
            float t = Mathf.Repeat(Time.unscaledTime, 1.4f) / 1.4f, k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.6f));
            _hand.style.translate = new Translate(dir.x * 56f * k, dir.y * 56f * k, 0);
            _hand.style.opacity = t < 0.85f ? 0.9f : 0.9f * (1f - (t - 0.85f) / 0.15f);
        }
    }
}
