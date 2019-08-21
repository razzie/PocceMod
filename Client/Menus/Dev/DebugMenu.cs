using MenuAPI;
using PocceMod.Shared;

namespace PocceMod.Client.Menus.Dev
{
    [MainMenuInclude]
    public class DebugMenu : Menu
    {
        private enum ConfigKind
        {
            String,
            Int,
            Float,
            Bool,
            Control
        }

        public DebugMenu() : base("PocceMod", "debug menu")
        {
            AddConfigItem("MenuKey", ConfigKind.Control);
            AddConfigItem("MenuRightAlign", ConfigKind.Bool);
            AddConfigItem("RopegunWindKey", ConfigKind.Control);
            AddConfigItem("RopeClearKey", ConfigKind.Control);
            AddConfigItem("PropUndoKey", ConfigKind.Control);
            AddConfigItem("PropEditDistanceFactor", ConfigKind.Float);
            AddConfigItem("DisablePropEdit", ConfigKind.Bool);
            AddConfigItem("DisableAutoHazardLights", ConfigKind.Bool);
            AddConfigItem("WheelFireTopSpeed", ConfigKind.Float);
            AddConfigItem("WheelFireDensity", ConfigKind.Float);
            AddConfigItem("WheelFireTimeoutSec", ConfigKind.Float);
            AddConfigItem("TurboBoostKey", ConfigKind.Control);
            AddConfigItem("TurboBoostPower", ConfigKind.Float);
            AddConfigItem("TurboBoostChargeSec", ConfigKind.Float);
            AddConfigItem("TurboBoostRechargeRate", ConfigKind.Float);
            AddConfigItem("TurboBoostMinAngle", ConfigKind.Float);
            AddConfigItem("TurboBoostMaxAngle", ConfigKind.Float);
            AddConfigItem("IgnorePermissions", ConfigKind.Bool);

            var permissionsMenu = new PermissionsMenu();
            var permissionsMenuItem = new MenuItem("Permissions ↕");
            MenuController.BindMenuItem(this, permissionsMenu, permissionsMenuItem);
            AddMenuItem(permissionsMenuItem);

            var telemetryMenu = new TelemetryMenu();
            var telemetryMenuItem = new MenuItem("Telemetry ↕");
            MenuController.BindMenuItem(this, telemetryMenu, telemetryMenuItem);
            AddMenuItem(telemetryMenuItem);
        }

        private string GetConfigItem(string item, ConfigKind kind)
        {
            string value;

            switch (kind)
            {
                case ConfigKind.String:
                    if (Config.GetConfigString(item, out value))
                        return value;
                    break;

                case ConfigKind.Int:
                    if (Config.GetConfigString(item, out value) && int.TryParse(value, out int intVal))
                        return intVal.ToString();
                    break;

                case ConfigKind.Float:
                    if (Config.GetConfigString(item, out value) && float.TryParse(value, out float floatVal))
                        return floatVal.ToString();
                    break;

                case ConfigKind.Bool:
                    if (Config.GetConfigString(item, out value) && bool.TryParse(value, out bool boolVal))
                        return boolVal.ToString();
                    break;

                case ConfigKind.Control:
                    if (Config.GetConfigString(item, out value) && Controls.InputPair.TryParse(value, out Controls.InputPair controlVal))
                        return controlVal.ToString();
                    break;
            }

            return "?";
        }

        private void AddConfigItem(string item, ConfigKind kind)
        {
            var menuItem = new MenuItem(item) { Label = GetConfigItem(item, kind) };
            AddMenuItem(menuItem);
        }
    }
}
