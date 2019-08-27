using CitizenFX.Core.Native;
using MenuAPI;
using PocceMod.Shared;
using System.Collections.Generic;

namespace PocceMod.Client.Menus.Dev
{
    public class TelemetryMenu : Menu
    {
        public TelemetryMenu() : base("PocceMod", "telemetry menu")
        {
            var submenu = new PlayerTelemetryMenu();
            MenuController.AddSubmenu(this, submenu);
            
            OnItemSelect += (menu, item, index) =>
            {
                submenu.OpenMenu(item.ItemData);
            };

            OnListItemSelect += (menu, listItem, listIndex, itemIndex) =>
            {
                int timeoutSec = 0;

                switch (listIndex)
                {
                    case 0:
                        timeoutSec = 10;
                        break;

                    case 1:
                        timeoutSec = 30;
                        break;

                    case 2:
                        timeoutSec = 60;
                        break;

                    case 3:
                        timeoutSec = 120;
                        break;

                    case 4:
                        Clear();
                        return;

                    default:
                        return;
                }

                Common.Notification(string.Format("Starting {0} second measurement for all players", timeoutSec));
                Telemetry.Request(timeoutSec);
            };

            Clear();

            Telemetry.TelemetryReceived += AddTelemetry;
        }

        public void AddTelemetry(int sourcePlayer, Telemetry.Measurement measurement)
        {
            var playerName = API.GetPlayerName(API.GetPlayerFromServerId(sourcePlayer));
            var menuItem = new MenuItem(string.Format("{0} (player#{1})", playerName, sourcePlayer)) { ItemData = measurement };
            AddMenuItem(menuItem);
        }

        public void Clear()
        {
            ClearMenuItems();

            var startMeasurementItem = new MenuListItem("Start measurement for all", new List<string> { "10sec", "30sec", "1min", "2min", "Clear" }, 0)
            {
                Enabled = Permission.CanDo(Ability.ReceiveTelemetry)
            };
            AddMenuItem(startMeasurementItem);
        }
    }
}
