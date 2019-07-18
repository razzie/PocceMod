using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Props : BaseScript
    {
        private const string PropDecor = "POCCE_PROP";
        private static readonly List<int> _props = new List<int>();
        private static bool _firstSpawn = true;

        [Flags]
        public enum Filter
        {
            None = 0,
            Stock = 1 // props that weren't placed by players
        }

        public const Filter DefaultFilters = Filter.Stock;

        public Props()
        {
            API.DecorRegister(PropDecor, 2);

            Tick += Update;
        }

        public static List<int> Get(Filter exclude = DefaultFilters, float rangeSquared = 3600.0f)
        {
            var props = new List<int>();
            int prop = 0;
            int handle = API.FindFirstObject(ref prop);
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);

            if (handle == -1)
                return props;

            bool HasFilter(Filter filter)
            {
                return (exclude & filter) == filter;
            }

            do
            {
                var pos = API.GetEntityCoords(prop, false);

                if (HasFilter(Filter.Stock) && !API.DecorGetBool(prop, PropDecor))
                    continue;

                if (API.IsEntityAPed(prop) || API.IsEntityAVehicle(prop))
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared)
                    continue;

                props.Add(prop);

            } while (API.FindNextObject(handle, ref prop));

            API.EndFindObject(handle);
            return props;
        }

        public static Task<int> Spawn(string model)
        {
            var player = API.GetPlayerPed(-1);
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Hud.Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return Task.FromResult(-1);
            }

            if (_firstSpawn)
            {
                Hud.Notification("Use the arrow keys to correct the position of the prop");
                _firstSpawn = false;
            }

            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                return SpawnOnEntity(vehicle, hash);
            }
            else
            {
                 return SpawnInFrontOfPed(player, hash);
            }
        }

        public static async Task<int> SpawnAtCoords(string model, Vector3 coords, Vector3 rotation)
        {
            var hash = (uint)API.GetHashKey(model);
            if (!API.IsModelValid(hash))
            {
                Hud.Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            await Common.RequestModel(hash);
            var prop = API.CreateObject((int)hash, coords.X, coords.Y, coords.Z, true, false, false);
            _props.Add(prop);

            API.SetEntityRotation(prop, rotation.X, rotation.Y, rotation.Z, 0, true);
            API.DecorSetBool(prop, PropDecor, true);
            return prop;
        }

        private static async Task<int> SpawnInFrontOfPed(int ped, uint model)
        {
            var pedModel = (uint)API.GetEntityModel(ped);
            var pos = API.GetEntityCoords(ped, true);
            var heading = API.GetEntityHeading(ped);
            var headingRad = (Math.PI / 180) * heading;

            await Common.RequestModel(model);
            var prop = API.CreateObject((int)model, pos.X - (float)Math.Sin(headingRad), pos.Y + (float)Math.Cos(headingRad), pos.Z - 1.0f, true, false, true);
            _props.Add(prop);

            API.SetEntityHeading(prop, -heading);
            API.SetEntityCollision(prop, true, true);
            API.ActivatePhysics(prop);
            API.DecorSetBool(prop, PropDecor, true);
            return prop;
        }

        private static async Task<int> SpawnOnEntity(int entity, uint model)
        {
            var subentity = API.GetEntityAttachedTo(entity);
            if (subentity > 0)
                return await SpawnOnEntity(subentity, model);

            var pos = API.GetEntityCoords(entity, API.IsEntityAPed(entity));
            var entityModel = (uint)API.GetEntityModel(entity);
            Vector3 entityMin = Vector3.Zero;
            Vector3 entityMax = Vector3.Zero;
            API.GetModelDimensions(entityModel, ref entityMin, ref entityMax);

            Vector3 propMin = Vector3.Zero;
            Vector3 propMax = Vector3.Zero;
            API.GetModelDimensions(model, ref propMin, ref propMax);

            await Common.RequestModel(model);
            var prop = API.CreateObject((int)model, pos.X, pos.Y, pos.Z + entityMax.Z - propMin.Z, true, false, true);
            _props.Add(prop);

            if (!API.DoesEntityHavePhysics(prop) || propMax.Z - propMin.Z > 3.0f) // large objects glitch too much
                API.AttachEntityToEntity(prop, entity, 0, 0.0f, 0.0f, -propMin.Z, 0.0f, 0.0f, 0.0f, false, false, false, false, 0, true);
            else
                API.AttachEntityToEntityPhysically(prop, entity, 0, 0, 0.0f, 0.0f, entityMax.Z, 0.0f, 0.0f, propMin.Z, 0.0f, 0.0f, 0.0f, 100.0f, true, false, true, true, 2);

            API.ActivatePhysics(prop);
            API.DecorSetBool(prop, PropDecor, true);
            return prop;
        }

        public static void ClearAll()
        {
            foreach (var prop in _props)
            {
                var tmp_prop = prop;
                API.DeleteObject(ref tmp_prop);
            }

            _props.Clear();
        }

        public static void ClearLast()
        {
            if (_props.Count == 0)
                return;

            var prop = _props[_props.Count - 1];
            API.DeleteObject(ref prop);
            _props.RemoveAt(_props.Count - 1);
        }

        private static Task Update()
        {
            if (_props.Count == 0)
                return Delay(1000);

            var player = API.GetPlayerPed(-1);
            var coords = API.GetEntityCoords(player, true);

            var prop = _props[_props.Count - 1];
            var pos = API.GetEntityCoords(prop, false);
            var rotation = API.GetEntityRotation(prop, 0);

            if (coords.DistanceToSquared(pos) < 100f)
            {
                if (API.IsControlPressed(0, 172)) // up
                    API.SetEntityCoords(prop, pos.X, pos.Y, pos.Z + 0.01f, false, false, true, false);
                else if (API.IsControlPressed(0, 173)) // down
                    API.SetEntityCoords(prop, pos.X, pos.Y, pos.Z - 0.01f, false, false, true, false);

                if (API.IsControlPressed(0, 174)) // left
                    API.SetEntityRotation(prop, rotation.X, rotation.Y, rotation.Z + 1f, 0, true);
                else if (API.IsControlPressed(0, 175)) // right
                    API.SetEntityRotation(prop, rotation.X, rotation.Y, rotation.Z - 1f, 0, true);

                return Delay(0);
            }

            return Delay(1000);
        }
    }
}
