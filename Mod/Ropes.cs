using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;

namespace PocceMod.Mod
{
    public class Ropes : BaseScript
    {
        private static Dictionary<int, List<int>> _ropes = new Dictionary<int, List<int>>();

        public Ropes()
        {
            EventHandlers["PocceMod:AddRope"] += new Action<int, int, int>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<int>(ClearRopes);
        }

        private static Vector3 GetAdjustedPosition(int entity, float front)
        {
            var right = Vector3.Zero;
            var forward = Vector3.Zero;
            var up = Vector3.Zero;
            var pos = Vector3.Zero;
            API.GetEntityMatrix(entity, ref right, ref forward, ref up, ref pos);

            if (!API.IsEntityAVehicle(entity))
                return pos;

            var model = (uint)API.GetEntityModel(entity);
            if (API.IsThisModelAHeli(model))
                return pos;

            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            if (front > 0)
                right *= (max.X * front);
            else
                right *= (-min.X * front);

            pos += right;
            return pos;
        }

        private static void AddRope(int player, int entity1, int entity2)
        {
            entity1 = API.NetToEnt(entity1);
            entity2 = API.NetToEnt(entity2);

            var pos1 = GetAdjustedPosition(entity1, -0.75f);
            var pos2 = GetAdjustedPosition(entity2, 0.75f);
            var length = (float)Math.Sqrt(pos1.DistanceToSquared(pos2));

            int unkPtr = 0;
            var rope = API.AddRope(pos1.X, pos1.Y, pos1.Z, 0.0f, 0.0f, 0.0f, length, 1, length, 1.0f, 0.0f, false, false, false, 5.0f, true, ref unkPtr);
            API.AttachEntitiesToRope(rope, entity1, entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, length, false, false, null, null);

            if (_ropes.TryGetValue(player, out List<int> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(player, new List<int> { rope });

            if (!API.RopeAreTexturesLoaded())
                API.RopeLoadTextures();
        }

        private static void ClearRopes(int player)
        {
            if (_ropes.TryGetValue(player, out List<int> playerRopes))
            {
                foreach (var rope in playerRopes)
                {
                    var tmp_rope = rope;
                    API.DeleteRope(ref tmp_rope);
                }

                playerRopes.Clear();
            }
        }

        public static void PlayerAttach(int entity)
        {
            var player = Game.Player.Character.Handle;
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                Attach(vehicle, entity);
            }
            else
            {
                Attach(player, entity);
            }
        }

        public static void Attach(int entity1, int entity2)
        {
            TriggerServerEvent("PocceMod:AddRope", Game.Player.Handle, API.ObjToNet(entity1), API.ObjToNet(entity2));
        }

        public static void Clear()
        {
            TriggerServerEvent("PocceMod:ClearRopes", Game.Player.Handle);
        }
    }
}
