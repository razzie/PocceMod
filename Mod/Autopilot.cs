using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public class Autopilot : BaseScript
    {
        private static readonly uint Model = 0xA8683715; // monkey
        private static readonly int DrivingStyle = 156;
        private static readonly string FlagDecor = "POCCE_AUTOPILOT_FLAG";
        private static readonly string PlayerDecor = "POCCE_AUTOPILOT_PLAYER";
        private static readonly string WaypointHashDecor = "POCCE_AUTOPILOT_WAYPOINT";

        public Autopilot()
        {
            API.DecorRegister(FlagDecor, 2);
            API.DecorRegister(PlayerDecor, 3);
            API.DecorRegister(WaypointHashDecor, 3);

            Tick += Update;
        }

        public static async Task Activate()
        {
            var player = Game.Player.Character.Handle;
            if (!API.IsPedInAnyVehicle(player, false))
            {
                Hud.Notification("Player is not in a vehicle");
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            if (API.IsVehicleSeatFree(vehicle, -1))
            {
                await Spawn(vehicle);
                return;
            }

            if (API.GetPedInVehicleSeat(vehicle, -1) != player)
            {
                Hud.Notification("You are not the driver of this vehicle");
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
                Hud.Notification("An extra seat is required");
            }
        }

        private static async Task Spawn(int vehicle)
        {
            await Common.RequestModel(Model);
            var playerID = API.PlayerId();
            var ped = API.CreatePedInsideVehicle(vehicle, 26, Model, -1, true, false);
            API.DecorSetBool(ped, FlagDecor, true);
            API.DecorSetInt(ped, PlayerDecor, playerID);
            API.SetDriverAbility(ped, 1f);
            API.SetDriverAggressiveness(ped, 0f);
            API.SetPedAsGroupMember(ped, API.GetPlayerGroup(playerID));
            API.SetEntityAsMissionEntity(ped, true, true);
            Wander(ped, vehicle);
        }

        private static async Task Update()
        {
            var player = Game.Player.Character.Handle;
            if (!API.IsPedInAnyVehicle(player, false))
                return;

            var vehicle = API.GetVehiclePedIsIn(player, false);
            var driver = API.GetPedInVehicleSeat(vehicle, -1);

            if (!API.DecorGetBool(driver, FlagDecor) || API.DecorGetInt(driver, PlayerDecor) != API.PlayerId())
                return;

            if (API.AnyPassengersRappeling(vehicle))
            {
                var coords = API.GetEntityCoords(vehicle, false);
                API.TaskHeliMission(driver, vehicle, 0, 0, coords.X, coords.Y, coords.Z, 4, 0.0f, 5.0f, API.GetEntityHeading(vehicle), -1, -1, 0, 0);
                API.DecorSetInt(driver, WaypointHashDecor, 0);
                await Delay(1000);
                return;
            }

            if (Common.GetWaypoint(out Vector3 wp, false))
            {
                // waypoint hasn't changed
                if (API.DecorGetInt(driver, WaypointHashDecor) == wp.GetHashCode())
                    return;

                API.DecorSetInt(driver, WaypointHashDecor, wp.GetHashCode());
                GotoWaypoint(driver, vehicle, wp);
                return;
            }

            // waypoint was removed
            if (API.DecorGetInt(driver, WaypointHashDecor) > 0)
            {
                API.DecorSetInt(driver, WaypointHashDecor, 0);
                Wander(driver, vehicle);
            }
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
                API.TaskHeliMission(driver, vehicle, 0, 0, wp.X, wp.Y, wp.Z, 4, speed, 5.0f, heading, -1, -1, 0, 32);
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
                var height = API.GetEntityHeightAboveGround(vehicle);
                var pos = API.GetEntityCoords(vehicle, false);
                API.TaskPlaneLand(driver, vehicle, pos.X, pos.Y, pos.Z, pos.X, pos.Y, pos.Z);
            }
            else if (API.IsThisModelAHeli(vehicleModel))
            {
                var height = API.GetEntityHeightAboveGround(vehicle);
                var speed = API.GetVehicleModelMaxSpeed(vehicleModel);
                var heading = API.GetEntityHeading(vehicle);
                var pos = API.GetEntityCoords(vehicle, false);
                pos.Z -= height;
                API.TaskHeliMission(driver, vehicle, 0, 0, pos.X, pos.Y, pos.Z, 4, speed, 5.0f, heading, -1, -1, 0, 32);
            }
            else
            {
                var speed = API.GetVehicleModelMaxSpeed(vehicleModel);
                API.TaskVehicleDriveWander(driver, vehicle, speed, DrivingStyle);
            }
        }
    }
}
