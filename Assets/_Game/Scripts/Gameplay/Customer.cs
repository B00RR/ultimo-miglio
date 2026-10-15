using System.Collections.Generic;
using UnityEngine;

namespace UltimoMiglio.Gameplay
{
    /// <summary>Customer walking to a queue slot; leaves after patience (20 s) at the counter.</summary>
    public sealed class Customer : MonoBehaviour
    {
        public ParcelColor Wants; public float Speed = 2.2f;
        public Vector3 Target; public bool Leaving; public float Waited;
        public bool AtCounter => !Leaving && (transform.position - Target).sqrMagnitude < 0.04f;
        public CustomerQueue Owner;
        static readonly int SpeedId = Animator.StringToHash("Speed"), PatienceId = Animator.StringToHash("Patience"), PayId = Animator.StringToHash("Pay"), AngryId = Animator.StringToHash("Angry");
        Animator _an;
        Animator Anim() { if (!_an) _an = GetComponentInChildren<Animator>(); return _an; }
        /// <summary>chr_cliente.controller: Pay when served, Angry when patience ran out.</summary>
        public void OnLeave(bool served) { var a = Anim(); if (a && a.runtimeAnimatorController) a.SetTrigger(served ? PayId : AngryId); }
        void Update()
        {
            if (!GameRoot.Ready || !Owner) return;
            var d = Target - transform.position; d.y = 0;
            bool moving = d.sqrMagnitude > 0.0025f;
            if (moving) { transform.position += Vector3.ClampMagnitude(d.normalized * Speed * Time.deltaTime, d.magnitude); transform.rotation = Quaternion.LookRotation(d); }
            else if (Leaving) Owner.Recycle(this);
            var an = Anim();
            if (an && an.runtimeAnimatorController) { an.SetFloat(SpeedId, moving ? Speed : 0f); an.SetFloat(PatienceId, Leaving ? 1f : Mathf.Clamp01(1f - Waited / (float)GameRoot.I.Eco.Cfg.CustomerPatienceSec)); }
            if (!Leaving && (Waited += Time.deltaTime) > (float)GameRoot.I.Eco.Cfg.CustomerPatienceSec) Owner.Dequeue(this, false);
        }
    }
}
