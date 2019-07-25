using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class AntiGravity : BaseScript
    {
        private static readonly Dictionary<int, float> _entities = new Dictionary<int, float>();

        public AntiGravity()
        {
            Tick += Update;
        }

        public static void Add(int entity, float force)
        {
            _entities.Add(entity, force);
        }

        public static bool Contains(int entity)
        {
            return _entities.ContainsKey(entity);
        }

        public static void Remove(int entity)
        {
            _entities.Remove(entity);
        }

        private static Task Update()
        {
            foreach (var pair in _entities.ToArray())
            {
                var entity = pair.Key;
                var force = pair.Value;

                if (!API.DoesEntityExist(entity))
                {
                    _entities.Remove(entity);
                    continue;
                }

                API.ApplyForceToEntityCenterOfMass(entity, 0, 0f, 0f, force, false, true, true, false);
            }

            return Delay(0);
        }
    }
}
