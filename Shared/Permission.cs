using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;

namespace PocceMod.Shared
{
    public class Permission : BaseScript
    {
        public enum Group
        {
            User = 0,
            Moderator = 1,
            Admin = 2,
            Noone = 3
        }

        public delegate void GrantedEvent(Player player, Group group);
        public static event GrantedEvent Granted;

        private static readonly Dictionary<Ability, Group> _permissions = new Dictionary<Ability, Group>();
        private static readonly Dictionary<Player, Group> _playerGroups = new Dictionary<Player, Group>();

        public Permission()
        {
            foreach (var ability in (Ability[])Enum.GetValues(typeof(Ability)))
            {
                var group = (Group)Config.GetConfigInt(ability.ToString());
                _permissions.Add(ability, group);
            }

#if CLIENT
            EventHandlers["PocceMod:Permission"] += new Action<Group>(group => AddPlayer(Game.Player, group));
#endif

#if SERVER
            API.ExecuteCommand("add_ace group.moderator \"PocceMod.Moderator\" allow");
            API.ExecuteCommand("add_ace group.admin \"PocceMod.Admin\" allow");
#endif
        }

        public static void AddPlayer(Player player, Group playerGroup)
        {
            _playerGroups.Add(player, playerGroup);
            Granted?.Invoke(player, playerGroup);
        }

        public static bool CanDo(Player player, Ability ability)
        {
            if (!_playerGroups.TryGetValue(player, out Group playerGroup))
                return false;

            if (_permissions.TryGetValue(ability, out Group group))
                return (int)playerGroup >= (int)group;
            else
                return false;
        }

#if CLIENT
        public static bool CanDo(Ability ability)
        {
            return CanDo(Game.Player, ability);
        }
#endif

#if SERVER
        private static Group GetPlayerGroup(Player player)
        {
            if (API.IsPlayerAceAllowed(player.Handle, "PocceMod.Admin"))
                return Group.Admin;
            else if (API.IsPlayerAceAllowed(player.Handle, "PocceMod.Moderator"))
                return Group.Moderator;
            else
                return Group.User;
        }

        public static void AddPlayer(Player player)
        {
            var group = GetPlayerGroup(player);
            AddPlayer(player, group);
            TriggerClientEvent(player, "PocceMod:Permission", group);
        }
#endif
    }
}
