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

        public AntiGravity()
        {
            EventHandlers["PocceMod:AntiGravityAdd"] += new Action<int, float>((entity, force) => _entities.Add(API.NetToEnt(entity), force * 10f));
            EventHandlers["PocceMod:AntiGravityRemove"] += new Action<int>(entity => _entities.Remove(API.NetToEnt(entity)));

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

        private static async Task Update()
        {
            if (_entities.Count == 0)
            {
                await Delay(100);
                return;
            }

            foreach (var pair in _entities.ToArray())
            {
                var entity = pair.Key;
                var force = pair.Value;

                if (!API.DoesEntityExist(entity))
                {
                    _entities.Remove(entity);
                    continue;
                }

                API.ApplyForceToEntityCenterOfMass(entity, 0, 0f, 0f, force, false, false, true, false);
            }
        }
    }
}
