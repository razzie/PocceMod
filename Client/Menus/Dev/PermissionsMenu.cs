using MenuAPI;
using PocceMod.Shared;
using System;

namespace PocceMod.Client.Menus.Dev
{
    public class PermissionsMenu : Menu
    {
        public PermissionsMenu() : base("PocceMod", "permissions")
        {
            foreach (var ability in (Ability[])Enum.GetValues(typeof(Ability)))
            {
                var group = (Permission.Group)Config.GetConfigInt(ability.ToString());
                AddAbility(ability, group);
            }
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
