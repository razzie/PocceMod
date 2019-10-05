using MenuAPI;
using PocceMod.Shared;

namespace PocceMod.Client.Menus
{
    [MainMenuInclude]
    public class CustomHornMenu : Menu
    {
        public CustomHornMenu() : base("PocceMod", "select horn")
        {
            var horns = (Config.HornList.Length > 0) ? Config.HornList : new string[] { "SIRENS_AIRHORN" };
            foreach (var horn in horns)
            {
                var menuItem = new MenuItem(horn);
                AddMenuItem(menuItem);
            }

            OnItemSelect += (menu, item, index) => SetCustomHorn(index);
        }

        public static void SetCustomHorn(int horn)
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            Vehicles.SetCustomHorn(vehicle, horn);
        }
    }
}
