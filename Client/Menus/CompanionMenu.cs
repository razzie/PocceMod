using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public static class CompanionMenu
    {
        public static async Task PocceCompanion()
        {
            int ped;
            int player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (Vehicles.GetFreeSeat(vehicle, out int seat))
                {
                    var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
                    await Common.RequestModel(pocce);
                    ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                    API.SetModelAsNoLongerNeeded(pocce);
                }
                else if (API.GetEntitySpeed(vehicle) > 0.1f)
                {
                    Common.Notification("Player is in a moving vehicle and there are no free seats");
                    return;
                }
                else
                {
                    ped = await Peds.Spawn(Config.PocceList);
                }
            }
            else
            {
                ped = await Peds.Spawn(Config.PocceList);
            }

            Companions.Add(ped);
            await Peds.Arm(ped, Config.WeaponList);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PetCompanion()
        {
            int player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                Common.Notification("Don't spawn that poor pet on a heli");
                return;
            }
            else if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (API.GetVehicleDashboardSpeed(vehicle) > 0.1f)
                {
                    Common.Notification("Player is in a moving vehicle");
                    return;
                }
            }

            var ped = await Peds.Spawn(Config.PetList, 28);
            Companions.Add(ped);
            await Peds.Arm(ped, null);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PoccePassengers()
        {
            if (!Common.EnsurePlayerIsInVehicle(out int player, out int vehicle))
                return;

            while (Vehicles.GetFreeSeat(vehicle, out int seat))
            {
                var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
                await Common.RequestModel(pocce);
                var ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                API.SetModelAsNoLongerNeeded(pocce);
                API.SetEntityAsNoLongerNeeded(ref ped);
            }
        }
    }
}
