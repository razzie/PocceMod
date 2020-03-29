using System.Collections.Generic;
using System.Linq;

namespace PocceMod.Shared
{
    public class RopeSet
    {
        private readonly Dictionary<string, Dictionary<int, IRope>> _ropes = new Dictionary<string, Dictionary<int, IRope>>();

        public IEnumerable<IRope> Ropes
        {
            get { return _ropes.Values.SelectMany(ropes => ropes.Values); }
        }

        public IEnumerable<IRope> GetPlayerRopes(string player)
        {
            if (_ropes.TryGetValue(player, out Dictionary<int, IRope> playerRopes))
                return playerRopes.Values;
            else
                return Enumerable.Empty<IRope>();
        }

        public IEnumerable<IRope> GetEntityRopes(int entity)
        {
            return Ropes.Where(rope => rope.Entity1 == entity || rope.Entity2 == entity);
        }

        public bool IsAnyRopeAttachedToEntity(int entity)
        {
            return Ropes.Any(rope => rope.Entity1 == entity || rope.Entity2 == entity);
        }

        public void AddRope(IRope rope)
        {
            if (_ropes.TryGetValue(rope.Player, out Dictionary<int, IRope> playerRopes))
                playerRopes.Add(rope.ID, rope);
            else
                _ropes.Add(rope.Player, new Dictionary<int, IRope> { { rope.ID, rope } });
        }

        public IRope GetRope(string player, int id)
        {
            if (_ropes.TryGetValue(player, out Dictionary<int, IRope> playerRopes) && playerRopes.TryGetValue(id, out IRope rope))
                return rope;
            else
                return null;
        }

        public void RemoveRope(string player, int id)
        {
            if (_ropes.TryGetValue(player, out Dictionary<int, IRope> playerRopes) && playerRopes.TryGetValue(id, out IRope rope))
            {
                rope.Clear();
                playerRopes.Remove(id);
            }
        }
    }
}
