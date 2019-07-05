using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public static class Props
    {
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

        public static async Task<int> Spawn(string model)
        {
            var player = Game.Player.Character.Handle;
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Hud.Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            await Common.RequestModel(hash);
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
    }
}
