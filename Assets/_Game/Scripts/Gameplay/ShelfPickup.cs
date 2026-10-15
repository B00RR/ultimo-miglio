using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{    /// <summary>Shelf-pickup zone in front of the shelf (taking parcels from shelf to carry to the Bancone).</summary>
    public sealed class ShelfPickup : ProximityZone
    {
        public Shelf Shelf;
        protected override void OnZoneTick(float t) { if (!Nina.Stack.Full && Shelf.Count > 0 && Nina.Stack.TryPush(Shelf.Color, Shelf.transform.position + new Vector3(0, 1.0f, -1.2f))) Shelf.Take(); }
    }
}
