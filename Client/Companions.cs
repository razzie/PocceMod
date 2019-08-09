using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Companions : BaseScript
    {
        private const string FlagDecor = "POCCE_COMPANION_FLAG";
        private const string PlayerDecor = "POCCE_COMPANION_PLAYER";
        private const uint Parachute = 0xFBAB5776; // gadget_parachute

        public Companions()
        {
            API.DecorRegister(FlagDecor, 2);
            API.DecorRegister(PlayerDecor, 3);

            Tick += Update;
        }

        private static bool IsCompanion(int ped)
        {
            return API.DecorGetBool(ped, FlagDecor) && API.DecorGetInt(ped, PlayerDecor) == API.PlayerId();
        }

        public static List<int> Get(IEnumerable<int> peds)
        {
            var companions = new List<int>();

            foreach (var ped in peds)
            {
                if (IsCompanion(ped))
                {
                    if (API.IsPedDeadOrDying(ped, true))
                    {
                        var blip = API.GetBlipFromEntity(ped);
                        API.RemoveBlip(ref blip);
                        continue;
                    }

                    companions.Add(ped);
                }
            }

            return companions;
        }

        public static void Add(int ped)
        {
            var playerID = API.PlayerId();
            var player = API.GetPlayerPed(-1);

            API.DecorSetBool(ped, FlagDecor, true);
            API.DecorSetInt(ped, PlayerDecor, playerID);
            API.SetPedRelationshipGroupHash(ped, (uint)API.GetPedRelationshipGroupHash(player));
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);

            var blip = API.AddBlipForEntity(ped);
            API.SetBlipAsFriendly(blip, true);
        }

        private static bool CheckAndHandlePlayerCombat(IEnumerable<int> peds, IEnumerable<int> companions)
        {
            var player = API.GetPlayerPed(-1);
            int target = 0;
            if (API.GetEntityPlayerIsFreeAimingAt(API.PlayerId(), ref target) || API.GetPlayerTargetEntity(API.PlayerId(), ref target))
            {
                foreach (var companion in companions)
                {
                    API.TaskCombatPed(companion, target, 0, 16);
                }
                return true;
            }

            foreach (var ped in peds)
            {
                if (API.IsPedInCombat(ped, player))
                {
                    if (IsCompanion(ped))
                    {
                        API.ClearPedTasks(ped);
                        continue;
                    }

                    foreach (var companion in companions)
                    {
                        API.TaskCombatPed(companion, ped, 0, 16);
                    }
                    return true;
                }
            }

            return false;
        }

        private static bool CheckAndHandleFreefall(int companion, Vector3 coords)
        {
            var paraState = API.GetPedParachuteState(companion);
            if (paraState == 1 || paraState == 2)
            {
                API.SetParachuteTaskTarget(companion, coords.X, coords.Y, coords.Z);
                return true;
            }

            if ((API.IsPedFalling(companion) || API.IsPedInParachuteFreeFall(companion)) &&
                !API.IsPedInAnyVehicle(companion, false))
            {
                API.SetPedSeeingRange(companion, 1f);
                API.SetPedHearingRange(companion, 1f);
                API.SetPedKeepTask(companion, true);
                API.GiveWeaponToPed(companion, Parachute, 1, false, true);
                API.TaskParachuteToTarget(companion, coords.X, coords.Y, coords.Z);
                return true;
            }

            API.SetPedSeeingRange(companion, 100f);
            API.SetPedHearingRange(companion, 100f);
            return false;
        }

        private static void FollorPlayerToVehicle(int player, IEnumerable<int> companions)
        {
            var vehicle = API.GetVehiclePedIsIn(player, false);
            var seats = Vehicles.GetFreeSeats(vehicle);

            foreach (var companion in companions)
            {
                if (seats.Count == 0)
                    break;

                if (!API.IsPedHuman(companion))
                    continue;

                if (API.IsPedInAnyVehicle(companion, true))
                {
                    var otherVehicle = API.GetVehiclePedIsUsing(companion);
                    if (otherVehicle != vehicle)
                        API.TaskLeaveVehicle(companion, otherVehicle, 0);

                    continue;
                }

                var seat = seats.Dequeue();
                API.TaskEnterVehicle(companion, vehicle, -1, seat, 2f, 1, 0);
            }
        }

        private static void FollowPlayer(int player, IEnumerable<int> companions)
        {
            var coords = API.GetEntityCoords(player, true);
            foreach (var companion in companions)
            {
                if (CheckAndHandleFreefall(companion, coords))
                    continue;

                var pos = API.GetEntityCoords(companion, true);
                if (API.IsPedInAnyVehicle(companion, true))
                {
                    var vehicle = API.GetVehiclePedIsIn(companion, false);
                    if (API.GetEntitySpeed(vehicle) < 0.1f)
                        API.TaskLeaveVehicle(companion, vehicle, 0);
                    else
                        API.TaskLeaveVehicle(companion, vehicle, 4096);
                }
                else if (pos.DistanceToSquared(coords) > 25f)
                {
                    if (API.IsPedActiveInScenario(companion))
                    {
                        API.ClearPedTasks(companion);
                        API.ClearPedTasksImmediately(companion);
                    }

                    API.TaskGoToEntity(companion, player, -1, 5f, 2f, 0, 0);
                }
                else if (API.IsPedHuman(companion))
                {
                    if (API.IsPedOnFoot(companion) && !API.IsPedUsingAnyScenario(companion))
                    {
                        var scenario = (Config.ScenarioList.Length > 0) ? Config.ScenarioList[API.GetRandomIntInRange(0, Config.ScenarioList.Length)] : "WORLD_HUMAN_STAND_MOBILE";
                        API.TaskStartScenarioInPlace(companion, scenario, 0, true);
                    }
                }
                else
                {
                    API.TaskLookAtEntity(companion, player, -1, 2048, 3);
                }
            }
        }

        private static Task Update()
        {
            var player = API.GetPlayerPed(-1);
            var peds = Peds.Get(Peds.Filter.LocalPlayer);
            var companions = Get(peds);

            if (companions.Count == 0 || CheckAndHandlePlayerCombat(peds, companions))
                return Delay(2000);

            if (API.IsPedInAnyVehicle(player, false))
                FollorPlayerToVehicle(player, companions);
            else
                FollowPlayer(player, companions);

            return Delay(2000);
        }
    }
}
