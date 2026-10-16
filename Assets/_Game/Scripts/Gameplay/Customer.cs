using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Customer walking to a queue slot; leaves after patience (20 s) at the counter.</summary>
    public sealed class Customer : MonoBehaviour
    {
        /// <summary>#111: raggio del corpo (CapsuleCollider del prefab e sweep).</summary>
        public const float BodyRadius = 0.3f;
        public ParcelColor Wants; public float Speed = 2.2f;
        public Vector3 Target; public bool Leaving; public float Waited;
        public bool AtCounter => !Leaving && (transform.position - Target).sqrMagnitude < 0.36f; // 0.6m — WebGL was stuck at Bancone with 0.2m
        public CustomerQueue Owner;
        static readonly int SpeedId = Animator.StringToHash("Speed"), PatienceId = Animator.StringToHash("Patience"), PayId = Animator.StringToHash("Pay"), AngryId = Animator.StringToHash("Angry");
        Animator _an; float _stuck;
        Animator Anim() { if (!_an) _an = GetComponentInChildren<Animator>(); return _an; }
        /// <summary>chr_cliente.controller: Pay when served, Angry when patience ran out.</summary>
        public void OnLeave(bool served) { var a = Anim(); if (a && a.runtimeAnimatorController) a.SetTrigger(served ? PayId : AngryId); }
        void Update()
        {
            if (!GameRoot.Ready || !Owner) return;
            var d = Target - transform.position; d.y = 0;
            bool moving = d.sqrMagnitude > 0.0025f;
            float cur = 0f;
            if (moving)
            {
                var step = Vector3.ClampMagnitude(d.normalized * Speed * Time.deltaTime, d.magnitude);
                var ap = AgentMove.Slide(transform, step, BodyRadius);
                // #111: niente più teletrasporto al Target. Fermo davanti a un personaggio: si aspetta in fila.
                // Incastrato su un solido statico da più di 1,5 s: avanza dritto a velocità normale (nessun salto).
                if (ap.sqrMagnitude < 1e-10f && !AgentMove.LastBlockedByAgent) { _stuck += Time.deltaTime; if (_stuck > 1.5f) { transform.position += step; ap = step; } }
                else if (ap.sqrMagnitude > 1e-10f) _stuck = 0;
                AgentMove.Face(transform, ap.sqrMagnitude > 1e-8f ? ap : d, Time.deltaTime); cur = Time.deltaTime > 0 ? ap.magnitude / Time.deltaTime : 0f;
            }
            else if (Leaving) Owner.Recycle(this);
            var an = Anim();
            if (an && an.runtimeAnimatorController) { an.SetFloat(SpeedId, moving ? Mathf.Max(cur, 0.5f) : 0f); an.SetFloat(PatienceId, Leaving ? 1f : Mathf.Clamp01(1f - Waited / (float)GameRoot.I.Eco.Cfg.CustomerPatienceSec)); }
            if (!Leaving && (Waited += Time.deltaTime) > (float)GameRoot.I.Eco.Cfg.CustomerPatienceSec) Owner.Dequeue(this, false);
        }
    }
}
