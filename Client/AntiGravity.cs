using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class AntiGravity : BaseScript
    {
        private static readonly Dictionary<int, float> _entities = new Dictionary<int, float>();
        private static DateTime _lastTick = DateTime.Now;

        public AntiGravity()
        {
            EventHandlers["PocceMod:AntiGravityAdd"] += new Action<int, float>(async (entity, force) => _entities.Add(await Common.WaitForNetEntity(entity), force * 2));
            EventHandlers["PocceMod:AntiGravityRemove"] += new Action<int>(async entity => _entities.Remove(await Common.WaitForNetEntity(entity)));

            Tick += Update;
        }

        public static void Add(int entity, float force)
        {
            TriggerServerEvent("PocceMod:AntiGravityAdd", API.ObjToNet(entity), force);
        }

        public static bool Contains(int entity)
        {
            return _entities.ContainsKey(entity);
        }

        public static void Remove(int entity)
        {
            TriggerServerEvent("PocceMod:AntiGravityRemove", API.ObjToNet(entity));
        }

        private static Task Update()
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastTick).TotalSeconds;

            foreach (var pair in _entities.ToArray())
            {
                var entity = pair.Key;
                var force = pair.Value;

                if (!API.DoesEntityExist(entity))
                {
                    _entities.Remove(entity);
                    continue;
                }

                API.ApplyForceToEntityCenterOfMass(entity, 1, 0f, 0f, force * (float)Math.Sqrt(elapsed), false, false, true, false);
            }

            _lastTick = now;

            return Delay(10);
        }
    }
}
