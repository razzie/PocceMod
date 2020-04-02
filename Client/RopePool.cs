using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Collections.Generic;

namespace PocceMod.Client
{
    public static class RopePool
    {
        private static readonly int PoolSize = Config.GetConfigInt("RopePoolSize");
        private static readonly Queue<int> _pool = new Queue<int>();

        static RopePool()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                int unkPtr = 0;
                int handle = API.AddRope(0f, 0f, 0f, 0f, 0f, 0f, Ropes.MaxLength, 1, Ropes.MaxLength, 0.25f, 0f, false, false, false, 5f, false, ref unkPtr);
                _pool.Enqueue(handle);
            }
        }

        public static int AddRope()
        {
            return _pool.Dequeue();
        }

        public static void DeleteRope(ref int rope)
        {
            API.StopRopeUnwindingFront(rope);
            API.StopRopeWinding(rope);
            API.RopeConvertToSimple(rope);
            _pool.Enqueue(rope);
            rope = -1;
        }
    }
}
