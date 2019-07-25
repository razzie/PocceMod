using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PocceMod.Shared
{
    public class Rope
    {
        [Flags]
        public enum ModeFlag
        {
            Normal = 0,
            Tow = 1,
            Ropegun = 2,
            Grapple = 4
        }

        public Rope(Player player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode)
        {
            Player = player;
            Entity1 = entity1;
            Entity2 = entity2;
            Offset1 = offset1;
            Offset2 = offset2;
            Mode = mode;
        }

        public Player Player { get; private set; }
        public int Entity1 { get; private set; }
        public int Entity2 { get; private set; }
        public Vector3 Offset1 { get; private set; }
        public Vector3 Offset2 { get; private set; }
        public ModeFlag Mode { get; private set; }
        
        public virtual void Clear()
        {
        }
    }

    public class RopeSet
    {
        private readonly Dictionary<Player, List<Rope>> _ropes = new Dictionary<Player, List<Rope>>();

        public Rope[] GetRopes() // too bad iterator blocks cannot be used in async methods
        {
            return _ropes.Values.SelectMany(ropes => ropes).ToArray();
        }

        public void AddRope(Rope rope)
        {
            if (_ropes.TryGetValue(rope.Player, out List<Rope> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(rope.Player, new List<Rope> { rope });
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
