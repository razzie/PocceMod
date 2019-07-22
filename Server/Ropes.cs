using CitizenFX.Core;
using PocceMod.Shared;
using System;
using System.Collections.Generic;

namespace PocceMod.Server
{
    public class Rope
    {
        public Rope(Player player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, int mode)
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
        public int Mode { get; private set; }
    }

    public class Ropes : BaseScript
    {
        private static readonly Dictionary<Player, List<Rope>> _ropes = new Dictionary<Player, List<Rope>>();

        public Ropes()
        {
            EventHandlers["PocceMod:AddRope"] += new Action<Player, int, int, Vector3, Vector3, int>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<Player>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<Player>(ClearLastRope);
            EventHandlers["PocceMod:RequestRopes"] += new Action<Player>(RequestRopes);
        }

        public static void AddRope(Rope rope)
        {
            if (Permission.CanDo(rope.Player, Ability.Rope) || Permission.CanDo(rope.Player, Ability.RopeGun))
                TriggerClientEvent("PocceMod:AddRope", rope.Player.Handle, rope.Entity1, rope.Entity2, rope.Offset1, rope.Offset2, rope.Mode);

            if (_ropes.TryGetValue(rope.Player, out List<Rope> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(rope.Player, new List<Rope> { rope });
        }
        
        public static void AddRope([FromSource] Player player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, int mode)
        {
            var rope = new Rope(player, entity1, entity2, offset1, offset2, mode);
            AddRope(rope);
        }

        public static void ClearRopes([FromSource] Player player)
        {
            TriggerClientEvent("PocceMod:ClearRopes", player.Handle);

            if (_ropes.TryGetValue(player, out List<Rope> playerRopes))
                playerRopes.Clear();
        }

        public static void ClearLastRope([FromSource] Player player)
        {
            TriggerClientEvent("PocceMod:ClearLastRope", player.Handle);

            if (_ropes.TryGetValue(player, out List<Rope> playerRopes))
            {
                if (playerRopes.Count == 0)
                    return;

                playerRopes.RemoveAt(playerRopes.Count - 1);
            }
        }

        public static void RequestRopes([FromSource] Player player)
        {
            foreach (var playerRopes in _ropes.Values)
            {
                foreach (var rope in playerRopes)
                {
                    rope.Player.TriggerEvent("PocceMod:AddRope", rope.Player.Handle, rope.Entity1, rope.Entity2, rope.Offset1, rope.Offset2, rope.Mode);
                }
            }
        }
    }
}
