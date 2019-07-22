using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Client.Menus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Vehicles : BaseScript
    {
        [Flags]
        public enum Filter
        {
            None = 0,
            WithDriver = 1,
            PlayerVehicle = 2
        }

        [Flags]
        private enum StateFlag
        {
            HazardLight = 1,
            TiresIntact = 2,
            EngineIntact = 4,
            Cruising = 8
        }

        public const Filter DefaultFilters = Filter.PlayerVehicle;
        private const string LightMultiplierDecor = "POCCE_VEHICLE_LIGHT_MULTIPLIER";
        private const string StateFlagsDecor = "POCCE_VEHICLE_STATE_FLAGS";

        public Vehicles()
        {
            API.DecorRegister(LightMultiplierDecor, 1);
            API.DecorRegister(StateFlagsDecor, 3);

            EventHandlers["PocceMod:EMP"] += new Action<int>(vehicle => EMP(API.NetToVeh(vehicle)));
            EventHandlers["PocceMod:SetIndicator"] += new Action<int, int>((vehicle, state) => SetIndicator(API.NetToVeh(vehicle), state));

            Tick += Update;
        }

        public static List<int> Get(Filter exclude = DefaultFilters, float rangeSquared = 3600f)
        {
            var vehicles = new List<int>();
            int vehicle = 0;
            int handle = API.FindFirstVehicle(ref vehicle);
            var player = API.GetPlayerPed(-1);
            var playerVehicle = API.GetVehiclePedIsIn(player, false);
            var coords = API.GetEntityCoords(player, true);

            if (handle == -1)
                return vehicles;

            bool HasFilter(Filter filter)
            {
                return (exclude & filter) == filter;
            }

            do
            {
                var pos = API.GetEntityCoords(vehicle, false);

                if (HasFilter(Filter.PlayerVehicle) && vehicle == playerVehicle)
                    continue;

                if (HasFilter(Filter.WithDriver) && !API.IsVehicleSeatFree(vehicle, -1))
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared)
                    continue;

                vehicles.Add(vehicle);

            } while (API.FindNextVehicle(handle, ref vehicle));

            API.EndFindVehicle(handle);
            return vehicles;
        }

        public static bool GetFreeSeat(int vehicle, out int seat, bool forcePassenger = false)
        {
            var model = (uint)API.GetEntityModel(vehicle);
            int seats = API.GetVehicleModelNumberOfSeats(model) - 1;
            int minSeat = -1;

            if (forcePassenger)
            {
                minSeat = (seats > 1) ? 1 : 0;
            }

            for (seat = minSeat; seat < seats; ++seat)
            {
                if (API.IsVehicleSeatFree(vehicle, seat))
                    return true;
            }

            if (forcePassenger && API.IsVehicleSeatFree(vehicle, 0))
            {
                seat = 0;
                return true;
            }

            return false;
        }

        public static Queue<int> GetFreeSeats(int vehicle)
        {
            var model = (uint)API.GetEntityModel(vehicle);
            int seats = API.GetVehicleModelNumberOfSeats(model) - 1;
            var freeSeats = new Queue<int>();

            for (int seat = -1; seat < seats; ++seat)
            {
                if (API.IsVehicleSeatFree(vehicle, seat))
                    freeSeats.Enqueue(seat);
            }

            return freeSeats;
        }

        public static bool GetPedSeat(int vehicle, int ped, out int seat)
        {
            var model = (uint)API.GetEntityModel(vehicle);
            var seats = API.GetVehicleModelNumberOfSeats(model) - 1;

            for (seat = -1; seat < seats; ++seat)
            {
                var seatPed = API.GetPedInVehicleSeat(vehicle, seat);
                if (seatPed == ped)
                    return true;
            }

            return false;
        }

        public static List<int> GetPlayers(int vehicle)
        {
            var players = new List<int>();
            var model = (uint)API.GetEntityModel(vehicle);
            var seats = API.GetVehicleModelNumberOfSeats(model) - 1;

            for (int seat = -1; seat < seats; ++seat)
            {
                var ped = API.GetPedInVehicleSeat(vehicle, seat);
                if (API.IsPedAPlayer(ped))
                    players.Add(API.NetworkGetPlayerIndexFromPed(ped));
            }

            return players;
        }

        public static async Task<int> Spawn(string model)
        {
            var player = API.GetPlayerPed(-1);
            var pos = API.GetEntityCoords(player, true);
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Common.Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            if (API.IsPedInAnyVehicle(API.GetPlayerPed(-1), false))
            {
                Common.Notification("Player is in a vehicle");
                return -1;
            }

            await Common.RequestModel(hash);
            var vehicle = API.CreateVehicle(hash, pos.X, pos.Y, pos.Z + 1f, API.GetEntityHeading(player), true, false);
            API.SetPedIntoVehicle(player, vehicle, -1);

            if (API.IsThisModelAHeli(hash) && API.GetEntityHeightAboveGround(vehicle) > 10f)
                API.SetHeliBladesFullSpeed(vehicle);

            API.SetModelAsNoLongerNeeded(hash);
            return vehicle;
        }

        public static void EMP(float rangeSquared = 900f)
        {
            API.StartScreenEffect("RaceTurbo", 500, false);

            var vehicles = Get(DefaultFilters, rangeSquared);
            foreach (var vehicle in vehicles)
            {
                if (GetPlayers(vehicle).Count > 0)
                    TriggerServerEvent("PocceMod:EMP", API.VehToNet(vehicle));

                EMP(vehicle);
            }
        }

        private static void EMP(int vehicle)
        {
            var model = (uint)API.GetEntityModel(vehicle);
            if (API.IsThisModelAHeli(model) || API.IsThisModelAPlane(model))
                API.SetVehicleEngineHealth(vehicle, 1f);
            else
                API.SetVehicleEngineHealth(vehicle, 0f);
            
            API.SetVehicleLights(vehicle, 1);
        }

        public static void EnableUltrabrightHeadlight(int vehicle)
        {
            if (!API.IsEntityAVehicle(vehicle))
                return;

            if (!API.DecorExistOn(vehicle, LightMultiplierDecor))
                API.DecorSetFloat(vehicle, LightMultiplierDecor, 1f);
        }

        private static float GetLightMultiplier(int vehicle)
        {
            if (!API.DecorExistOn(vehicle, LightMultiplierDecor))
                return 1f;

            return API.DecorGetFloat(vehicle, LightMultiplierDecor);
        }

        private static void SetLightMultiplier(int vehicle, float multiplier)
        {
            if (multiplier < 0.25f)
                multiplier = 0.25f;

            API.DecorSetFloat(vehicle, LightMultiplierDecor, multiplier);
            API.SetVehicleLightMultiplier(vehicle, multiplier);
        }

        private static void SetIndicator(int vehicle, int state)
        {
            switch (state)
            {
                case 0:
                    API.SetVehicleIndicatorLights(vehicle, 0, false);
                    API.SetVehicleIndicatorLights(vehicle, 1, false);
                    break;

                case 1:
                    API.SetVehicleIndicatorLights(vehicle, 0, false);
                    API.SetVehicleIndicatorLights(vehicle, 1, true);
                    break;

                case 2:
                    API.SetVehicleIndicatorLights(vehicle, 0, true);
                    API.SetVehicleIndicatorLights(vehicle, 1, false);
                    break;

                case 3:
                    API.SetVehicleIndicatorLights(vehicle, 0, true);
                    API.SetVehicleIndicatorLights(vehicle, 1, true);
                    break;
            }
        }

        private static bool GetLastState(int vehicle, StateFlag flag)
        {
            var state = (StateFlag)API.DecorGetInt(vehicle, StateFlagsDecor);
            return (state & flag) == flag;
        }

        private static void SetState(int vehicle, StateFlag flag, bool value)
        {
            var state = (StateFlag)API.DecorGetInt(vehicle, StateFlagsDecor);

            if (value)
                state |= flag;
            else
                state &= ~flag;

            API.DecorSetInt(vehicle, StateFlagsDecor, (int)state);
        }

        private static bool AreTiresIntact(int vehicle)
        {
            var wheels = API.GetVehicleNumberOfWheels(vehicle);
            for (int wheel = 0; wheel < wheels; ++wheel)
            {
                if (API.IsVehicleTyreBurst(vehicle, wheel, false))
                    return false;
            }

            return true;
        }

        private static void UpdateState(int vehicle)
        {
            var state = (StateFlag)API.DecorGetInt(vehicle, StateFlagsDecor);

            void set(StateFlag flag, bool value)
            {
                if (value)
                    state |= flag;
                else
                    state &= ~flag;
            }

            set(StateFlag.TiresIntact, AreTiresIntact(vehicle));
            set(StateFlag.EngineIntact, API.GetVehicleEngineHealth(vehicle) >= 100f);
            set(StateFlag.Cruising, API.GetEntitySpeed(vehicle) > 10f);

            API.DecorSetInt(vehicle, StateFlagsDecor, (int)state);
        }

        private static bool IsHazardLightApplicable(uint model)
        {
            return API.IsThisModelACar(model) || API.IsThisModelABike(model) || API.IsThisModelAnAmphibiousCar(model) || API.IsThisModelAQuadbike(model);
        }

        public static void UpdateAutoHazardLights(int vehicle)
        {
            var speed = API.GetEntitySpeed(vehicle);
            var model = (uint)API.GetEntityModel(vehicle);

            if (!IsHazardLightApplicable(model))
                return;

            if (API.IsEntityUpsidedown(vehicle) ||
                API.IsEntityInAir(vehicle) ||
                API.IsEntityInWater(vehicle) ||
                API.IsEntityOnFire(vehicle) ||
                (speed < 3f && GetLastState(vehicle, StateFlag.Cruising)) ||
                (!AreTiresIntact(vehicle) && GetLastState(vehicle, StateFlag.TiresIntact)) ||
                (API.GetVehicleEngineHealth(vehicle) < 100f && GetLastState(vehicle, StateFlag.EngineIntact)))
            {
                if (!GetLastState(vehicle, StateFlag.HazardLight))
                {
                    SetState(vehicle, StateFlag.HazardLight, true);
                    TriggerServerEvent("PocceMod:SetIndicator", API.VehToNet(vehicle), 3);
                }
            }
            else if (speed > 5f && GetLastState(vehicle, StateFlag.HazardLight))
            {
                SetState(vehicle, StateFlag.HazardLight, false);
                TriggerServerEvent("PocceMod:SetIndicator", API.VehToNet(vehicle), 0);
            }

            UpdateState(vehicle);
        }

        private static Task Update()
        {
            var player = API.GetPlayerPed(-1);
            var vehicle = API.GetVehiclePedIsIn(player, !API.IsPedInAnyVehicle(player, false));
            var speed = API.GetEntitySpeed(vehicle);

            UpdateAutoHazardLights(vehicle);

            if (API.IsVehicleSeatFree(vehicle, -1))
            {
                if (speed > 1f && !GetLastState(vehicle, StateFlag.HazardLight))
                {
                    SetState(vehicle, StateFlag.HazardLight, true);
                    TriggerServerEvent("PocceMod:SetIndicator", API.VehToNet(vehicle), 3);
                }
                return Delay(1000);
            }
            else if (API.GetPedInVehicleSeat(vehicle, -1) != player)
            {
                return Delay(1000);
            }

            if (!MainMenu.IsOpen && API.DecorExistOn(vehicle, LightMultiplierDecor))
            {
                if (API.IsControlPressed(0, 172)) // up
                {
                    SetLightMultiplier(vehicle, GetLightMultiplier(vehicle) + 0.1f);
                    return Delay(0);
                }
                else if (API.IsControlPressed(0, 173)) // down
                {
                    SetLightMultiplier(vehicle, GetLightMultiplier(vehicle) - 0.1f);
                    return Delay(0);
                }
            }

            return Delay(100);
        }
    }
}
