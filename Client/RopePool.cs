using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Linq;

namespace PocceMod.Client
{
    public static class RopePool
    {
        private static readonly int PoolSize = Config.GetConfigInt("RopePoolSize");
        private static readonly HashSet<int> _pool = new HashSet<int>();

        static RopePool()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                int unkPtr = 0;
                int handle = API.AddRope(0f, 0f, 0f, 0f, 0f, 0f, Ropes.MaxLength, 1, Ropes.MaxLength, 0.25f, 0f, false, false, false, 5f, true, ref unkPtr);
                _pool.Add(handle);
            }
        }

        public static int AddRope()
        {
            var rope = _pool.First();
            _pool.Remove(rope);
            return rope;
        }

        public static void DeleteRope(ref int rope)
        {
            if (API.DoesRopeExist(ref rope))
                _pool.Add(rope);

            rope = -1;
        }
    }
}
