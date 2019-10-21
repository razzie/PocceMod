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
    using TurboBoostMode = Effect.TurboBoostEffect.Mode;

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
        public enum StateFlag
        {
            HazardLight = 1,
            TiresIntact = 2,
            EngineIntact = 4,
            Cruising = 8,
            EMP = 16
        }

        [Flags]
        public enum FeatureFlag
        {
            BackToTheFuture = 1,
            TurboBoost = 2,
            AntiGravity = 4,
            RemoteControl = 8,
            JesusMode = 16
        }

        public enum Light
        {
            Headlight,
            LeftIndicator,
            RightIndicator,
            HazardLight
        }

        public delegate void StateChangedDelegate(int vehicle, StateFlag states);
        public static event StateChangedDelegate StateChanged;

        public delegate void FeatureChangedDelegate(int vehicle, FeatureFlag features);
        public static event FeatureChangedDelegate FeatureChanged;

        public const Filter DefaultFilters = Filter.PlayerVehicle;
        private const string CustomHornDecor = "POCCE_CUSTOM_HORN";
        private const string LightMultiplierDecor = "POCCE_VEHICLE_LIGHT_MULTIPLIER";
        private const string StateFlagsDecor = "POCCE_VEHICLE_STATE_FLAGS";
        private const string FeatureFlagsDecor = "POCCE_VEHICLE_FEATURE_FLAGS";
        private static readonly int TurboBoostKey;

        static Vehicles()
        {
            TurboBoostKey = Config.GetConfigInt("TurboBoostKey");
        }

        public Vehicles()
        {
            API.DecorRegister(CustomHornDecor, 3);
            API.DecorRegister(LightMultiplierDecor, 1);
            API.DecorRegister(StateFlagsDecor, 3);
            API.DecorRegister(FeatureFlagsDecor, 3);

            EventHandlers["PocceMod:EMP"] += new Func<int, Task>(NetEMP);
            EventHandlers["PocceMod:SetIndicator"] += new Action<int, int>(NetSetIndicator);
            EventHandlers["PocceMod:CompressVehicle"] += new Func<int, Task>(NetCompress);
            EventHandlers["PocceMod:ToggleTurboBoost"] += new Func<int, bool, int, Task>(NetToggleTurboBoost);
            EventHandlers["PocceMod:ToggleHorn"] += new Func<int, bool, Task>(NetToggleHorn);

            if (!Config.GetConfigBool("DisableAutoHazardLights"))
                Tick += Telemetry.Wrap("auto_hazard_lights", UpdateAutoHazardLights);

            Tick += Telemetry.Wrap("vehicle_effects", UpdateEffects);
            Tick += Telemetry.Wrap("vehicle_controls", UpdateControls);
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

                if (rangeSquared > 0f && coords.DistanceToSquared(pos) > rangeSquared)
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

            bool autoLaunchAircraft = API.IsPedFalling(player) || API.GetPedParachuteState(player) != -1 || API.IsPedJumping(player);

            await Common.RequestModel(hash);
            var vehicle = API.CreateVehicle(hash, pos.X, pos.Y, pos.Z + 1f, heading, true, false);
            API.SetPedIntoVehicle(player, vehicle, -1);
            API.SetVehicleNumberPlateText(vehicle, "POCCE");

            if (Common.GetEntityHeightAboveGround(vehicle) > 10f && autoLaunchAircraft)
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

            if (API.IsBigVehicle(vehicle))
                API.SetEntityLodDist(vehicle, 0xFFFF);

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

        private static Task NetEMP(int netVehicle)
        {
            return EMP(API.NetToVeh(netVehicle));
        }

        public static void Compress(int vehicle)
        {
            TriggerServerEvent("PocceMod:CompressVehicle", API.VehToNet(vehicle));
        }

        private static async Task NetCompress(int netVehicle)
        {
            var vehicle = API.NetToVeh(netVehicle);
            if (!API.DoesEntityExist(vehicle))
                return;

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

        public static void SetCustomHorn(int vehicle, int horn)
        {
            API.DecorSetInt(vehicle, CustomHornDecor, horn);
        }

        public static string GetCustomHorn(int vehicle)
        {
            var horn = API.DecorGetInt(vehicle, CustomHornDecor);
            return (horn < Config.HornList.Length) ? Config.HornList[horn] : "SIRENS_AIRHORN";
        }

        public static bool HasCustomHorn(int vehicle)
        {
            return API.DecorExistOn(vehicle, CustomHornDecor);
        }

        public static void ToggleLightMultiplier(int vehicle)
        {
            if (!API.DecorExistOn(vehicle, LightMultiplierDecor))
            {
                API.DecorSetFloat(vehicle, LightMultiplierDecor, 1f);
                Common.Notification("Use arrow up/down keys to change brightness");
            }
            else
            {
                API.DecorRemove(vehicle, LightMultiplierDecor);
                API.SetVehicleLightMultiplier(vehicle, 1f);
            }
        }

        public static float GetLightMultiplier(int vehicle)
        {
            if (!API.DecorExistOn(vehicle, LightMultiplierDecor))
                return 1f;

            return API.DecorGetFloat(vehicle, LightMultiplierDecor);
        }

        public static void SetLightMultiplier(int vehicle, float multiplier)
        {
            if (multiplier < 0.25f)
                multiplier = 0.25f;

            API.DecorSetFloat(vehicle, LightMultiplierDecor, multiplier);
            API.SetVehicleLightMultiplier(vehicle, multiplier);
        }

        public static void TurnOnLight(int vehicle, Light light)
        {
            if (!API.IsVehicleEngineOn(vehicle))
                API.SetVehicleEngineOn(vehicle, true, true, false);

            switch (light)
            {
                case Light.Headlight:
                    API.SetVehicleLights(vehicle, 3);
                    break;

                case Light.LeftIndicator:
                    TriggerServerEvent("PocceMod:SetIndicator", API.VehToNet(vehicle), 1);
                    break;

                case Light.RightIndicator:
                    TriggerServerEvent("PocceMod:SetIndicator", API.VehToNet(vehicle), 2);
                    break;

                case Light.HazardLight:
                    TriggerServerEvent("PocceMod:SetIndicator", API.VehToNet(vehicle), 3);
                    break;
            }
        }

        public static void TurnOffLight(int vehicle, Light light)
        {
            switch (light)
            {
                case Light.Headlight:
                    API.SetVehicleLights(vehicle, 1);
                    break;

                case Light.LeftIndicator:
                case Light.RightIndicator:
                case Light.HazardLight:
                    TriggerServerEvent("PocceMod:SetIndicator", API.VehToNet(vehicle), 0);
                    break;
            }
        }

        private static void NetSetIndicator(int netVehicle, int state)
        {
            var vehicle = API.NetToVeh(netVehicle);
            if (!API.DoesEntityExist(vehicle))
                return;

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

        private static async Task NetToggleHorn(int netVehicle, bool state)
        {
            var vehicle = API.NetToVeh(netVehicle);
            if (!API.DoesEntityExist(vehicle))
                return;

            if (state)
                await Effects.AddHornEffect(vehicle);
            else
                Effects.RemoveHornEffect(vehicle);
        }

        private static async Task NetToggleTurboBoost(int netVehicle, bool state, int mode)
        {
            var vehicle = API.NetToVeh(netVehicle);
            if (!API.DoesEntityExist(vehicle))
                return;

            if (state)
                await Effects.AddTurboBoostEffect(vehicle, (TurboBoostMode)mode);
            else
                Effects.RemoveTurboBoostEffect(vehicle);
        }

        public static bool IsFeatureEnabled(int vehicle, FeatureFlag flag)
        {
            var state = (FeatureFlag)API.DecorGetInt(vehicle, FeatureFlagsDecor);
            return (state & flag) == flag;
        }

        public static void SetFeatureEnabled(int vehicle, FeatureFlag flag, bool value)
        {
            var state = (FeatureFlag)API.DecorGetInt(vehicle, FeatureFlagsDecor);

            if (value)
                state |= flag;
            else
                state &= ~flag;

            API.DecorSetInt(vehicle, FeatureFlagsDecor, (int)state);

            FeatureChanged?.Invoke(vehicle, state);
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

            StateChanged?.Invoke(vehicle, state);
        }

        public static bool AreTiresIntact(int vehicle)
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

            StateChanged?.Invoke(vehicle, state);
        }

        private static bool IsHazardLightApplicable(uint model)
        {
            return API.IsThisModelACar(model) || API.IsThisModelABike(model) || API.IsThisModelAnAmphibiousCar(model) || API.IsThisModelAQuadbike(model);
        }

        public static void UpdateAutoHazardLights(int vehicle)
        {
            var speed = API.GetEntitySpeedVector(vehicle, true).Y;
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
                    TurnOnLight(vehicle, Light.HazardLight);
                }
            }
            else if (speed > 5f && GetLastState(vehicle, StateFlag.HazardLight))
            {
                SetState(vehicle, StateFlag.HazardLight, false);
                TurnOffLight(vehicle, Light.HazardLight);
            }

            UpdateState(vehicle);
        }

        private static Task UpdateAutoHazardLights()
        {
            var player = API.GetPlayerPed(-1);
            var vehicle = API.GetVehiclePedIsIn(player, !API.IsPedInAnyVehicle(player, false));

            if (API.IsVehicleSeatFree(vehicle, -1))
            {
                UpdateAutoHazardLights(vehicle);

                if (API.GetEntitySpeed(vehicle) > 1f && !GetLastState(vehicle, StateFlag.HazardLight))
                {
                    SetState(vehicle, StateFlag.HazardLight, true);
                    TurnOnLight(vehicle, Light.HazardLight);
                }
            }
            else if (API.GetPedInVehicleSeat(vehicle, -1) == player)
            {
                UpdateAutoHazardLights(vehicle);
            }

            return Delay(100);
        }

        private static async Task UpdateEffects()
        {
            await Delay(100);

            var vehicles = Get(Filter.None);
            if (vehicles.Count == 0)
                return;

            foreach (var vehicle in vehicles.Where(vehicle => GetLastState(vehicle, StateFlag.EMP)))
            {
                var engineHealth = API.GetVehicleEngineHealth(vehicle);
                if (engineHealth > 100f)
                    SetState(vehicle, StateFlag.EMP, false);
                else
                    await Effects.AddEMPEffect(vehicle);
            }

            foreach (var vehicle in vehicles.Where(vehicle => IsFeatureEnabled(vehicle, FeatureFlag.BackToTheFuture)))
            {
                await Effects.AddWheelFireEffect(vehicle);
            }

            foreach (var vehicle in vehicles.Where(vehicle => IsFeatureEnabled(vehicle, FeatureFlag.JesusMode)))
            {
                await Effects.AddJesusEffect(vehicle);
            }

            foreach (var vehicle in vehicles)
            {
                if (IsFeatureEnabled(vehicle, FeatureFlag.AntiGravity))
                    AntiGravity.Add(vehicle, 0.7f);
                else
                    AntiGravity.Remove(vehicle);
            }
        }

        private static bool IsAnyRemoteControlPressed()
        {
            return
                API.IsControlPressed(0, 172) ||
                API.IsControlPressed(0, 173) ||
                API.IsControlPressed(0, 174) ||
                API.IsControlPressed(0, 175);
        }

        private static Task UpdateControls()
        {
            var player = API.GetPlayerPed(-1);
            var vehicle = API.GetVehiclePedIsIn(player, !API.IsPedInAnyVehicle(player, false));
            if (API.IsEntityDead(vehicle))
            {
                if (Common.GetClosestEntity(Autopilot.Get(true, -1).Select(tup => tup.Item2).Where(v => !API.IsEntityDead(v)), out vehicle))
                    API.SetPlayersLastVehicle(vehicle);
                else
                    return Delay(100);
            }

            var driver = API.GetPedInVehicleSeat(vehicle, -1);
            var isAutopilot = Autopilot.IsOwnedAutopilot(driver);
            var hasOtherDriver = !API.IsVehicleSeatFree(vehicle, -1) && driver != player && !isAutopilot;

            if (hasOtherDriver || MainMenu.IsOpen || API.IsEntityDead(vehicle))
                return Delay(100);

            if (API.DecorExistOn(vehicle, CustomHornDecor))
            {
                if (API.IsControlJustPressed(0, 86)) // INPUT_VEH_HORN
                {
                    TriggerServerEvent("PocceMod:ToggleHorn", API.VehToNet(vehicle), true);
                }
                else if (API.IsControlJustReleased(0, 86))
                {
                    TriggerServerEvent("PocceMod:ToggleHorn", API.VehToNet(vehicle), false);
                }
            }

            if (API.DecorExistOn(vehicle, LightMultiplierDecor))
            {
                if (API.IsControlPressed(0, 172)) // up
                {
                    SetLightMultiplier(vehicle, GetLightMultiplier(vehicle) + 0.1f);
                    TurnOnLight(vehicle, Light.Headlight);
                }
                else if (API.IsControlPressed(0, 173)) // down
                {
                    SetLightMultiplier(vehicle, GetLightMultiplier(vehicle) - 0.1f);
                    TurnOnLight(vehicle, Light.Headlight);
                }
            }

            if (TurboBoostKey > 0 && IsFeatureEnabled(vehicle, FeatureFlag.TurboBoost))
            {
                var mode = TurboBoostMode.Custom;
                if (API.IsControlPressed(0, 21)) // LEFT_SHIFT
                    mode = TurboBoostMode.Vertical;
                else if (API.IsControlPressed(0, 36)) // LEFT_CTRL
                    mode = TurboBoostMode.Horizontal;

                if (API.IsControlJustPressed(0, TurboBoostKey) || API.IsDisabledControlJustPressed(0, TurboBoostKey))
                    TriggerServerEvent("PocceMod:ToggleTurboBoost", API.VehToNet(vehicle), true, (int)mode);
                else if (API.IsControlJustReleased(0, TurboBoostKey) || API.IsDisabledControlJustReleased(0, TurboBoostKey))
                    TriggerServerEvent("PocceMod:ToggleTurboBoost", API.VehToNet(vehicle), false, 0);
            }

            if (driver != player && IsFeatureEnabled(vehicle, FeatureFlag.RemoteControl) && IsAnyRemoteControlPressed())
            {
                if (API.IsVehicleSeatFree(vehicle, -1))
                    return Autopilot.Spawn(vehicle);
                
                if (API.IsPedInFlyingVehicle(driver))
                {
                    var model = (uint)API.GetEntityModel(vehicle);
                    var isHeli = API.IsThisModelAHeli(model);

                    var force = isHeli ? 0.1f : 1f;
                    if (API.IsControlPressed(0, 172)) // up
                        Common.ApplyTorque(vehicle, force, 0);
                    else if (API.IsControlPressed(0, 173)) // down
                        Common.ApplyTorque(vehicle, -force, 0);

                    if (API.IsControlPressed(0, 174)) // left
                        Common.ApplyTorque(vehicle, 0, force / 2);
                    else if (API.IsControlPressed(0, 175)) // right
                        Common.ApplyTorque(vehicle, 0, -force / 2);

                    if (isHeli)
                        API.TaskVehicleTempAction(driver, vehicle, 9, 1);

                    return Delay(33);
                }
                else // non-aircraft
                {
                    if (API.IsControlPressed(0, 172)) // up
                    {
                        API.TaskVehicleTempAction(driver, vehicle, 9, 1);

                        if (API.IsControlPressed(0, 174)) // left
                            API.TaskVehicleTempAction(driver, vehicle, 7, 1);
                        else if (API.IsControlPressed(0, 175)) // right
                            API.TaskVehicleTempAction(driver, vehicle, 8, 1);
                    }
                    else if (API.IsControlPressed(0, 173)) // down
                    {
                        API.TaskVehicleTempAction(driver, vehicle, 22, 1);

                        if (API.IsControlPressed(0, 174)) // left
                            API.TaskVehicleTempAction(driver, vehicle, 13, 1);
                        else if (API.IsControlPressed(0, 175)) // right
                            API.TaskVehicleTempAction(driver, vehicle, 14, 1);
                    }
                    else
                    {
                        if (API.IsControlPressed(0, 174)) // left
                            API.TaskVehicleTempAction(driver, vehicle, 4, 1);
                        else if (API.IsControlPressed(0, 175)) // right
                            API.TaskVehicleTempAction(driver, vehicle, 5, 1);
                    }
                }
            }

            return Task.FromResult(0);
        }
    }
}
