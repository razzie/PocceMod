using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public static class Props
    {
        private static List<int> _props = new List<int>();

        public static List<int> Get(float rangeSquared = 100.0f)
        {
            var props = new List<int>();
            int prop = 0;
            int handle = API.FindFirstObject(ref prop);
            var coords = Game.Player.Character.Position;

            if (handle == -1)
                return props;

            do
            {
                var pos = API.GetEntityCoords(prop, false);

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
            var player = Game.Player.Character.Handle;
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Hud.Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return Task.FromResult(-1);
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

        private static async Task<int> SpawnInFrontOfPed(int ped, uint model)
        {
            var pedModel = (uint)API.GetEntityModel(ped);
            var pos = API.GetEntityCoords(ped, true);
            var heading = (Math.PI / 180) * API.GetEntityHeading(ped);

            await Common.RequestModel(model);
            var prop = API.CreateObject((int)model, pos.X - (float)Math.Sin(heading), pos.Y + (float)Math.Cos(heading), pos.Z - 1.0f, true, false, true);
            _props.Add(prop);

            API.SetEntityCollision(prop, true, true);

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
            var prop = API.CreateObject((int)model, pos.X, pos.Y, pos.Z, true, false, true);
            _props.Add(prop);

            if (!API.DoesEntityHavePhysics(prop) || propMax.Z - propMin.Z > 2.0f) // large objects glitch too much
                API.AttachEntityToEntity(prop, entity, 0, 0.0f, 0.0f, -propMin.Z, 0.0f, 0.0f, 0.0f, false, false, false, false, 0, true);
            else
                API.AttachEntityToEntityPhysically(prop, entity, 0, 0, 0.0f, 0.0f, entityMax.Z, 0.0f, 0.0f, propMin.Z, 0.0f, 0.0f, 0.0f, 100.0f, true, false, true, false, 2);

            return prop;
        }

        public static void Clear()
        {
            foreach (var prop in _props)
            {
                var tmp_prop = prop;
                API.DeleteObject(ref tmp_prop);
            }

            _props.Clear();
        }
    }
}
