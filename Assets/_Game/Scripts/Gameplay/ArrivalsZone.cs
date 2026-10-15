using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{    /// <summary>Zona Arrivi: the van drops parcels; Nina picks 1 per tick.</summary>
    public sealed class ArrivalsZone : ProximityZone
    {
        public int Stock = 20, MaxStock = 40; public float RefillPerSec = 1f; float _r;
        protected override void OnIdleUpdate() { _r += RefillPerSec * Time.deltaTime; while (_r >= 1 && Stock < MaxStock) { _r--; Stock++; } }
        protected override void OnZoneTick(float t)
        {
            if (Stock <= 0) return;
            var c = (ParcelColor)Random.Range(0, 3);
            if (Nina.Stack.TryPush(c, transform.position + Vector3.up * 0.4f)) { Stock--; UM_FxPlayer.Play(UM_FxPlayer.Fx.PackagePickup, transform.position); }
        }
        public bool TakeForRobot(out ParcelColor c) { c = (ParcelColor)Random.Range(0, 3); if (Stock <= 0) return false; Stock--; return true; }
    }
}
