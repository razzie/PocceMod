using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Peds : BaseScript
    {
        [Flags]
        public enum Filter
        {
            None = 0,
            Animals = 1,
            Players = 2,
            NonPlayers = 4,
            LocalPlayer = 8,
            Dead = 16,
            VehiclePassengers= 32,
            CurrentVehiclePassengers = 64
        }

        public const Filter DefaultFilters = Filter.LocalPlayer | Filter.Dead;
        private const string BurnDecor = "POCCE_BURN";
        private static readonly List<int> _knownPeds = new List<int>();

        public Peds()
        {
            API.DecorRegister(BurnDecor, 2);

            EventHandlers["PocceMod:RequestMPSkin"] += new Action<int, int>(NetRequestMPSkin);
            EventHandlers["PocceMod:SetMPSkin"] += new Action<int, byte[]>(NetSetMPSkin);

            Tick += Telemetry.Wrap("peds", Update);
        }

        public static List<int> Get(Filter exclude = DefaultFilters, float rangeSquared = 1600f)
        {
            var peds = new List<int>();
            int ped = 0;
            int handle = API.FindFirstPed(ref ped);
            var player = API.GetPlayerPed(-1);
            var coords = API.GetEntityCoords(player, false);
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

                if (HasFilter(Filter.LocalPlayer) && ped == player)
                    continue;

                if (API.IsPedAPlayer(ped))
                {
                    if (HasFilter(Filter.Players))
                        continue;
                }
                else
                {
                    if (HasFilter(Filter.NonPlayers))
                        continue;
                }

                if (HasFilter(Filter.Dead) && API.IsPedDeadOrDying(ped, true))
                    continue;

                if (HasFilter(Filter.VehiclePassengers) && API.IsPedInAnyVehicle(ped, true))
                    continue;

                if (HasFilter(Filter.CurrentVehiclePassengers) && API.GetVehiclePedIsIn(ped, false) == vehicle)
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared)
                    continue;

                peds.Add(ped);

            } while (API.FindNextPed(handle, ref ped));

            API.EndFindPed(handle);
            return peds;
        }

        public static Vector3 GetSafeCoords(Vector3 coords)
        {
            var pos = new Vector3();
            if (!API.GetSafeCoordForPed(coords.X, coords.Y, coords.Z, true, ref pos, 16))
            {
                pos.X = coords.X;
                pos.Y = coords.Y;
                pos.Z = coords.Z + 1f;
            }

            return pos;
        }

        public static async Task<int> Spawn(uint model, Vector3 coords, bool trySafeCoords = true, int pedType = 26)
        {
            await Common.RequestModel(model);
            var pos = trySafeCoords ? GetSafeCoords(coords) : coords;
            var ped = API.CreatePed(pedType, model, pos.X, pos.Y, pos.Z, 0f, true, false);
            API.SetModelAsNoLongerNeeded(model);
            API.SetEntityHeading(ped, API.GetRandomFloatInRange(0f, 360f));
            return ped;
        }

        public static Task<int> Spawn(uint[] modelList, int pedType = 26)
        {
            if (modelList.Length == 0)
                return Task.FromResult(-1);

            var model = modelList[API.GetRandomIntInRange(0, modelList.Length)];
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);
            return Spawn(model, coords, true, pedType);
        }

        public static async Task<int> SpawnInRange(uint[] modelList, Vector3 center, float minRange, float maxRange, int pedType = 26)
        {
            if (modelList.Length == 0)
                return -1;

            var model = modelList[API.GetRandomIntInRange(0, modelList.Length)];
            var coords = Common.GetRandomSpawnCoordsInRange(center, minRange, maxRange, out float heading);
            var ped = await Spawn(model, coords, false, pedType);
            return ped;
        }

        public static async Task Arm(int ped, uint[] weaponList)
        {
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await Delay(10);

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
                Weapons.Give(ped, weapon);
            }
        }

        public static void Burn(int ped)
        {
            API.DecorSetBool(ped, BurnDecor, true);
        }

        private static void NetRequestMPSkin(int netPed, int requestingPlayer)
        {
            var skin = new MultiplayerSkin(API.NetToPed(netPed));
            TriggerServerEvent("PocceMod:SetMPSkin", netPed, skin.Serialize(), requestingPlayer);
        }

        private static void NetSetMPSkin(int netPed, dynamic data)
        {
            var skin = MultiplayerSkin.Deserialize(data);
            skin.Restore(API.NetToPed(netPed));
        }

        private static Task Update()
        {
            foreach (var ped in _knownPeds.ToArray())
            {
                if (!API.DoesEntityExist(ped))
                    _knownPeds.Remove(ped);
            }

            var playerID = API.PlayerId();
            var peds = Get(Filter.Players | Filter.Dead).Except(_knownPeds);

            foreach (var ped in peds)
            {
                if (Skin.IsMultiplayerPed(ped) &&
                    API.NetworkGetEntityIsNetworked(ped) &&
                    API.NetworkGetEntityOwner(ped) != playerID)
                {
                    var owner = API.NetworkGetEntityOwner(ped);
                    TriggerServerEvent("PocceMod:RequestMPSkin", API.PedToNet(ped), API.GetPlayerServerId(owner));
                }

                if (API.DecorGetBool(ped, BurnDecor) && !API.IsEntityOnFire(ped))
                    API.StartEntityFire(ped);
            }

            _knownPeds.AddRange(peds);

            return Delay(1000);
        }
    }
}
