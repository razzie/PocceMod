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

            var state = !Vehicles.IsFeatureEnabled(vehicle, Vehicles.FeatureFlag.BackToTheFuture);
            Vehicles.SetFeatureEnabled(vehicle, Vehicles.FeatureFlag.BackToTheFuture, state);

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

            var state = !Vehicles.IsFeatureEnabled(vehicle, Vehicles.FeatureFlag.TurboBoost);
            Vehicles.SetFeatureEnabled(vehicle, Vehicles.FeatureFlag.TurboBoost, state);

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

            var state = Vehicles.IsFeatureEnabled(vehicle, Vehicles.FeatureFlag.AntiGravity);
            Vehicles.SetFeatureEnabled(vehicle, Vehicles.FeatureFlag.AntiGravity, !state);

            if (!state)
                Common.Notification("Anti-gravity enabled");
        }

        public static void ToggleRemoteControl()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var state = Vehicles.IsFeatureEnabled(vehicle, Vehicles.FeatureFlag.RemoteControl);
            Vehicles.SetFeatureEnabled(vehicle, Vehicles.FeatureFlag.RemoteControl, !state);

            if (!state)
                Common.Notification("Remote control enabled (use arrow keys)");
        }

        public static void ToggleJesusMode()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var state = Vehicles.IsFeatureEnabled(vehicle, Vehicles.FeatureFlag.JesusMode);
            Vehicles.SetFeatureEnabled(vehicle, Vehicles.FeatureFlag.JesusMode, !state);

            if (!state)
                Common.Notification("Jesus mode enabled (you can float on water)");
        }
    }
}
