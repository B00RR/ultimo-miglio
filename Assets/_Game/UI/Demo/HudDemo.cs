using System.Collections.Generic;
using UnityEngine;
using UltimoMiglio.UI;

namespace UltimoMiglio.UI.Demo
{
    /// <summary>
    /// Demo-only driver for HudDemo.unity (or any scene with the HUD prefab). Keys: 1 currencies, 2 Pedaggio on/off,
    /// 3 room label, 4 offline toast, 5 safe card, 6 ring phone, P open phone, Esc = Android Back.
    /// Values below are MOCKUP display values from 04_UI, used only to preview layout. Real numbers come from game data.
    /// </summary>
    public class HudDemo : MonoBehaviour
    {
        [SerializeField] HudController hud;
        [SerializeField] PhoneController phone;
        bool toll;

        void Start()
        {
            hud.PhoneButtonTapped += () => phone.OpenPhone();
            hud.PedaggioTapped += () => hud.ShowSafeCard(MockSafe());
            phone.ShowTheWayRequested += (app, id) => hud.ShowToast("Freccia → " + app + " " + id);
            phone.Opened += () => hud.SetPhoneBadge(0);
            foreach (var a in new[] { PhoneApp.Messaggi, PhoneApp.Ordini, PhoneApp.Flotta }) phone.UnlockApp(a);
            phone.SetBadge(PhoneApp.Flotta, 1);
            hud.SetCurrencies(1240, 12);
            hud.SetPhoneBadge(1);
            phone.SetHome(new PhoneHomeData { day = 4, daysPerYear = 365, time = "08:40", installmentLine = "Rata 1 · Cassaforte · 8.800 LN · Giorno 24", notification = "Mamma Rosa: ordine speciale!", questLine = "Quest di oggi: consegna 10 pacchi" });
            phone.SetOrders(new List<OrderItem> { new OrderItem { id = "rosa_01", customer = "MAMMA ROSA", title = "Pacco di sughi", reward = "+40 LN", secondsLeft = 95, comboText = "x1,2" } });
            phone.SetChat(new ChatThread { contact = "Civetta 🦉", accentClass = "accent-civetta",
                messages = new List<ChatMessage> { new ChatMessage { text = "Conoscevo tua nonna. Ada teneva i conti meglio di te. Il pacco sul bancone? Non aprirlo." } },
                quickReplies = new List<string> { "Chi sei?" } });
            phone.SetFleet(new FleetData { robots = new List<RobotStatus> { new RobotStatus { name = "NONNO", state = "lavora", level = 1, parcelsPerMin = 6, offlinePerMin = 2 } },
                cassaAmount = 240, cassaCapacity = 352, cassaFullInSeconds = 1200, nextPadTitle = "NUOVO SCAFFALE", nextPadId = "pad_shelf_2" });
        }

        SafeCardData MockSafe() => new SafeCardData { chapter = 1, trancheTotal = 132000, tranchePaid = 8800, day = 4, daysPerYear = 365,
            installmentIndex = 1, installmentAmount = 8800, installmentDueDay = 24, tollActive = toll, tollFraction = 0.15f };

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current; if (kb == null) return;
            if (kb.digit1Key.wasPressedThisFrame) { hud.SetCurrencies(Random.Range(0, 99999), Random.Range(0, 99)); hud.PulseCurrency(); }
            if (kb.digit2Key.wasPressedThisFrame) { toll = !toll; if (toll) hud.ShowPedaggio(0.15f); else hud.HidePedaggio(); }
            if (kb.digit3Key.wasPressedThisFrame) hud.ShowRoomLabel("Magazzino");
            if (kb.digit4Key.wasPressedThisFrame) hud.ShowOfflineToast(240);
            if (kb.digit5Key.wasPressedThisFrame) hud.ShowSafeCard(MockSafe());
            if (kb.digit6Key.wasPressedThisFrame) { hud.RingPhone(); hud.SetPhoneBadge(1); }
            if (kb.digit7Key.wasPressedThisFrame) hud.ShowCollectPopup(new Vector2(Screen.width / 2f, Screen.height / 2f), 8.5, toll ? 0.15f : 0f);
            if (kb.pKey.wasPressedThisFrame) phone.OpenPhone();
        }
    }
}
