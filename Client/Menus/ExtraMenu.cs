using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public static class ExtraMenu
    {
        private static bool _crazyOceanWaves = false;

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

        public static void UltrabrightHeadlight()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            Vehicles.EnableUltrabrightHeadlight(vehicle);
            Common.Notification("Use arrow up/down keys to change brightness");
        }

        public static void CargobobMagnet()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                if (API.GetPedInVehicleSeat(heli, -1) != player)
                {
                    Common.Notification("Player is not the pilot of this heli");
                    return;
                }

                if (API.IsCargobobMagnetActive(heli))
                {
                    API.SetCargobobPickupMagnetActive(heli, false);
                    API.RetractCargobobHook(heli);
                }
                else
                {
                    API.EnableCargobobHook(heli, 1);
                    API.SetCargobobPickupMagnetActive(heli, true);
                }
            }
            else
            {
                Common.Notification("Player is not in a heli");
            }
        }

        public static void ToggleZeroGravity()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            if (AntiGravity.Contains(vehicle))
                AntiGravity.Remove(vehicle);
            else
                AntiGravity.Add(vehicle, 1.5f);
        }

        public static async Task Balloons()
        {
            var models = new string[] { "prop_beach_volball01", "prop_beach_volball02", "prop_beachball_02", "prop_bskball_01" };
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);
            coords.Z += 1f;

            var root = await Props.SpawnAtCoords("prop_devin_rope_01", coords, Vector3.Zero);
            Ropes.PlayerAttach(root, Vector3.Zero);

            var balls = new List<int>();
            for (int i = 0; i < API.GetRandomIntInRange(3, 6); ++i)
            {
                var model = models[API.GetRandomIntInRange(0, models.Length)];
                var offset = new Vector3(API.GetRandomFloatInRange(-0.25f, 0.25f), API.GetRandomFloatInRange(-0.25f, 0.25f), API.GetRandomFloatInRange(0f, 0.5f));
                var ball = await Props.SpawnAtCoords(model, coords + offset, Vector3.Zero);
                Ropes.Attach(root, ball, Vector3.Zero, Vector3.Zero);
                AntiGravity.Add(ball, 2f);
                balls.Add(ball);
            }

            foreach (var ball in balls)
            {
                int tmp_ball = ball;
                API.SetEntityAsNoLongerNeeded(ref tmp_ball);
            }

            API.SetEntityAsNoLongerNeeded(ref root);
        }
    }
}
