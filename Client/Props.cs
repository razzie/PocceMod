using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Client.Menus;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Props : BaseScript
    {
        private const string PropDecor = "POCCE_PROP";
        private static readonly int PropUndoKey;
        private static readonly List<int> _props = new List<int>();
        private static bool _firstSpawn = true;

        [Flags]
        public enum Filter
        {
            None = 0,
            Stock = 1 // props that weren't placed by players
        }

        public const Filter DefaultFilters = Filter.Stock;

        static Props()
        {
            PropUndoKey = Config.GetConfigInt("PropUndoKey");
        }

        public Props()
        {
            API.DecorRegister(PropDecor, 2);

            Tick += Update;
        }

        public static List<int> Get(Filter exclude = DefaultFilters, float rangeSquared = 3600f)
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

        public static bool IsPocceProp(int prop)
        {
            return API.DecorGetBool(prop, PropDecor);
        }

        public static Task<int> Spawn(string model)
        {
            if (_firstSpawn)
            {
                Common.Notification("Use the arrow keys to correct the position of the prop");
                _firstSpawn = false;
            }

            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                return SpawnOnEntity(vehicle, model);
            }
            else
            {
                 return SpawnInFrontOfPed(player, model);
            }
        }

        public static async Task<int> SpawnAtCoords(string model, Vector3 coords, Vector3 rotation, bool freeze = false)
        {
            var hash = (uint)API.GetHashKey(model);
            if (!API.IsModelValid(hash))
            {
                Common.Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            await Common.RequestModel(hash);
            var prop = API.CreateObject((int)hash, coords.X, coords.Y, coords.Z, true, false, false);
            _props.Add(prop);
            API.SetModelAsNoLongerNeeded(hash);

            API.SetEntityRotation(prop, rotation.X, rotation.Y, rotation.Z, 0, true);
            API.DecorSetBool(prop, PropDecor, true);
            await Common.RequestCollision(hash);

            if (!API.DoesEntityHavePhysics(prop) || freeze)
                API.FreezeEntityPosition(prop, true);

            return prop;
        }

        private static Task<int> SpawnInFrontOfPed(int ped, string model)
        {
            var pedModel = (uint)API.GetEntityModel(ped);
            var pos = API.GetEntityCoords(ped, true);
            var heading = API.GetEntityHeading(ped);
            var headingRad = MathUtil.DegreesToRadians(heading);

            return SpawnAtCoords(model, new Vector3(pos.X - (float)Math.Sin(headingRad), pos.Y + (float)Math.Cos(headingRad), pos.Z - 1f), new Vector3(0f, 0f, heading));
        }

        private static async Task<int> SpawnOnEntity(int entity, string model)
        {
            var pos = API.GetEntityCoords(entity, API.IsEntityAPed(entity));
            var heading = API.GetEntityHeading(entity);
            var entityModel = (uint)API.GetEntityModel(entity);
            Vector3 entityMin = Vector3.Zero;
            Vector3 entityMax = Vector3.Zero;
            API.GetModelDimensions(entityModel, ref entityMin, ref entityMax);

            Vector3 propMin = Vector3.Zero;
            Vector3 propMax = Vector3.Zero;
            API.GetModelDimensions((uint)API.GetHashKey(model), ref propMin, ref propMax);

            var prop = await SpawnAtCoords(model, new Vector3(pos.X, pos.Y, pos.Z + entityMax.Z - propMin.Z), new Vector3(0f, 0f, heading));
            if (!API.DoesEntityHavePhysics(prop) || propMax.Z - propMin.Z > 3f) // large objects glitch too much
                API.AttachEntityToEntity(prop, entity, 0, 0f, 0f, -propMin.Z, 0f, 0f, 0f, false, false, false, false, 0, true);
            else
                API.AttachEntityToEntityPhysically(prop, entity, 0, 0, 0f, 0f, entityMax.Z, 0f, 0f, propMin.Z, 0f, 0f, 0f, 100f, true, false, true, true, 2);

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

        private static float GetEntityHeight(int entity)
        {
            var model = (uint)API.GetEntityModel(entity);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);
            return max.Z - min.Z;
        }

        private static Task Update()
        {
            if (MainMenu.IsOpen || _props.Count == 0)
                return Delay(1000);

            var player = API.GetPlayerPed(-1);
            var coords = (Vector2)API.GetEntityCoords(player, true);

            var prop = _props[_props.Count - 1];
            var pos = API.GetEntityCoords(prop, false);
            var rotation = API.GetEntityRotation(prop, 0);

            if (Vector2.DistanceSquared(coords, (Vector2)pos) < 100f)
            {
                Common.GetCamHorizontalForwardAndRightVectors(out Vector3 forward, out Vector3 right);
                var speed = Math.Max(0.05f * GetEntityHeight(prop), 0.05f);
                forward *= speed;
                right *= speed;

                if (API.IsControlPressed(0, 21)) // LEFT_SHIFT
                {
                    if (API.IsControlPressed(0, 172)) // up
                        API.SetEntityCoords(prop, pos.X + forward.X, pos.Y + forward.Y, pos.Z, true, false, false, false);
                    else if (API.IsControlPressed(0, 173)) // down
                        API.SetEntityCoords(prop, pos.X - forward.X, pos.Y - forward.Y, pos.Z, true, false, false, false);

                    if (API.IsControlPressed(0, 174)) // left
                        API.SetEntityCoords(prop, pos.X - right.X, pos.Y - right.Y, pos.Z, false, true, false, false);
                    else if (API.IsControlPressed(0, 175)) // right
                        API.SetEntityCoords(prop, pos.X + right.X, pos.Y + right.Y, pos.Z, false, true, false, false);
                }
                else
                {
                    if (API.IsControlPressed(0, 172)) // up
                        API.SetEntityCoords(prop, pos.X, pos.Y, pos.Z + speed, false, false, true, false);
                    else if (API.IsControlPressed(0, 173)) // down
                        API.SetEntityCoords(prop, pos.X, pos.Y, pos.Z - speed, false, false, true, false);

                    if (API.IsControlPressed(0, 174)) // left
                        API.SetEntityRotation(prop, rotation.X, rotation.Y, rotation.Z + 1f, 0, true);
                    else if (API.IsControlPressed(0, 175)) // right
                        API.SetEntityRotation(prop, rotation.X, rotation.Y, rotation.Z - 1f, 0, true);
                }

                if (PropUndoKey > 0 && API.IsControlJustPressed(0, PropUndoKey))
                    ClearLast();

                return Delay(0);
            }

            return Delay(1000);
        }
    }
}
