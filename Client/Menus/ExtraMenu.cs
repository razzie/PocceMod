using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public static class ExtraMenu
    {
        private const string FreezePosDecor = "POCCE_FREEZE_POSITION";
        private static bool _crazyOceanWaves = false;

        static ExtraMenu()
        {
            API.DecorRegister(FreezePosDecor, 2);
        }

        public static void ToggleCrazyOceanWaves()
        {
            _crazyOceanWaves = !_crazyOceanWaves;
            if (_crazyOceanWaves)
                API.SetWavesIntensity(8f);
            else
                API.ResetWavesIntensity();
        }

        public static async Task RappelFromHeli()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                if (!Vehicles.GetPedSeat(heli, player, out int seat))
                    return;

                switch (seat)
                {
                    case -1:
                        if (API.AreAnyVehicleSeatsFree(heli))
                        {
                            await Autopilot.Activate();
                            API.TaskRappelFromHeli(player, 0);
                        }
                        break;

                    case 0:
                        if (Vehicles.GetFreeSeat(heli, out int goodSeat, true))
                        {
                            API.SetPedIntoVehicle(player, heli, goodSeat);
                            API.TaskRappelFromHeli(player, 0);
                        }
                        break;

                    default:
                        API.TaskRappelFromHeli(player, 0);
                        break;
                }
            }
            else
            {
                Common.Notification("Player is not in a heli");
            }
        }

        public static async Task SpawnTrashPed()
        {
            var ped = await Peds.Spawn(Config.TrashPedList);
            BaseScript.TriggerServerEvent("PocceMod:Burn", API.PedToNet(ped));
            API.SetPedAsNoLongerNeeded(ref ped);
        }

        public static async Task Balloons()
        {
            var player = Common.GetPlayerPedOrVehicle();
            var coords = Common.GetEntityTopCoords(player);
            coords.Z += 0.5f;

            var balloon = await Props.SpawnBalloons(coords);
            Ropes.Attach(player, balloon, Vector3.Zero, Vector3.Zero);
            API.SetEntityAsNoLongerNeeded(ref balloon);
        }

        public static void FreezePosition()
        {
            var entity = Common.GetPlayerPedOrVehicle();
            var state = API.DecorGetBool(entity, FreezePosDecor);

            API.FreezeEntityPosition(entity, !state);
            API.DecorSetBool(entity, FreezePosDecor, !state);
        }
    }
}
