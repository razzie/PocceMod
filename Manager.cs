using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod
{
    public static class Manager
    {
        public static uint[] WeaponList { get; } = { 0x1B06D571, 0xBFEFFF6D, 0x1D073A89, 0x958A4A8F, 0x3656C8C1 };
        public static uint[] TrashPedList { get; } = { 0xE16D8F01, 0x6BD9B68C, 0xB097523B, 0x0E32D8D0, 0xE0E69974 };
        public static uint[] PocceList { get; } = { 0x780C01BD, 0x303638A7, 0xC79F6928, 0x445AC854, 0x028ABF95, 0x9FC7F637, 0x9CF26183, 0xBE086EFD, 0xDB134533, 0x5AA42C21, 0xC7496729, 0x5C2CF7F8, 0x20C8012F };
        public static uint[] PetList { get; } = { 0x573201B8, 0x14EC17EA, 0x4E8F95A2, 0x6D362854, 0x9563221D, 0x431FC24C, 0xA8683715 };
        public static string[] ScenarioList { get; } = { "WORLD_HUMAN_AA_COFFEE", "WORLD_HUMAN_AA_SMOKE", "WORLD_HUMAN_DRINKING", "WORLD_HUMAN_PARTYING", "WORLD_HUMAN_PUSH_UPS", "WORLD_HUMAN_SUPERHERO" };
        public static string PocceCompanionDecor = "POCCE_COMPANION";

        public static void Notification(string message, bool blink = false, bool saveToBrief = false)
        {
            API.SetNotificationTextEntry("CELL_EMAIL_BCON");
            foreach (string s in CitizenFX.Core.UI.Screen.StringToArray(message))
            {
                API.AddTextComponentSubstringPlayerName(s);
            }
            API.DrawNotification(blink, saveToBrief);
        }

        public static List<int> GetPeds(bool includeAnimals = true, bool includePlayers = false, bool includeDead = false, float radiusSquared = 900.0f)
        {
            var peds = new List<int>();
            int ped = 0;
            int handle = API.FindFirstPed(ref ped);
            var player = Game.Player.Character.Handle;
            var coords = Game.Player.Character.Position;

            if (handle == -1)
                return peds;

            do
            {
                var pos = new Ped(ped).Position;

                if (!includeAnimals && !API.IsPedHuman(ped))
                    continue;

                if (!includePlayers && API.IsPedAPlayer(ped))
                    continue;

                if (!includeDead && API.IsPedDeadOrDying(ped, true))
                    continue;

                if (coords.DistanceToSquared(pos) > 900.0f || ped == player)
                    continue;

                peds.Add(ped);

            } while (API.FindNextPed(handle, ref ped));

            API.EndFindPed(handle);

            return peds;
        }

        public static List<int> GetCompanions(IEnumerable<int> peds)
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

        public static async Task RequestModel(uint model)
        {
            while (!API.HasModelLoaded(model))
            {
                API.RequestModel(model);
                await BaseScript.Delay(10);
            }
        }

        public static async Task RequestControl(int entity)
        {
            while (!API.NetworkHasControlOfEntity(entity))
            {
                API.NetworkRequestControlOfEntity(entity);
                await BaseScript.Delay(10);
            }
        }

        public static async Task<int> SpawnVehicle(string model)
        {
            var pos = Game.Player.Character.Position;
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            if (API.IsPedInAnyVehicle(Game.Player.Character.Handle, false))
            {
                Notification("Player is in a vehicle");
                return -1;
            }

            await RequestModel(hash);
            var vehicle = API.CreateVehicle(hash, pos.X, pos.Y, pos.Z + 1.0f, Game.Player.Character.Heading, true, false);
            Game.Player.Character.SetIntoVehicle(new Vehicle(vehicle), VehicleSeat.Driver);
            return vehicle;
        }

        public static async Task<int> SpawnProp(string model)
        {
            var player = Game.Player.Character.Handle;
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            await RequestModel(hash);
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                var roofBone = API.GetEntityBoneIndexByName(vehicle, "roof");
                var pos = API.GetWorldPositionOfEntityBone(vehicle, roofBone);
                var prop = API.CreateObject((int)hash, pos.X, pos.Y, pos.Z + 2.0f, true, false, true);
                //API.AttachEntityToEntityPhysically(obj, vehicle, 0, roofBone, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1000.0f, true, false, true, true, 2);
                API.AttachEntityToEntity(prop, vehicle, roofBone, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, false, false, false, false, 0, true);
                return prop;
            }
            else
            {
                var pos = Game.Player.Character.Position;
                var heading = API.GetEntityRotation(player, 0).Z;
                var prop = API.CreateObject((int)hash, pos.X + (float)Math.Cos(heading), pos.Y + (float)Math.Sin(heading), pos.Z - 1.0f, true, false, true);
                API.SetEntityCollision(prop, true, true);
                return prop;
            }
        }

        public static async Task<int> SpawnPed(uint[] modelList, int pedType = 26)
        {
            var model = modelList[API.GetRandomIntInRange(0, modelList.Length)];
            var coords = Game.Player.Character.Position;
            var pos = new Vector3();

            if (!API.GetSafeCoordForPed(coords.X, coords.Y, coords.Z, true, ref pos, 16))
            {
                pos.X = coords.X;
                pos.Y = coords.Y;
                pos.Z = coords.Z + 1.0f;
            }

            await RequestModel(model);
            return API.CreatePed(pedType, model, pos.X, pos.Y, pos.Z, 0.0f, true, false);
        }

        public static async Task ArmPed(int ped, uint[] weaponList)
        {
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await BaseScript.Delay(10);

            API.SetPedCombatAbility(ped, 100);
            API.SetPedCombatMovement(ped, 2);
            API.SetPedCombatRange(ped, 2);
            API.SetPedHearingRange(ped, float.MaxValue);
            API.SetPedCombatAttributes(ped, 5, true);
            API.SetPedCombatAttributes(ped, 17, true);
            API.SetPedCombatAttributes(ped, 46, true);
            API.SetPedCombatAttributes(ped, 1424, true);
            API.SetPedFleeAttributes(ped, 0, false);
            API.SetEntityHealth(ped, 200);

            if (weaponList != null)
            {
                API.SetPedArmour(ped, 200);
                var weapon = weaponList[API.GetRandomIntInRange(0, weaponList.Length)];
                new Ped(ped).Weapons.Give((WeaponHash)weapon, int.MaxValue, true, true);
            }
        }

        public static async Task MakeCompanion(int ped)
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
            await BaseScript.Delay(10);

            API.TaskGoToEntity(ped, player, -1, 5.0f, 2.0f, 0, 0);
        }

        public static async Task UpdateCompanions()
        {
            var player = Game.Player.Character.Handle;
            var peds = GetPeds();
            var companions = GetCompanions(peds);

            if (companions.Count == 0)
                return;

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
                        API.TaskGoToEntity(companion, player, -1, 5.0f, 2.0f, 0, 0);
                    }
                    else if (API.IsPedHuman(companion))
                    {
                        if (!API.IsPedActiveInScenario(companion))
                        {
                            var heading = API.GetEntityHeading(companion);
                            var scenario = ScenarioList[API.GetRandomIntInRange(0, ScenarioList.Length)];
                            API.TaskLookAtEntity(companion, player, -1, 2048, 3);
                            API.TaskStandGuard(companion, pos.X, pos.Y, pos.Z, heading, scenario);
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
