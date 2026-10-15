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
        void Update()
        {
            var d = Target - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.0025f) { transform.position += Vector3.ClampMagnitude(d.normalized * Speed * Time.deltaTime, d.magnitude); transform.rotation = Quaternion.LookRotation(d); }
            else if (Leaving) Owner.Recycle(this);
            if (!Leaving && (Waited += Time.deltaTime) > (float)GameRoot.I.Eco.Cfg.CustomerPatienceSec) Owner.Dequeue(this, false);
        }
    }
}
