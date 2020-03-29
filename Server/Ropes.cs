using CitizenFX.Core;
using PocceMod.Shared;
using System;
using System.Linq;

namespace PocceMod.Server.Server
{
    public class Ropes : BaseScript
    {
        private static readonly RopeSet _ropes = new RopeSet();

        private class Rope : IRope
        {
            public Rope(string player, int id, int entity1, int entity2, Vector3 offset1, Vector3 offset2, float length)
            {
                Player = player;
                ID = id;
                Entity1 = entity1;
                Entity2 = entity2;
                Offset1 = offset1;
                Offset2 = offset2;
                Length = length;
            }

            public string Player { get; }
            public int ID { get; }
            public int Entity1 { get; }
            public int Entity2 { get; }
            public Vector3 Offset1 { get; }
            public Vector3 Offset2 { get; }
            public float Length { get; set; }

            public void Clear()
            {
            }

            public void Update()
            {
                throw new NotImplementedException();
            }
        }

        public Ropes()
        {
            EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDropped);
            EventHandlers["PocceMod:AddRope"] += new Action<Player, int, int, int, Vector3, Vector3, float>(AddRope);
            EventHandlers["PocceMod:SetRopeLength"] += new Action<Player, int, float>(SetRopeLength);
            EventHandlers["PocceMod:RemoveRope"] += new Action<Player, int>(RemoveRope);
            EventHandlers["PocceMod:RemoveEntityRopes"] += new Action<int>(RemoveEntityRopes);
            EventHandlers["PocceMod:RequestRopes"] += new Action<Player>(RequestRopes);
        }

        private static void PlayerDropped([FromSource] Player source, string reason)
        {
            foreach (var rope in _ropes.GetPlayerRopes(source.Handle).ToArray())
            {
                _ropes.RemoveRope(rope.Player, rope.ID);
                TriggerClientEvent("PocceMod:RemoveRope", rope.Player, rope.ID);
            }
        }

        private static void AddRope([FromSource] Player source, int id, int entity1, int entity2, Vector3 offset1, Vector3 offset2, float length)
        {
            if (Permission.CanDo(source, Ability.Rope) || Permission.CanDo(source, Ability.RopeGun) || Permission.CanDo(source, Ability.Balloons))
            {
                _ropes.AddRope(new Rope(source.Handle, id, entity1, entity2, offset1, offset2, length));
                TriggerClientEvent("PocceMod:AddRope", source.Handle, id, entity1, entity2, offset1, offset2, length);
            }
        }

        private static void SetRopeLength([FromSource] Player source, int id, float length)
        {
            var rope = _ropes.GetRope(source.Handle, id);
            if (rope != null)
            {
                rope.Length = length;
                TriggerClientEvent("PocceMod:SetRopeLength", source.Handle, id, length);
            }
        }

        private static void RemoveRope([FromSource] Player source, int id)
        {
            _ropes.RemoveRope(source.Handle, id);
            TriggerClientEvent("PocceMod:RemoveRope", source.Handle, id);
        }

        private static void RequestRopes([FromSource] Player source)
        {
            foreach (var rope in _ropes.Ropes)
            {
                source.TriggerEvent("PocceMod:AddRope", rope.Player, rope.ID,
                    rope.Entity1, rope.Entity2, rope.Offset1, rope.Offset2, rope.Length);
            }
        }

        private static void RemoveEntityRopes(int entity)
        {
            foreach (var rope in _ropes.GetEntityRopes(entity).ToArray())
            {
                _ropes.RemoveRope(rope.Player, rope.ID);
                TriggerClientEvent("PocceMod:RemoveRope", rope.Player, rope.ID);
            }
        }
    }
}
