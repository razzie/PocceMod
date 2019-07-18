using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public static class Peds
    {
        [Flags]
        public enum Filter
        {
            None = 0,
            Animals = 1,
            Players = 2,
            Dead = 4,
            CurrentVehiclePassengers = 8
        }

        public const Filter DefaultFilters = Filter.Dead;

        public static List<int> Get(Filter exclude = DefaultFilters, float rangeSquared = 1600f)
        {
            var peds = new List<int>();
            int ped = 0;
            int handle = API.FindFirstPed(ref ped);
            var player = API.GetPlayerPed(-1);
            var coords = API.GetEntityCoords(player, true);
            var vehicle = API.GetVehiclePedIsIn(player, false);

            if (!API.IsPedInAnyVehicle(player, false))
                exclude &= ~Filter.CurrentVehiclePassengers;

            if (handle == -1)
                return peds;

            bool HasFilter(Filter filter)
            {
                return (exclude & filter) == filter;
            }

            do
            {
                var pos = API.GetEntityCoords(ped, true);

                if (HasFilter(Filter.Animals) && !API.IsPedHuman(ped))
                    continue;

                if (HasFilter(Filter.Players) && API.IsPedAPlayer(ped))
                    continue;

                if (HasFilter(Filter.Dead) && API.IsPedDeadOrDying(ped, true))
                    continue;

                if (HasFilter(Filter.CurrentVehiclePassengers) && API.GetVehiclePedIsIn(ped, false) == vehicle)
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared || ped == player)
                    continue;

                peds.Add(ped);

            } while (API.FindNextPed(handle, ref ped));

            API.EndFindPed(handle);
            return peds;
        }

        public static async Task<int> Spawn(uint[] modelList, int pedType = 26)
        {
            if (modelList.Length == 0)
                return -1;

            var model = modelList[API.GetRandomIntInRange(0, modelList.Length)];
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);
            var pos = new Vector3();

            if (!API.GetSafeCoordForPed(coords.X, coords.Y, coords.Z, true, ref pos, 16))
            {
                pos.X = coords.X;
                pos.Y = coords.Y;
                pos.Z = coords.Z + 1f;
            }

            await Common.RequestModel(model);
            return API.CreatePed(pedType, model, pos.X, pos.Y, pos.Z, 0f, true, false);
        }

        public static void GiveWeapon(int ped, uint weapon)
        {
            API.GiveWeaponToPed(ped, weapon, 1, false, true);

            int ammoInClip = API.GetMaxAmmoInClip(ped, weapon, false);
            API.SetAmmoInClip(ped, weapon, ammoInClip);

            var ammo = int.MaxValue;
            API.GetMaxAmmo(ped, weapon, ref ammo);
            API.SetPedAmmo(ped, weapon, ammo);
        }

        public static async Task Arm(int ped, uint[] weaponList)
        {
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await BaseScript.Delay(10);

            API.SetPedCombatAbility(ped, 100);
            API.SetPedCombatMovement(ped, 2);
            API.SetPedCombatRange(ped, 2);
            API.SetPedHearingRange(ped, float.MaxValue);
            API.SetPedCombatAttributes(ped, 2, true);
            API.SetPedCombatAttributes(ped, 5, true);
            API.SetPedCombatAttributes(ped, 17, true);
            API.SetPedCombatAttributes(ped, 46, true);
            API.SetPedCombatAttributes(ped, 1424, true);
            API.SetPedFleeAttributes(ped, 0, false);
            API.SetEntityHealth(ped, 200);

            if (weaponList != null && weaponList.Length > 0)
            {
                API.SetPedArmour(ped, 200);
                var weapon = weaponList[API.GetRandomIntInRange(0, weaponList.Length)];
                GiveWeapon(ped, weapon);
            }
        }
    }
}
