using CitizenFX.Core;
using MenuAPI;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class MainMenu : Menu
    {
        private readonly Dictionary<int, Func<Task>> _menuItemActions = new Dictionary<int, Func<Task>>();
        private readonly Dictionary<int, List<Func<Task>>> _menuListItemActions = new Dictionary<int, List<Func<Task>>>();

        static MainMenu()
        {
            MenuController.MenuToggleKey = Control.SelectCharacterMichael;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.DontOpenAnyMenu = true;

            var menukey = Config.GetConfigInt("MenuKey");
            if (menukey > 0)
            {
                MenuController.MenuToggleKey = (Control)menukey;
            }
        }

        public MainMenu() : base("PocceMod", "menu")
        {
            VehicleMenu = new VehicleMenu();
            PropMenu = new PropMenu();
            SkinMenu = new SkinMenu();

            OnItemSelect += async (_menu, _item, _index) =>
            {
                if (_menuItemActions.TryGetValue(_index, out Func<Task> action))
                {
                    await action();
                    CloseMenu();
                }
            };

            OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                if (_menuListItemActions.TryGetValue(_itemIndex, out List<Func<Task>> actions))
                {
                    await actions[_listIndex]();
                    CloseMenu();
                }
            };

        }

        public VehicleMenu VehicleMenu
        {
            get; private set;
        }

        public PropMenu PropMenu
        {
            get; private set;
        }

        public SkinMenu SkinMenu
        {
            get; private set;
        }

        public static bool IsOpen
        {
            get { return MenuController.IsAnyMenuOpen(); }
        }

        public void AddMenuItemAsync(string item, Func<Task> onSelect)
        {
            MenuController.DontOpenAnyMenu = false;
            var menuItem = new MenuItem(item);
            AddMenuItem(menuItem);
            _menuItemActions.Add(menuItem.Index, onSelect);
        }

        public void AddMenuItem(string item, Action onSelect)
        {
            AddMenuItemAsync(item, () => { onSelect(); return BaseScript.Delay(0); });
        }

        public void AddMenuListItemAsync(string item, string subitem, Func<Task> onSelect)
        {
            MenuController.DontOpenAnyMenu = false;

            foreach (var menuItem in GetMenuItems())
            {
                if (menuItem is MenuListItem && menuItem.Text == item)
                {
                    var subitems = ((MenuListItem)menuItem).ListItems;
                    subitems.Add(subitem);
                    _menuListItemActions[menuItem.Index].Add(onSelect);
                    return;
                }
            }

            var menuListItem = new MenuListItem(item, new List<string> { subitem }, 0);
            AddMenuItem(menuListItem);
            _menuListItemActions.Add(menuListItem.Index, new List<Func<Task>> { onSelect });
        }

        public void AddMenuListItem(string item, string subitem, Action onSelect)
        {
            AddMenuListItemAsync(item, subitem, () => { onSelect(); return BaseScript.Delay(0); });
        }

    }
}
