using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    [MainMenuInclude]
    public class VehicleMenu : Menu
    {
        private static readonly bool AutoDespawn;

        static VehicleMenu()
        {
            AutoDespawn = Config.GetConfigBool("AutoDespawnVehicles");
        }

        public VehicleMenu() : base("PocceMod", "select vehicle")
        {
            foreach (var vehicle in Config.VehicleList)
            {
                var model = (uint)API.GetHashKey(vehicle);
                if (!API.IsModelValid(model))
                {
                    Debug.WriteLine("[PocceMod] invalid vehicle: " + vehicle);
                    continue;
                }

                AddVehicle(vehicle);
            }

            OnListItemSelect += async (menu, listItem, listIndex, itemIndex) =>
            {
                var model = listItem.ListItems[listIndex];
                var vehicle = await Vehicles.Spawn(model);

                if (AutoDespawn)
                    API.SetVehicleAsNoLongerNeeded(ref vehicle);
            };
        }

        private void AddVehicle(string model)
        {
            var hash = (uint)API.GetHashKey(model);
            if (!API.IsModelAVehicle(hash))
                return;

            var category = GetCategoryName(hash);

            foreach (var menuListItem in GetMenuItems().Cast<MenuListItem>())
            {
                if (menuListItem.Text == category)
                {
                    foreach (var item in menuListItem.ListItems)
                    {
                        if (item == model)
                            return;
                    }

                    menuListItem.ListItems.Add(model);
                    return;
                }
            }
            
            AddMenuItem(new MenuListItem(category, new List<string> { model }, 0));
        }

        public async Task SpawnByName()
        {
            var model = await Common.GetUserInput("Spawn vehicle by name", "", 30);
            if (string.IsNullOrEmpty(model))
                return;

            AddVehicle(model);
            var vehicle = await Vehicles.Spawn(model);

            if (AutoDespawn)
                API.SetVehicleAsNoLongerNeeded(ref vehicle);
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

        private static string GetCategoryName(uint model)
        {
            if (API.IsThisModelACar(model))
                return "Cars";
            else if (API.IsThisModelABike(model))
                return "Bikes";
            else if (API.IsThisModelABicycle(model))
                return "Bicycles";
            else if (API.IsThisModelAQuadbike(model))
                return "Quadbikes";
            else if (API.IsThisModelAHeli(model))
                return "Helicopters";
            else if (API.IsThisModelAPlane(model))
                return "Airplanes";
            else if (API.IsThisModelABoat(model))
                return "Boats";
            else if (API.IsThisModelAnEmergencyBoat(model))
                return "Emergency boats";
            else if (API.IsThisModelAJetski(model))
                return "Jetskis";
            else if (API.IsThisModelAnAmphibiousCar(model))
                return "Amphibious cars";
            else if (API.IsThisModelASubmersible(model))
                return "Submersibles";
            else if (API.IsThisModelATrain(model))
                return "Trains";
            else
                return "Other";
        }
    }
}
