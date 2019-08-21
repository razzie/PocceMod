using MenuAPI;
using System.Collections.Generic;

namespace PocceMod.Client.Menus.Dev
{
    using PlayerTelemetry = Dictionary<string, List<string>>;

    public class PlayerTelemetryMenu : Menu
    {
        private PlayerTelemetry _source;

        public PlayerTelemetryMenu() : base("PocceMod", "player telemetry")
        {
            OnMenuOpen += (_menu) =>
            {
                if (_source == null)
                    return;

                foreach (var feature in _source)
                {
                    var menuListItem = new MenuListItem(feature.Key, feature.Value, 0);
                    AddMenuItem(menuListItem);
                }
            };

            OnMenuClose += (_menu) =>
            {
                ClearMenuItems();
            };
        }

        public void OpenMenu(PlayerTelemetry source)
        {
            _source = source;
            OpenMenu();
        }
    }
}
