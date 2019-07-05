using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;

namespace PocceMod.Mod
{
    public static class Ropes
    {
        public static int PlayerAttach(int entity)
        {
            var player = Game.Player.Character.Handle;
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                return Attach(vehicle, entity);
            }
            else
            {
                return Attach(player, entity);
            }
        }

        public static int Attach(int entity1, int entity2)
        {
            var pos1 = API.GetEntityCoords(entity1, API.IsEntityAPed(entity1));
            var pos2 = API.GetEntityCoords(entity2, API.IsEntityAPed(entity2));
            var length = (float)Math.Sqrt(pos1.DistanceToSquared(pos2));

            int unkPtr = 0;
            var rope = API.AddRope(pos1.X, pos1.Y, pos1.Z, 0.0f, 0.0f, 0.0f, length, 1, length, 1.0f, 0.0f, false, false, false, 5.0f, true, ref unkPtr);
            API.AttachEntitiesToRope(rope, entity1, entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, length, false, false, null, null);
            return rope;
        }
    }
}
