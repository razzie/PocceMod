using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public class Common : BaseScript
    {
        public Common()
        {
            EventHandlers["PocceMod:Burn"] += new Action<int>(entity => API.StartEntityFire(API.NetToEnt(entity)));
        }

        public static async Task RequestModel(uint model)
        {
            while (!API.HasModelLoaded(model))
            {
                API.RequestModel(model);
                await Delay(10);
            }
        }

        public static bool GetClosestEntity(IEnumerable<int> entities, out int closest)
        {
            closest = -1;
            bool found = false;
            float minDist = float.MaxValue;
            var coords = Game.Player.Character.Position;

            foreach (var entity in entities)
            {
                var pos = API.GetEntityCoords(entity, API.IsEntityAPed(entity));
                var dist = coords.DistanceToSquared(pos);

                if (dist < minDist)
                {
                    closest = entity;
                    minDist = dist;
                    found = true;
                }
            }

            return found;
        }

        public static void Burn(int entity)
        {
            TriggerServerEvent("PocceMod:Burn", API.ObjToNet(entity));
        }
    }
}
