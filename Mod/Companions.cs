using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public class Companions : BaseScript
    {
        private static readonly string PocceCompanionFlagDecor = "POCCE_COMPANION_FLAG";
        private static readonly string PocceCompanionPlayerDecor = "POCCE_COMPANION_PLAYER";

        public Companions()
        {
            API.DecorRegister(PocceCompanionFlagDecor, 2);
            API.DecorRegister(PocceCompanionPlayerDecor, 3);

            Tick += async () =>
            {
                await Delay(2000);
                await Update();
            };
        }

        private static bool IsCompanion(int ped)
        {
            return API.DecorGetBool(ped, PocceCompanionFlagDecor) && API.DecorGetInt(ped, PocceCompanionPlayerDecor) == API.PlayerId();
        }

        public static List<int> Get(IEnumerable<int> peds)
        {
            var companions = new List<int>();

            foreach (var ped in peds)
            {
                if (IsCompanion(ped))
                    companions.Add(ped);
            }

            return companions;
        }

        public static async Task Add(int ped)
        {
            var playerID = API.PlayerId();
            var player = Game.Player.Character.Handle;
            var playerGroup = API.GetPedGroupIndex(player);

            API.DecorSetBool(ped, PocceCompanionFlagDecor, true);
            API.DecorSetInt(ped, PocceCompanionPlayerDecor, playerID);
            API.SetPedRelationshipGroupHash(ped, (uint)API.GetPedRelationshipGroupHash(player));
            var blip = API.AddBlipForEntity(ped);
            API.SetBlipAsFriendly(blip, true);

            API.ClearPedTasksImmediately(ped);
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await Delay(10);

            API.TaskGoToEntity(ped, player, -1, 5.0f, 2.0f, 0, 0);
        }

        private static async Task Update()
        {
            var player = Game.Player.Character.Handle;
            var peds = Peds.Get();
            var companions = Get(peds);

            int target = 0;
            if (API.GetEntityPlayerIsFreeAimingAt(API.PlayerId(), ref target) || API.GetPlayerTargetEntity(API.PlayerId(), ref target))
            {
                foreach (var companion in companions)
                {
                    API.TaskCombatPed(companion, target, 0, 16);
                }
                return;
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
                    return;
                }
            }

            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                foreach (var companion in companions)
                {
                    if (!API.IsPedHuman(companion))
                        continue;

                    if (!API.AreAnyVehicleSeatsFree(vehicle))
                        break;

                    if (API.IsPedInAnyVehicle(companion, true))
                    {
                        var otherVehicle = API.GetVehiclePedIsUsing(companion);
                        if (otherVehicle != vehicle)
                        {
                            API.TaskLeaveVehicle(companion, otherVehicle, 0);
                            await Delay(10);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (Vehicles.GetFreeSeat(vehicle, out int seat))
                        API.TaskEnterVehicle(companion, vehicle, -1, seat, 2.0f, 1, 0);
                    else
                        break;
                }
            }
            else
            {
                foreach (var companion in companions)
                {
                    var pos = API.GetEntityCoords(companion, true);
                    if (API.IsPedInAnyVehicle(companion, true))
                    {
                        var vehicle = API.GetVehiclePedIsIn(companion, false);
                        if (API.IsVehicleStopped(vehicle))
                            API.TaskLeaveVehicle(companion, vehicle, 0);
                        else
                            API.TaskLeaveVehicle(companion, vehicle, 4096);
                    }
                    else if (pos.DistanceToSquared(Game.Player.Character.Position) > 25.0f)
                    {
                        if (API.IsPedActiveInScenario(companion))
                        {
                            API.ClearPedTasks(companion);
                            API.ClearPedTasksImmediately(companion);
                        }

                        API.TaskGoToEntity(companion, player, -1, 5.0f, 2.0f, 0, 0);
                    }
                    else if (API.IsPedHuman(companion))
                    {
                        if (!API.IsPedActiveInScenario(companion))
                        {
                            var heading = API.GetEntityHeading(companion);
                            var scenario = Config.ScenarioList[API.GetRandomIntInRange(0, Config.ScenarioList.Length)];
                            //API.TaskLookAtEntity(companion, player, -1, 2048, 3);
                            API.TaskStartScenarioInPlace(companion, scenario, 0, true);
                        }
                    }
                    else
                    {
                        API.TaskLookAtEntity(companion, player, -1, 2048, 3);
                    }
                }
            }
        }
    }
}
