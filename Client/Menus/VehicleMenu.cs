using PocceMod.Shared;

namespace PocceMod.Client.Menus
{
    public class VehicleMenu : Submenu
    {
        public VehicleMenu() : base("select vehicle", Vehicles.Spawn, Config.VehicleList)
        {
        }
    }
}
