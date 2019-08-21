using CitizenFX.Core.Native;
using MenuAPI;
using PocceMod.Shared;
using System;

namespace PocceMod.Client.Menus.Dev
{
    public class PermissionsMenu : Menu
    {
        public PermissionsMenu() : base("PocceMod", "permissions")
        {
            AddMenuItem(new MenuItem("PlayerID") { Label = API.PlayerId().ToString() });

            if (Permission.GetPlayerGroup(out Permission.Group permissionGroup))
                AddMenuItem(new MenuItem("Permission group") { Label = permissionGroup.ToString() });
            else
                AddMenuItem(new MenuItem("Permission group") { Enabled = false });

            foreach (var ability in (Ability[])Enum.GetValues(typeof(Ability)))
            {
                var group = (Permission.Group)Config.GetConfigInt(ability.ToString());
                AddAbility(ability, group);
            }

            OnMenuClose += (_menu) =>
            {
                ParentMenu.CloseMenu();
            };
        }

        private void AddAbility(Ability ability, Permission.Group group)
        {
            var menuItem = new MenuItem(ability.ToString())
            {
                Label = group.ToString(),
                Enabled = Permission.CanDo(ability)
            };
            AddMenuItem(menuItem);
        }
    }
}
