using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class VehicleMenu : Submenu
    {
        public VehicleMenu() : base("select vehicle", Vehicles.Spawn, Config.VehicleList)
        {
        }

        public static async Task SpawnByName()
        {
            var model = await Common.GetUserInput("Spawn vehicle by name", "", 30);
            await Vehicles.Spawn(model);
        }

        public static void TeleportToClosestVehicle(bool forcePassenger = false)
        {
            var vehicles = Vehicles.Get();
            if (Common.GetClosestEntity(vehicles, out int vehicle))
            {
                if (Vehicles.GetFreeSeat(vehicle, out int seat, forcePassenger))
                {
                    var player = API.GetPlayerPed(-1);
                    API.SetPedIntoVehicle(player, vehicle, seat);
                }
                else
                {
                    Common.Notification("Closest vehicle doesn't have a free seat");
                }
            }
            else
            {
                Common.Notification("No vehicles in range");
            }
        }
    }
}
