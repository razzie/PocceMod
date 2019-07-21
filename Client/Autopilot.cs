using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Autopilot : BaseScript
    {
        private const uint Model = 0xA8683715; // monkey
        private const uint PoliceHeli = 0x1517D4D9; // polmav
        private const int DrivingStyle = 537133886;
        private const string FlagDecor = "POCCE_AUTOPILOT_FLAG";
        private const string PlayerDecor = "POCCE_AUTOPILOT_PLAYER";
        private const string WaypointHashDecor = "POCCE_AUTOPILOT_WAYPOINT";

        public Autopilot()
        {
            API.DecorRegister(FlagDecor, 2);
            API.DecorRegister(PlayerDecor, 3);
            API.DecorRegister(WaypointHashDecor, 3);

            Tick += Update;
        }

        public static async Task Activate()
        {
            var player = API.GetPlayerPed(-1);
            if (!API.IsPedInAnyVehicle(player, false))
            {
                Common.Notification("Player is not in a vehicle");
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            if (API.IsVehicleSeatFree(vehicle, -1))
            {
                await Spawn(vehicle);
                return;
            }

            if (API.GetPedInVehicleSeat(vehicle, -1) != player)
            {
                Common.Notification("You are not the driver of this vehicle");
                return;
            }

            if (Vehicles.GetFreeSeat(vehicle, out int seat, true))
            {
                var driver = API.GetPedInVehicleSeat(vehicle, -1);
                API.SetPedIntoVehicle(driver, vehicle, seat);

                await Spawn(vehicle);
            }
            else
            {
                Common.Notification("An extra seat is required");
            }
        }

        public static async Task Deactivate()
        {
            var player = API.GetPlayerPed(-1);
            if (!API.IsPedInAnyVehicle(player, false))
            {
                Common.Notification("Player is not in a vehicle");
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            var driver = API.GetPedInVehicleSeat(vehicle, -1);
            if (!IsAutopilot(driver))
            {
                Common.Notification("The driver is not autopilot");
                return;
            }
            else if (!IsOwnedAutopilot(driver))
            {
                Common.Notification("The autopilot belongs to an other player");
                return;
            }
            
            API.TaskLeaveVehicle(driver, vehicle, 4096);
            await Delay(1000);

            API.SetPedIntoVehicle(player, vehicle, -1);
        }

        public static async Task Toggle()
        {
            var player = API.GetPlayerPed(-1);
            if (!API.IsPedInAnyVehicle(player, false))
            {
                Common.Notification("Player is not in a vehicle");
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            var driver = API.GetPedInVehicleSeat(vehicle, -1);
            if (IsOwnedAutopilot(driver))
                await Deactivate();
            else
                await Activate();
        }

        private static bool IsAutopilot(int driver)
        {
            return API.DecorGetBool(driver, FlagDecor);
        }

        private static bool IsOwnedAutopilot(int driver)
        {
            return IsAutopilot(driver) && API.DecorGetInt(driver, PlayerDecor) == API.PlayerId();
        }

        private static async Task Spawn(int vehicle)
        {
            await Common.RequestModel(Model);
            var playerID = API.PlayerId();
            var ped = API.CreatePedInsideVehicle(vehicle, 26, Model, -1, true, false);
            API.SetModelAsNoLongerNeeded(Model);
            API.DecorSetBool(ped, FlagDecor, true);
            API.DecorSetInt(ped, PlayerDecor, playerID);
            API.SetDriverAbility(ped, 1f);
            API.SetDriverAggressiveness(ped, 0f);
            API.SetPedAsGroupMember(ped, API.GetPlayerGroup(playerID));
            API.SetEntityAsMissionEntity(ped, true, true);
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await Delay(10);

            Wander(ped, vehicle);
        }

        private static Task Update()
        {
            var player = API.GetPlayerPed(-1);
            var inVehicle = API.IsPedInAnyVehicle(player, false);
            var vehicle = API.GetVehiclePedIsIn(player, !inVehicle);
            var driver = API.GetPedInVehicleSeat(vehicle, -1);

            if (!IsAutopilot(driver))
                return Delay(1000);

            if (!inVehicle && API.IsEntityAMissionEntity(driver) &&
                !API.AnyPassengersRappeling(vehicle) && Vehicles.GetPlayers(vehicle).Count == 0)
            {
                API.RemovePedFromGroup(driver);
                API.SetEntityAsMissionEntity(driver, false, false);
                var tmp_driver = driver;
                API.SetPedAsNoLongerNeeded(ref tmp_driver);
            }

            if ((uint)API.GetEntityModel(vehicle) == PoliceHeli)
            {
                var players = Peds.Get(Peds.Filter.NonPlayers, 1600f, driver);
                if (Common.GetClosestEntity(players, out int target, driver))
                {
                    API.SetVehicleSearchlight(vehicle, true, true);
                    API.SetMountedWeaponTarget(driver, target, 0, 0f, 0f, 0f);
                }
            }

            if (API.AnyPassengersRappeling(vehicle))
            {
                var coords = API.GetEntityCoords(vehicle, false);
                API.TaskHeliMission(driver, vehicle, 0, 0, coords.X, coords.Y, coords.Z, 4, 0f, 5f, API.GetEntityHeading(vehicle), -1, -1, 0, 0);
                API.DecorSetInt(driver, WaypointHashDecor, 0);
                return Delay(1000);
            }

            if (!IsOwnedAutopilot(driver))
                return Delay(1000);

            Vehicles.UpdateAutoHazardLights(vehicle);

            if (Common.GetWaypoint(out Vector3 wp, false))
            {
                // waypoint hasn't changed
                if (API.DecorGetInt(driver, WaypointHashDecor) == wp.GetHashCode())
                    return Delay(1000);

                API.DecorSetInt(driver, WaypointHashDecor, wp.GetHashCode());
                GotoWaypoint(driver, vehicle, wp);
                return Delay(1000);
            }

            // waypoint was removed
            if (API.DecorGetInt(driver, WaypointHashDecor) > 0)
            {
                API.DecorSetInt(driver, WaypointHashDecor, 0);
                Wander(driver, vehicle);
            }

            return Delay(1000);
        }

        private static float GetHeading(int vehicle, Vector3 wp)
        {
            var coords = API.GetEntityCoords(vehicle, false);
            var heading = (float)Math.Atan2(wp.Y - coords.Y, wp.X - coords.X);
            return MathUtil.RadiansToDegrees(heading);
        }

        private static void GotoWaypoint(int driver, int vehicle, Vector3 wp)
        {
            var vehicleModel = (uint)API.GetEntityModel(vehicle);
            if (API.IsThisModelAPlane(vehicleModel))
            {
                API.TaskPlaneLand(driver, vehicle, wp.X, wp.Y, wp.Z, wp.X, wp.Y, wp.Z);
            }
            else if (API.IsThisModelAHeli(vehicleModel))
            {
                var speed = API.GetVehicleModelMaxSpeed(vehicleModel);
                var heading = GetHeading(vehicle, wp);
                API.TaskHeliMission(driver, vehicle, 0, 0, wp.X, wp.Y, wp.Z, 4, speed, 5f, heading, -1, -1, 0, 32);
            }
            else
            {
                var speed = API.GetVehicleModelMaxSpeed(vehicleModel);
                API.TaskVehicleDriveToCoordLongrange(driver, vehicle, wp.X, wp.Y, wp.Z, speed, DrivingStyle, 5f);
            }
        }

        private static void Wander(int driver, int vehicle)
        {
            var vehicleModel = (uint)API.GetEntityModel(vehicle);
            if (API.IsThisModelAPlane(vehicleModel))
            {
                var pos = API.GetEntityCoords(vehicle, false);
                API.TaskPlaneLand(driver, vehicle, pos.X, pos.Y, pos.Z, pos.X, pos.Y, pos.Z);
            }
            else if (API.IsThisModelAHeli(vehicleModel))
            {
                var speed = API.GetVehicleModelMaxSpeed(vehicleModel);
                var heading = API.GetEntityHeading(vehicle);
                var pos = API.GetEntityCoords(vehicle, false);
                API.TaskHeliMission(driver, vehicle, 0, 0, pos.X, pos.Y, pos.Z, 4, speed, 5f, heading, -1, -1, 0, 0);
            }
            else
            {
                var speed = API.GetVehicleModelMaxSpeed(vehicleModel);
                API.TaskVehicleDriveWander(driver, vehicle, speed, DrivingStyle);
            }
        }
    }
}
