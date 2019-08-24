using CitizenFX.Core.Native;
using MenuAPI;
using PocceMod.Shared;

namespace PocceMod.Client.Menus
{
    [MainMenuInclude]
    public class AircraftHornMenu : Menu
    {
        public AircraftHornMenu() : base("PocceMod", "select horn")
        {
            var horns = (Config.HornList.Length > 0) ? Config.HornList : new string[] { "SIRENS_AIRHORN" };
            foreach (var horn in horns)
            {
                var menuItem = new MenuItem(horn);
                AddMenuItem(menuItem);
            }

            OnItemSelect += (_menu, _item, _index) =>
            {
                SetAircraftHorn(_index);
                CloseMenu();
            };
        }

        public static void SetAircraftHorn(int horn)
        {
            var player = API.GetPlayerPed(-1);
            if (!API.IsPedInFlyingVehicle(player))
            {
                Common.Notification("Player is not in a flying vehicle");
                return;
            }

            if (!Common.EnsurePlayerIsVehicleDriver(out player, out int vehicle))
                return;

            Vehicles.SetAircraftHorn(vehicle, horn);
        }
    }
}
