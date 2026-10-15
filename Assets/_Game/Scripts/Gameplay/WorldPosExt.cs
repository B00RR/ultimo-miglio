using UnityEngine;
using UltimoMiglio.Core;

namespace UltimoMiglio.Gameplay
{
    public static class WorldPosExt
    {
        public static WorldPos ToWP(this Vector3 v) => new WorldPos(v.x, v.y, v.z);
        public static Vector3 ToV3(this WorldPos p) => new Vector3(p.x, p.y, p.z);
    }
}
