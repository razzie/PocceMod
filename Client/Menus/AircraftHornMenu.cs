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
                Vehicles.SetAircraftHorn(_index);
                CloseMenu();
            };
        }
    }
}
