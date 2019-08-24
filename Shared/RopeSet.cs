using CitizenFX.Core;
using System.Collections.Generic;
using System.Linq;

namespace PocceMod.Shared
{
    public class RopeSet
    {
        private readonly Dictionary<Player, List<Rope>> _ropes = new Dictionary<Player, List<Rope>>();

        public Rope[] Ropes
        {
            get { return _ropes.Values.SelectMany(ropes => ropes).ToArray(); }
        }

        public void AddRope(Rope rope)
        {
            if (_ropes.TryGetValue(rope.Player, out List<Rope> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(rope.Player, new List<Rope> { rope });
        }

        public bool HasRopesAttached(int entity)
        {
            foreach (var playerRopes in _ropes.Values)
            {
                foreach (var rope in playerRopes)
                {
                    if (rope.Entity1 == entity || rope.Entity2 == entity)
                        return true;
                }
            }

            return false;
        }

        public void ClearRopes(Player player)
        {
            if (_ropes.TryGetValue(player, out List<Rope> playerRopes))
            {
                foreach (var rope in playerRopes)
                {
                    rope.Clear();
                }

                playerRopes.Clear();
            }
        }

        public void ClearLastRope(Player player)
        {
            if (_ropes.TryGetValue(player, out List<Rope> playerRopes))
            {
                if (playerRopes.Count == 0)
                    return;

                var rope = playerRopes[playerRopes.Count - 1];
                rope.Clear();

                playerRopes.RemoveAt(playerRopes.Count - 1);
            }
        }

        public void ClearEntityRopes(int entity)
        {
            foreach (var playerRopes in _ropes.Values)
            {
                foreach (var rope in playerRopes.ToArray())
                {
                    if (rope.Entity1 == entity || rope.Entity2 == entity)
                    {
                        rope.Clear();
                        playerRopes.Remove(rope);
                    }
                }
            }
        }
    }
}
