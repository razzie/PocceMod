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

        public delegate void GrantedDelegate(Player player, Group group);
        public static event GrantedDelegate Granted;

        private static readonly Dictionary<Ability, Group> _permissions = new Dictionary<Ability, Group>();
        private static readonly Dictionary<Player, Group> _playerGroups = new Dictionary<Player, Group>();

        static Permission()
        {
            IgnorePermissions = Config.GetConfigBool("IgnorePermissions");
        }

        public Permission()
        {
            foreach (var ability in (Ability[])Enum.GetValues(typeof(Ability)))
            {
                var group = (Group)Config.GetConfigInt(ability.ToString());
                _permissions.Add(ability, group);
            }

#if CLIENT
            EventHandlers["PocceMod:Permission"] += new Action<int>(group => AddPlayer(Game.Player, (Group)group));
            TriggerServerEvent("PocceMod:RequestPermission");
#endif

#if SERVER
            API.ExecuteCommand("add_ace group.moderator \"PocceMod.Moderator\" allow");
            API.ExecuteCommand("add_ace group.admin \"PocceMod.Admin\" allow");
            EventHandlers["PocceMod:RequestPermission"] += new Action<Player>(RequestPermission);
#endif
        }

        public static bool IgnorePermissions { get; }

        private static void AddPlayer(Player player, Group playerGroup)
        {
            _playerGroups[player] = playerGroup;
            Granted?.Invoke(player, playerGroup);
        }

        public static bool CanDo(Player player, Ability ability)
        {
            if (IgnorePermissions)
                return true;

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

        private static void RequestPermission([FromSource] Player source)
        {
            var group = GetPlayerGroup(source);
            AddPlayer(source, group);
            TriggerClientEvent(source, "PocceMod:Permission", (int)group);
        }
#endif
    }
}
