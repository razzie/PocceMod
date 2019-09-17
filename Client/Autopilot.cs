using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Autopilot : BaseScript
    {
        private const uint Model = 0xA8683715; // monkey
        private const int DrivingStyle = 537133886;
        private const string FlagDecor = "POCCE_AUTOPILOT_FLAG";
        private const string PlayerDecor = "POCCE_AUTOPILOT_PLAYER";
        private const string WaypointHashDecor = "POCCE_AUTOPILOT_WAYPOINT";

        public Autopilot()
        {
            API.DecorRegister(FlagDecor, 2);
            API.DecorRegister(PlayerDecor, 3);
            API.DecorRegister(WaypointHashDecor, 3);
            
            Tick += Telemetry.Wrap("autopilot", Update);
        }

        public static async Task Activate()
        {
            if (!Common.EnsurePlayerIsInVehicle(out int player, out int vehicle))
                return;

            if (API.IsVehicleSeatFree(vehicle, -1))
            {
                await Spawn(vehicle);
                return;
            }

            if (API.GetPedInVehicleSeat(vehicle, -1) != player)
            {
                Common.Notification("Player is not the driver of this vehicle");
                return;
            }

            var model = (uint)API.GetEntityModel(vehicle);
            if (API.GetVehicleModelNumberOfSeats(model) == 1)
            {
                API.TaskLeaveVehicle(player, vehicle, 4096);
                while (API.IsPedInVehicle(player, vehicle, false))
                {
                    await Delay(100);
                }

                await Spawn(vehicle);
                return;
            }

            if (Vehicles.GetFreeSeat(vehicle, out int seat, true))
            {
                var driver = API.GetPedInVehicleSeat(vehicle, -1);
                API.SetPedIntoVehicle(driver, vehicle, seat);

                await Spawn(vehicle);
                return;
            }
            else
            {
                Common.Notification("An extra seat is required");
            }
        }

        public static async Task Deactivate()
        {
            if (!Common.EnsurePlayerIsInVehicle(out int player, out int vehicle))
                return;

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

            if (API.IsPedDeadOrDying(driver, true))
            {
                API.DeletePed(ref driver);
            }
            else
            {
                API.TaskLeaveVehicle(driver, vehicle, 4096);
                while (API.IsPedInVehicle(driver, vehicle, false))
                {
                    await Delay(100);
                }
            }

            API.SetPedIntoVehicle(player, vehicle, -1);
        }

        public static async Task Toggle()
        {
            if (!Common.EnsurePlayerIsInVehicle(out int player, out int vehicle))
                return;

            var driver = API.GetPedInVehicleSeat(vehicle, -1);
            if (IsOwnedAutopilot(driver))
                await Deactivate();
            else
                await Activate();
        }

        public static bool IsAutopilot(int driver)
        {
            return API.DecorGetBool(driver, FlagDecor);
        }

        public static bool IsOwnedAutopilot(int driver)
        {
            return IsAutopilot(driver) && API.DecorGetInt(driver, PlayerDecor) == Common.PlayerID;
        }

        public static async Task Spawn(int vehicle)
        {
            await Common.RequestModel(Model);
            var ped = API.CreatePedInsideVehicle(vehicle, 26, Model, -1, true, false);
            API.SetModelAsNoLongerNeeded(Model);
            API.DecorSetBool(ped, FlagDecor, true);
            API.DecorSetInt(ped, PlayerDecor, Common.PlayerID);
            API.SetDriverAbility(ped, 1f);
            API.SetDriverAggressiveness(ped, 0f);
            API.SetPedAsGroupMember(ped, API.GetPlayerGroup(API.PlayerId()));
            API.SetEntityAsMissionEntity(ped, true, true);
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await Delay(0);

            Wander(ped, vehicle);
        }

        public static IEnumerable<Tuple<int, int>> Get(bool ownedAutipilots, float rangeSquared = 3600f)
        {
            var ownershipCheck = ownedAutipilots ? new Func<int, bool>(IsOwnedAutopilot) : new Func<int, bool>(IsAutopilot);

            return Peds.Get(Peds.Filter.Dead | Peds.Filter.Players, rangeSquared)
                .Where(ped => ownershipCheck(ped))
                .Select(ped => new Tuple<int, int>(ped, API.GetVehiclePedIsIn(ped, false)))
                .Where(tup => API.GetPedInVehicleSeat(tup.Item2, -1) == tup.Item1);
        }

        private static bool HasWaypoint(int driver)
        {
            return API.DecorGetInt(driver, WaypointHashDecor) > 0;
        }

        private static bool HasWaypoint(int driver, Vector3 wp)
        {
            return API.DecorGetInt(driver, WaypointHashDecor) == wp.GetHashCode();
        }

        private static float GetHeading(int vehicle, Vector3 wp)
        {
            var coords = API.GetEntityCoords(vehicle, false);
            var heading = (float)Math.Atan2(wp.Y - coords.Y, wp.X - coords.X);
            return MathUtil.RadiansToDegrees(heading);
        }

        public static void GotoWaypoint(int driver, int vehicle, Vector3 wp)
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

            API.DecorSetInt(driver, WaypointHashDecor, wp.GetHashCode());
        }

        public static void Wander(int driver, int vehicle)
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

            API.DecorSetInt(driver, WaypointHashDecor, 0);
        }

        private static void UpdateOwnedAutopilot(int vehicle, int driver)
        {
            if (!API.AnyPassengersRappeling(vehicle) && Common.GetWaypoint(out Vector3 wp, false))
            {
                // waypoint hasn't changed
                if (HasWaypoint(driver, wp))
                    return;

                GotoWaypoint(driver, vehicle, wp);
                return;
            }

            // waypoint was removed
            if (HasWaypoint(driver))
            {
                Wander(driver, vehicle);
            }
        }

        private static Task Update()
        {
            foreach (var tuple in Get(false))
            {
                var ped = tuple.Item1;
                var vehicle = tuple.Item2;

                Vehicles.UpdateAutoHazardLights(vehicle);

                if (API.IsEntityAMissionEntity(ped) && Vehicles.GetPlayers(vehicle).Count == 0)
                {
                    API.RemovePedFromGroup(ped);
                    API.SetEntityAsMissionEntity(ped, false, false);
                    var tmp_ped = ped;
                    API.SetPedAsNoLongerNeeded(ref tmp_ped);
                }

                if (API.IsEntityDead(vehicle))
                    continue;

                var model = (uint)API.GetEntityModel(vehicle);
                if (API.IsThisModelAHeli(model))
                {
                    if (API.AnyPassengersRappeling(vehicle) && API.DecorGetInt(ped, WaypointHashDecor) > 0)
                    {
                        var coords = API.GetEntityCoords(vehicle, false);
                        API.TaskHeliMission(ped, vehicle, 0, 0, coords.X, coords.Y, coords.Z, 4, 0f, 5f, API.GetEntityHeading(vehicle), -1, -1, 0, 0);
                        API.DecorSetInt(ped, WaypointHashDecor, 0);
                    }

                    if (!API.IsVehicleSearchlightOn(vehicle))
                        API.SetVehicleSearchlight(vehicle, true, true);
                    else if (!API.IsMountedWeaponTaskUnderneathDrivingTask(ped))
                        API.ControlMountedWeapon(ped);
                }
                else if (API.IsPedInFlyingVehicle(ped))
                {
                    if (!API.IsEntityInAir(vehicle) &&
                        (!Vehicles.IsFeatureEnabled(vehicle, Vehicles.FeatureFlag.RemoteControl) || API.GetEntityRotation(vehicle, 2).X > 30f))
                    {
                        API.ApplyForceToEntity(vehicle, 0, 0f, 0f, 200f, 0f, 1f, 0f, -1, true, true, true, false, false);
                        API.ApplyForceToEntityCenterOfMass(vehicle, 0, 0f, 200f, 0f, false, true, true, false);

                        var pos = API.GetEntityCoords(vehicle, false);
                        API.TaskPlaneLand(ped, vehicle, pos.X, pos.Y, pos.Z, pos.X, pos.Y, pos.Z);
                        API.DecorSetInt(ped, WaypointHashDecor, 0);
                    }

                    if (Common.GetEntityHeightAboveGround(vehicle) > 10f && API.GetEntitySpeedVector(vehicle, true).Y > 10f)
                    {
                        if (API.GetVehicleLandingGear(vehicle) == 0)
                            API.SetVehicleLandingGear(vehicle, 1);
                    }
                    else
                    {
                        if (API.GetVehicleLandingGear(vehicle) == 4)
                            API.SetVehicleLandingGear(vehicle, 2);
                    }
                }

                if (IsOwnedAutopilot(ped))
                    UpdateOwnedAutopilot(vehicle, ped);
            }

            return Delay(100);
        }
    }
}
