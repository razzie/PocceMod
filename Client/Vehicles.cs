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
        public enum StateFlag
        {
            HazardLight = 1,
            TiresIntact = 2,
            EngineIntact = 4,
            Cruising = 8,
            EMP = 16,
            BackToTheFuture = 32,
            TurboBoost = 64,
            AntiGravity = 128,
        }

        public enum Light
        {
            Headlight,
            LeftIndicator,
            RightIndicator,
            HazardLight
        }

        public const Filter DefaultFilters = Filter.PlayerVehicle;
        private const string AircraftHornDecor = "POCCE_AIRCRAFT_HORN";
        private const string LightMultiplierDecor = "POCCE_VEHICLE_LIGHT_MULTIPLIER";
        private const string StateFlagsDecor = "POCCE_VEHICLE_STATE_FLAGS";
        private static readonly int TurboBoostKey;

        static Vehicles()
        {
            TurboBoostKey = Config.GetConfigInt("TurboBoostKey");
        }

        public Vehicles()
        {
            API.DecorRegister(AircraftHornDecor, 3);
            API.DecorRegister(LightMultiplierDecor, 1);
            API.DecorRegister(StateFlagsDecor, 3);

            EventHandlers["PocceMod:EMP"] += new Func<int, Task>(NetEMP);
            EventHandlers["PocceMod:SetIndicator"] += new Action<int, int>(NetSetIndicator);
            EventHandlers["PocceMod:CompressVehicle"] += new Func<int, Task>(NetCompress);
            EventHandlers["PocceMod:ToggleTurboBoost"] += new Func<int, bool, Task>(NetToggleTurboBoost);
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

            bool autoLaunchAircraft = API.IsPedFalling(player) || API.GetPedParachuteState(player) != -1 || API.IsPedJumping(player);

            await Common.RequestModel(hash);
            var vehicle = API.CreateVehicle(hash, pos.X, pos.Y, pos.Z + 1f, heading, true, false);
            API.SetPedIntoVehicle(player, vehicle, -1);
            API.SetVehicleNumberPlateText(vehicle, "POCCE");

            if (API.GetEntityHeightAboveGround(vehicle) > 10f && autoLaunchAircraft)
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

        private static Task NetEMP(int netVehicle)
        {
            return EMP(API.NetToVeh(netVehicle));
        }

        public static void ToggleUltrabrightHeadlight()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

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

        public static void ToggleBackToTheFuture()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var model = (uint)API.GetEntityModel(vehicle);
            if (!API.IsThisModelACar(model) && !API.IsThisModelABike(model) && !API.IsThisModelAQuadbike(model))
            {
                Common.Notification("Only cars, bikes and quadbikes are supported");
                return;
            }

            var state = !GetLastState(vehicle, StateFlag.BackToTheFuture);
            SetState(vehicle, StateFlag.BackToTheFuture, state);

            if (state)
            {
                Common.Notification("Back to the Future!");
                API.SetVehicleTyresCanBurst(vehicle, false);
                API.SetDisableVehiclePetrolTankFires(vehicle, true);
            }
        }

        public static void ToggleTurboBoost()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var state = !GetLastState(vehicle, StateFlag.TurboBoost);
            SetState(vehicle, StateFlag.TurboBoost, state);

            if (state)
                Common.Notification("Turbo Boost enabled");
        }

        public static void CargobobMagnet()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                var pilot = API.GetPedInVehicleSeat(heli, -1);

                if (pilot != player && !Autopilot.IsOwnedAutopilot(pilot))
                {
                    Common.Notification("Player is not the pilot of this heli");
                    return;
                }

                if (API.IsCargobobMagnetActive(heli))
                {
                    API.SetCargobobPickupMagnetActive(heli, false);
                    API.RetractCargobobHook(heli);
                }
                else
                {
                    API.EnableCargobobHook(heli, 1);
                    API.SetCargobobPickupMagnetActive(heli, true);
                }
            }
            else
            {
                Common.Notification("Player is not in a heli");
            }
        }
        
        public static void CompressVehicle()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

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

        public static void ToggleAntiGravity()
        {
            if (!Common.EnsurePlayerIsVehicleDriver(out int player, out int vehicle))
                return;

            var state = GetLastState(vehicle, StateFlag.AntiGravity);
            SetState(vehicle, StateFlag.AntiGravity, !state);
        }

        public static void SetAircraftHorn(int horn)
        {
            var player = API.GetPlayerPed(-1);
            if (!API.IsPedInFlyingVehicle(player))
            {
                Common.Notification("Player is not in a flying vehicle");
                return;
            }

            if (!Common.EnsurePlayerIsVehicleDriver(out player, out int vehicle))
                return;

            SetAircraftHorn(vehicle, horn);
        }

        public static void SetAircraftHorn(int aircraft, int horn)
        {
            API.DecorSetInt(aircraft, AircraftHornDecor, horn);
        }

        public static string GetAircraftHorn(int aircraft)
        {
            var horn = API.DecorGetInt(aircraft, AircraftHornDecor);
            return (horn < Config.HornList.Length) ? Config.HornList[horn] : "SIRENS_AIRHORN";
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
                await Effects.AddHornEffect(API.NetToVeh(vehicle));
            else
                Effects.RemoveHornEffect(API.NetToVeh(vehicle));
        }

        private static async Task NetToggleTurboBoost(int netVehicle, bool state)
        {
            var vehicle = API.NetToVeh(netVehicle);
            if (!API.DoesEntityExist(vehicle))
                return;

            if (state)
                await Effects.AddTurboBoostEffect(vehicle);
            else
                Effects.RemoveTurboBoostEffect(vehicle);
        }

        public static bool GetLastState(int vehicle, StateFlag flag)
        {
            var state = (StateFlag)API.DecorGetInt(vehicle, StateFlagsDecor);
            return (state & flag) == flag;
        }

        public static void SetState(int vehicle, StateFlag flag, bool value)
        {
            var state = (StateFlag)API.DecorGetInt(vehicle, StateFlagsDecor);

            if (value)
                state |= flag;
            else
                state &= ~flag;

            API.DecorSetInt(vehicle, StateFlagsDecor, (int)state);
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
                await Effects.AddEMPEffect(vehicle);
            }

            foreach (var vehicle in vehicles.Where(vehicle => GetLastState(vehicle, StateFlag.BackToTheFuture)))
            {
                await Effects.AddWheelFireEffect(vehicle);
            }

            foreach (var vehicle in vehicles)
            {
                if (GetLastState(vehicle, StateFlag.AntiGravity))
                    AntiGravity.Add(vehicle, 0.7f);
                else
                    AntiGravity.Remove(vehicle);
            }
        }

        private static Task UpdateControls()
        {
            var player = API.GetPlayerPed(-1);
            var vehicle = API.GetVehiclePedIsIn(player, !API.IsPedInAnyVehicle(player, false));
            var driver = API.GetPedInVehicleSeat(vehicle, -1);
            var hasOtherDriver = !API.IsVehicleSeatFree(vehicle, -1) && driver != player && !Autopilot.IsOwnedAutopilot(driver);

            if (hasOtherDriver || MainMenu.IsOpen || API.IsEntityDead(vehicle))
                return Delay(100);

            if (API.IsPedInFlyingVehicle(player) && API.DecorExistOn(vehicle, AircraftHornDecor))
            {
                if (API.GetPedInVehicleSeat(vehicle, -1) != player || API.IsEntityDead(vehicle) || !API.DecorExistOn(vehicle, AircraftHornDecor))
                    return Delay(1000);

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

            if (TurboBoostKey > 0 && GetLastState(vehicle, StateFlag.TurboBoost))
            {
                if (API.IsControlJustPressed(0, TurboBoostKey) || API.IsDisabledControlJustPressed(0, TurboBoostKey))
                    TriggerServerEvent("PocceMod:ToggleTurboBoost", API.VehToNet(vehicle), true);
                else if (API.IsControlJustReleased(0, TurboBoostKey) || API.IsDisabledControlJustReleased(0, TurboBoostKey))
                    TriggerServerEvent("PocceMod:ToggleTurboBoost", API.VehToNet(vehicle), false);
            }

            return Task.FromResult(0);
        }
    }
}
