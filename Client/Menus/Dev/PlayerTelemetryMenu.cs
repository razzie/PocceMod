using MenuAPI;

namespace PocceMod.Client.Menus.Dev
{
    public class PlayerTelemetryMenu : Menu
    {
        private Telemetry.Measurement _source;

        public PlayerTelemetryMenu() : base("PocceMod", "player telemetry")
        {
            OnMenuOpen += (_menu) =>
            {
                if (_source == null)
                    return;

                AddData("Total", _source.Total);

                foreach (var feature in _source.FeatureData)
                {
                    AddData(feature.Key, feature.Value);
                }
            };

            OnMenuClose += (_menu) =>
            {
                ClearMenuItems();
            };
        }

        public void OpenMenu(Telemetry.Measurement source)
        {
            _source = source;
            OpenMenu();
        }

        private void AddData(string feature, Telemetry.Data data)
        {
            var menuListItem = new MenuListItem(feature, data.Items, 4);
            AddMenuItem(menuListItem);
        }
    }
}
