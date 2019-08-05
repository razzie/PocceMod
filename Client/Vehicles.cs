using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Client.Menus;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
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
            Cruising = 8,
            EMP = 16
        }

        public const Filter DefaultFilters = Filter.PlayerVehicle;
        private const string LightMultiplierDecor = "POCCE_VEHICLE_LIGHT_MULTIPLIER";
        private const string StateFlagsDecor = "POCCE_VEHICLE_STATE_FLAGS";
        private static readonly Dictionary<int, int> _effects = new Dictionary<int, int>();

        public Vehicles()
        {
            API.DecorRegister(LightMultiplierDecor, 1);
            API.DecorRegister(StateFlagsDecor, 3);

            EventHandlers["PocceMod:EMP"] += new Func<int, Task>(async vehicle => await EMP(API.NetToVeh(vehicle)));
            EventHandlers["PocceMod:SetIndicator"] += new Action<int, int>((vehicle, state) => SetIndicator(API.NetToVeh(vehicle), state));
            EventHandlers["PocceMod:CompressVehicle"] += new Func<int, Task>(async vehicle => await Compress(API.NetToVeh(vehicle)));

            Tick += Update;
            Tick += UpdateEffects;
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
            var heading = API.GetEntityHeading(player);
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
            var vehicle = API.CreateVehicle(hash, pos.X, pos.Y, pos.Z + 1f, heading, true, false);
            API.SetPedIntoVehicle(player, vehicle, -1);

            if (API.GetEntityHeightAboveGround(vehicle) > 10f && (API.IsPedFalling(player) || API.IsPedJumping(player)))
            {
                if (API.IsThisModelAHeli(hash))
                {
                    API.SetHeliBladesFullSpeed(vehicle);
                }
                else if (API.IsThisModelAPlane(hash))
                {
                    var speed = API.GetVehicleMaxSpeed(vehicle) * 0.5f;
                    var headingRad = heading * (Math.PI / 180f);
                    API.SetEntityVelocity(vehicle, -(float)Math.Sin(headingRad) * speed, (float)Math.Cos(headingRad) * speed, 0f);
                    API.SetVehicleEngineOn(vehicle, true, true, true);
                    API.SetVehicleJetEngineOn(vehicle, true);
                }
            }

            API.SetModelAsNoLongerNeeded(hash);
            return vehicle;
        }

        public static async Task EMP()
        {
            API.StartScreenEffect("RaceTurbo", 500, false);

            var vehicles = Get(DefaultFilters, 900f);
            foreach (var vehicle in vehicles)
            {
                if (GetPlayers(vehicle).Count > 0)
                {
                    if (!Permission.CanDo(Ability.EMPOtherPlayer))
                        continue;

                    TriggerServerEvent("PocceMod:EMP", API.VehToNet(vehicle));
                }

                await EMP(vehicle);
            }
        }

        private static async Task EMP(int vehicle)
        {
            await Common.NetworkRequestControl(vehicle);

            var model = (uint)API.GetEntityModel(vehicle);
            if (API.IsThisModelAHeli(model) || API.IsThisModelAPlane(model))
                API.SetVehicleEngineHealth(vehicle, 1f);
            else
                API.SetVehicleEngineHealth(vehicle, 0f);
            
            API.SetVehicleLights(vehicle, 1);

            SetState(vehicle, StateFlag.EMP, true);
        }

        public static void ToggleUltrabrightHeadlight(int vehicle, bool notification = true)
        {
            if (!API.IsEntityAVehicle(vehicle))
                return;

            if (!API.DecorExistOn(vehicle, LightMultiplierDecor))
            {
                API.DecorSetFloat(vehicle, LightMultiplierDecor, 1f);

                if (notification)
                    Common.Notification("Use arrow up/down keys to change brightness");
            }
            else
            {
                API.DecorRemove(vehicle, LightMultiplierDecor);
                API.SetVehicleLightMultiplier(vehicle, 1f);
            }
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

        private static void TurnOnLights(int vehicle)
        {
            bool lightsOn = false;
            bool highbeamsOn = false;
            API.GetVehicleLightsState(vehicle, ref lightsOn, ref highbeamsOn);

            if (!lightsOn && !highbeamsOn)
                API.SetVehicleLights(vehicle, 0);

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

        private static async Task Compress(int vehicle)
        {
            var model = (uint)API.GetEntityModel(vehicle);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            var front = API.GetOffsetFromEntityInWorldCoords(vehicle, 0f, max.Y, 0f);
            var rear = API.GetOffsetFromEntityInWorldCoords(vehicle, 0f, min.Y, 0f);
            var left = API.GetOffsetFromEntityInWorldCoords(vehicle, min.X, 0f, 0f);
            var right = API.GetOffsetFromEntityInWorldCoords(vehicle, max.X, 0f, 0f);
            var top = API.GetOffsetFromEntityInWorldCoords(vehicle, 0f, 0f, max.Z);

            var sides = new Vector3[] { front, rear, left, right, top };

            for (int i = 0; i < 5; ++i)
            {
                foreach (var side in sides)
                {
                    API.SetVehicleDamage(vehicle, side.X, side.Y, side.Z, 100000f, i, false);
                }

                await Delay(100);
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

        private static bool ControlHeadlights(int vehicle)
        {
            if (!MainMenu.IsOpen && API.DecorExistOn(vehicle, LightMultiplierDecor))
            {
                if (API.IsControlPressed(0, 172)) // up
                {
                    if (!API.IsVehicleEngineOn(vehicle))
                        API.SetVehicleEngineOn(vehicle, true, true, false);

                    SetLightMultiplier(vehicle, GetLightMultiplier(vehicle) + 0.1f);
                    TurnOnLights(vehicle);
                    return true;
                }
                else if (API.IsControlPressed(0, 173)) // down
                {
                    if (!API.IsVehicleEngineOn(vehicle))
                        API.SetVehicleEngineOn(vehicle, true, true, false);

                    SetLightMultiplier(vehicle, GetLightMultiplier(vehicle) - 0.1f);
                    TurnOnLights(vehicle);
                    return true;
                }
            }

            return false;
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

                if (ControlHeadlights(vehicle))
                    return Task.FromResult(0);
                else
                    return Delay(1000);
            }
            else if (API.GetPedInVehicleSeat(vehicle, -1) != player)
            {
                return Delay(1000);
            }

            if (ControlHeadlights(vehicle))
                return Task.FromResult(0);
            else
                return Delay(100);
        }

        private static async Task UpdateEffects()
        {
            await Delay(1000);

            foreach (var pair in _effects.ToArray())
            {
                var vehicle = pair.Key;
                var effect = pair.Value;

                if (!API.DoesEntityExist(vehicle) || API.GetVehicleEngineHealth(vehicle) > 100f)
                {
                    SetState(vehicle, StateFlag.EMP, false);
                    API.StopParticleFxLooped(effect, false);
                    API.RemoveParticleFx(effect, false);
                    _effects.Remove(vehicle);
                }
            }

            var vehicles = Get(Filter.None).Where(vehicle => GetLastState(vehicle, StateFlag.EMP) && !_effects.ContainsKey(vehicle)).ToArray();
            if (vehicles.Length == 0)
                return;

            await Common.RequestPtfxAsset("core");

            foreach (var vehicle in vehicles)
            {
                API.UseParticleFxAssetNextCall("core");
                var engineBone = API.GetEntityBoneIndexByName(vehicle, "engine");
                var effect = API.StartParticleFxLoopedOnEntityBone("ent_amb_elec_crackle", vehicle, 0f, 0f, 0.1f, 0f, 0f, 0f, engineBone, 1f, false, false, false);
                _effects.Add(vehicle, effect);
            }

        }
    }
}
