using UnityEngine;
using UltimoMiglio.Gameplay;

namespace UltimoMiglio.UI.Bridge
{
    /// <summary>Trigger on a doorway: when Nina enters, shows the room mini-label (04_UI §1.2).
    /// The camera confiner blend stays in the TL's camera code; this only drives the label.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class RoomDoorway : MonoBehaviour
    {
        public string RoomName = "Magazzino";
        public HudController Hud;
        string lastShown;
        void Reset() { GetComponent<Collider>().isTrigger = true; }
        void OnTriggerEnter(Collider o)
        {
            if (!o.GetComponent<NinaController>()) return;
            if (!Hud) Hud = FindFirstObjectByType<HudController>();
            if (Hud && lastShown != RoomName) { Hud.ShowRoomLabel(RoomName); lastShown = RoomName; }
        }
        void OnTriggerExit(Collider o) { if (o.GetComponent<NinaController>()) lastShown = null; }
    }
}
