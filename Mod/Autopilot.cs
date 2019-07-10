using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public class Autopilot : BaseScript
    {
        private static readonly uint Model = 0xA8683715; // monkey
        private static readonly string FlagDecor = "POCCE_AUTOPILOT_FLAG";
        private static readonly string PlayerDecor = "POCCE_AUTOPILOT_PLAYER";
        private static readonly string WaypointHashDecor = "POCCE_AUTOPILOT_WAYPOINT";

        public Autopilot()
        {
            API.DecorRegister(FlagDecor, 2);
            API.DecorRegister(PlayerDecor, 3);
            API.DecorRegister(WaypointHashDecor, 3);

            Tick += async () =>
            {
                await Delay(2000);
                Update();
            };
        }

        public static async Task Activate()
        {
            var player = Game.Player.Character.Handle;
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (API.IsVehicleSeatFree(vehicle, -1))
                {
                    await Spawn(vehicle);
                }
                else if (Vehicles.GetFreeSeat(vehicle, out int seat, 1))
                {
                    var driver = API.GetPedInVehicleSeat(vehicle, -1);
                    API.SetPedIntoVehicle(driver, vehicle, seat);
                    await Spawn(vehicle);
                }
                else
                {
                    Hud.Notification("Passenger seat not available");
                }
            }
            else
            {
                Hud.Notification("Player is not in a vehicle");
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
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        private static void Update()
        {
            var player = Game.Player.Character.Handle;
            if (!API.IsPedInAnyVehicle(player, false))
                return;

            var vehicle = API.GetVehiclePedIsIn(player, false);
            var vehicleModel = (uint)API.GetEntityModel(vehicle);
            var driver = API.GetPedInVehicleSeat(vehicle, -1);
            if (!API.DecorGetBool(driver, FlagDecor) || API.DecorGetInt(driver, PlayerDecor) != API.PlayerId())
                return;

            if (API.IsWaypointActive())
            {
                var wp = API.GetBlipInfoIdCoord(API.GetFirstBlipInfoId(8));
                if (API.DecorGetInt(driver, WaypointHashDecor) == wp.GetHashCode())
                    return;

                API.DecorSetInt(driver, WaypointHashDecor, wp.GetHashCode());

                var adjustedWp = Vector3.Zero;
                if (API.GetClosestVehicleNode(wp.X, wp.Y, wp.Z, ref adjustedWp, 1, 100.0f, 2.5f))
                    wp = adjustedWp;

                if (API.IsThisModelAPlane(vehicleModel))
                    API.TaskPlaneLand(driver, vehicle, wp.X, wp.Y, wp.Z, wp.X, wp.Y, wp.Z);
                else if (API.IsThisModelAHeli(vehicleModel))
                    API.TaskHeliMission(driver, vehicle, 0, 0, wp.X, wp.Y, wp.Z, 4, API.GetVehicleModelMaxSpeed(vehicleModel), 5.0f, GetHeading(vehicle, wp), -1, -1, 0, 32);
                else
                    //API.TaskVehicleGotoNavmesh(driver, vehicle, wp.X, wp.Y, wp.Z, 30.0f, 156, 5.0f);
                    API.TaskVehicleDriveToCoordLongrange(driver, vehicle, wp.X, wp.Y, wp.Z, API.GetVehicleModelMaxSpeed(vehicleModel), 156, 5f);
            }
        }

        private static float GetHeading(int vehicle, Vector3 wp)
        {
            var coords = API.GetEntityCoords(vehicle, false);
            var heading = (float)Math.Atan2(wp.Y - coords.Y, wp.X - coords.X);
            return MathUtil.RadiansToDegrees(heading);
        }
    }
}
