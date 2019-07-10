using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public static class Hud
    {
        private static Menu _menu;
        private static Dictionary<int, Func<Task>> _menuItemActions;
        private static Dictionary<int, Func<int, Task>> _menuListItemActions;

        static Hud()
        {
            MenuController.MenuToggleKey = Control.SelectCharacterMichael;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;

            _menu = new Menu("PocceMod", "menu");
            MenuController.AddMenu(_menu);
            _menuItemActions = new Dictionary<int, Func<Task>>();
            _menuListItemActions = new Dictionary<int, Func<int, Task>>();

            _menu.OnItemSelect += async (_menu, _item, _index) =>
            {
                if (_menuItemActions.TryGetValue(_index, out Func<Task> action))
                {
                    await action();
                    _menu.CloseMenu();
                }
            };

            _menu.OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                if (_menuListItemActions.TryGetValue(_itemIndex, out Func<int, Task> action))
                {
                    await action(_listIndex);
                    _menu.CloseMenu();
                }
            };
        }

        public static void Notification(string message, bool blink = false, bool saveToBrief = false)
        {
            API.SetNotificationTextEntry("CELL_EMAIL_BCON");
            foreach (string s in CitizenFX.Core.UI.Screen.StringToArray(message))
            {
                API.AddTextComponentSubstringPlayerName(s);
            }
            API.DrawNotification(blink, saveToBrief);
        }


        public static void AddMenuItem(string item, Func<Task> onSelect)
        {
            var menuItem = new MenuItem(item);
            _menu.AddMenuItem(menuItem);
            _menuItemActions.Add(menuItem.Index, onSelect);
        }

        public static void AddMenuListItem(string name, Func<int, Task> onSelect, params string[] items)
        {
            var menuListItem = new MenuListItem(name, new List<string>(items), 0);
            _menu.AddMenuItem(menuListItem);
            _menuListItemActions.Add(menuListItem.Index, onSelect);
        }

        public static void AddSubmenu(string name, Func<string, Task> onSelect, IEnumerable<string> items, int groupByLetters = 0)
        {
            var submenuItem = new MenuItem(name);
            var submenu = new Menu("PocceMod", name);

            _menu.AddMenuItem(submenuItem);
            MenuController.AddSubmenu(_menu, submenu);
            MenuController.BindMenuItem(_menu, submenu, submenuItem);

            submenu.OnItemSelect += async (_menu, _item, _index) =>
            {
                var item = _item.Text;
                await onSelect(item);
                submenu.CloseMenu();
            };

            submenu.OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                var item = _listItem.ListItems[_listIndex];
                await onSelect(item);
                submenu.CloseMenu();
            };

            if (groupByLetters > 0)
            {
                var itemList = new List<string>();
                string lastItemPrefix = string.Empty;
                void addRow()
                {
                    if (itemList.Count > 0)
                    {
                        if (itemList.Count == 1)
                        {
                            var menuItem = new MenuItem(itemList[0]);
                            submenu.AddMenuItem(menuItem);
                        }
                        else
                        {
                            var menuListItem = new MenuListItem(lastItemPrefix + "*", itemList, 0);
                            submenu.AddMenuItem(menuListItem);
                        }
                        itemList = new List<string>();
                    }
                }

                foreach (var item in items)
                {
                    var itemPrefix = (item.Length > groupByLetters) ? item.Substring(0, groupByLetters) : item;
                    if (itemPrefix != lastItemPrefix)
                    {
                        addRow();
                        lastItemPrefix = itemPrefix;
                    }

                    itemList.Add(item);
                }
                addRow();
            }
            else
            {
                foreach (var item in items)
                {
                    submenu.AddMenuItem(new MenuItem(item));
                }
            }
        }

        public static void AddSubmenu(string name, Func<string, Task> onSelect, DataSource<string> dataSource)
        {
            var submenuItem = new MenuItem(name);
            var submenu = new Menu("PocceMod", name);

            _menu.AddMenuItem(submenuItem);
            MenuController.AddSubmenu(_menu, submenu);
            MenuController.BindMenuItem(_menu, submenu, submenuItem);

            submenu.OnItemSelect += async (_menu, _item, _index) =>
            {
                var item = _item.Text;
                await onSelect(item);
                submenu.CloseMenu();
            };

            submenu.OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                var item = _listItem.ListItems[_listIndex];
                await onSelect(item);
                submenu.CloseMenu();
            };

            submenu.OnMenuOpen += (_menu) =>
            {
                var items = dataSource.Pull();
                foreach (var item in items)
                {
                    bool isNewItem = true;
                    foreach (var menuItem in submenu.GetMenuItems())
                    {
                        if (menuItem.Text == item)
                        {
                            isNewItem = false;
                            break;
                        }
                    }

                    if (isNewItem)
                    {
                        submenu.AddMenuItem(new MenuItem(item));
                    }
                }
            };
        }
    }
}
