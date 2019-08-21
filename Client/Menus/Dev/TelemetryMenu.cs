using MenuAPI;

namespace PocceMod.Client.Menus.Dev
{
    public class TelemetryMenu : Menu
    {
        public TelemetryMenu() : base("PocceMod", "telemetry menu")
        {
            var submenu = new PlayerTelemetryMenu();
            MenuController.AddSubmenu(this, submenu);

            OnItemSelect += (_menu, _item, _index) =>
            {
                submenu.OpenMenu(_item.ItemData);
                CloseMenu();
            };

            OnMenuOpen += (_menu) =>
            {
                foreach (var playerTelemetry in Telemetry.Entries)
                {
                    var menuItem = new MenuItem("player#" + playerTelemetry.Key) { ItemData = playerTelemetry.Value };
                    AddMenuItem(menuItem);
                }
            };

            OnMenuClose += (_menu) =>
            {
                ClearMenuItems();
            };
        }
    }
}
