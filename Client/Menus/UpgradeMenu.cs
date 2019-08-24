using CitizenFX.Core.Native;

namespace PocceMod.Client.Menus
{
    public static class UpgradeMenu
    {
        public static void ToggleUltrabrightHeadlight()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            Vehicles.ToggleLightMultiplier(vehicle);
        }

        public static void ToggleBackToTheFuture()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var model = (uint)API.GetEntityModel(vehicle);
            if (!API.IsThisModelACar(model) && !API.IsThisModelABike(model) && !API.IsThisModelAQuadbike(model))
            {
                Common.Notification("Only cars, bikes and quadbikes are supported");
                return;
            }

            var state = !Vehicles.GetLastState(vehicle, Vehicles.StateFlag.BackToTheFuture);
            Vehicles.SetState(vehicle, Vehicles.StateFlag.BackToTheFuture, state);

            if (state)
            {
                Common.Notification("Back to the Future!");
                API.SetVehicleTyresCanBurst(vehicle, false);
                API.SetDisableVehiclePetrolTankFires(vehicle, true);
            }
        }

        public static void ToggleTurboBoost()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var state = !Vehicles.GetLastState(vehicle, Vehicles.StateFlag.TurboBoost);
            Vehicles.SetState(vehicle, Vehicles.StateFlag.TurboBoost, state);

            if (state)
                Common.Notification("Turbo Boost enabled");
        }

        public static void CargobobMagnet()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                var pilot = API.GetPedInVehicleSeat(heli, -1);

                if (pilot != player && !Autopilot.IsOwnedAutopilot(pilot))
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

        public static void CompressVehicle()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            Vehicles.Compress(vehicle);
        }

        public static void ToggleAntiGravity()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var state = Vehicles.GetLastState(vehicle, Vehicles.StateFlag.AntiGravity);
            Vehicles.SetState(vehicle, Vehicles.StateFlag.AntiGravity, !state);
        }
    }
}
