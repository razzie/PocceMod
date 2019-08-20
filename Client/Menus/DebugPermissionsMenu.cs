using MenuAPI;
using PocceMod.Shared;
using System;

namespace PocceMod.Client.Menus
{
    public class DebugPermissionsMenu : Menu
    {
        public DebugPermissionsMenu() : base("PocceMod", "permissions")
        {
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
