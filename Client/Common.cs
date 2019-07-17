using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
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
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);

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

        public static bool GetWaypoint(out Vector3 wp, bool adjust = true)
        {
            wp = Vector3.Zero;

            if (!API.IsWaypointActive())
                return false;

            wp = API.GetBlipInfoIdCoord(API.GetFirstBlipInfoId(8));

            if (adjust)
            {
                var adjustedWp = Vector3.Zero;
                if (API.GetClosestVehicleNode(wp.X, wp.Y, wp.Z, ref adjustedWp, 1, 100.0f, 2.5f))
                    wp = adjustedWp;
            }

            return true;
        }

        public static List<int> GetObjects()
        {
            var objs = new List<int>();
            int obj = 0;
            int handle = API.FindFirstObject(ref obj);
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);

            if (handle == -1)
                return objs;

            do
            {
                objs.Add(obj);

            } while (API.FindNextObject(handle, ref obj));

            API.EndFindObject(handle);
            return objs;
        }

        public static void Burn(int entity)
        {
            TriggerServerEvent("PocceMod:Burn", API.ObjToNet(entity));
        }
    }
}
