using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public class Companions : BaseScript
    {
        private static readonly string PocceCompanionDecor = "POCCE_COMPANION";

        public Companions()
        {
            Tick += async () =>
            {
                await Delay(2000);
                await Update();
            };
        }

        public static List<int> Get(IEnumerable<int> peds)
        {
            var companions = new List<int>();

            foreach (var ped in peds)
            {
                if (API.DecorGetBool(ped, PocceCompanionDecor) && API.NetworkHasControlOfEntity(ped))
                    companions.Add(ped);

                //API.DoesEntityBelongToThisScript(ped, )
            }

            return companions;
        }

        public static async Task Add(int ped)
        {
            var player = Game.Player.Character.Handle;
            var playerGroup = API.GetPedGroupIndex(player);

            if (!API.DecorIsRegisteredAsType(PocceCompanionDecor, 2))
                API.DecorRegister(PocceCompanionDecor, 2);

            API.DecorSetBool(ped, PocceCompanionDecor, true);
            API.SetPedRelationshipGroupHash(ped, (uint)API.GetPedRelationshipGroupHash(player));

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
            if (API.GetEntityPlayerIsFreeAimingAt(API.GetPlayerIndex(), ref target) || API.GetPlayerTargetEntity(API.GetPlayerIndex(), ref target))
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
                    if (API.DecorGetBool(ped, PocceCompanionDecor))
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
                            await BaseScript.Delay(10);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var model = (uint)API.GetEntityModel(vehicle);
                    int seats = API.GetVehicleModelNumberOfSeats(model);

                    for (int seat = -1; seat < seats; ++seat)
                    {
                        if (API.IsVehicleSeatFree(vehicle, seat))
                        {
                            API.TaskEnterVehicle(companion, vehicle, -1, 0, 2.0f, 1, 0);
                            continue;
                        }
                    }
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
