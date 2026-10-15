using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.Core;
using UltimoMiglio.VFX;

namespace UltimoMiglio.Gameplay
{    /// <summary>Cassa (offline, 02 §4): Nina walks over it to collect; LN otherwise stays inside.</summary>
    public sealed class CassaZone : ProximityZone
    {
        public CoinSpawner Coins; public bool OfferDouble; // rewarded/Bulloni flow plugs in here (Services, not in sprint 1)
        protected override void OnZoneStart()
        {
            var eco = GameRoot.I.Eco; if (eco.S.CassaLn <= 0) return;
            long day = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400;
            double ln = eco.CollectCassa(OfferDouble, day, transform.position.ToWP());
            Coins.Burst(transform.position, ln);
        }
        protected override void OnZoneTick(float t) { }
    }
}
