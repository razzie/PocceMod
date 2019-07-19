using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public static class Hud
    {
        private static Menu _menu;
        private static Dictionary<int, Func<Task>> _menuItemActions;
        private static Dictionary<int, List<Func<Task>>> _menuListItemActions;

        static Hud()
        {
            MenuController.MenuToggleKey = Control.SelectCharacterMichael;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.DontOpenAnyMenu = true;

            _menu = new Menu("PocceMod", "menu");
            MenuController.AddMenu(_menu);
            _menuItemActions = new Dictionary<int, Func<Task>>();
            _menuListItemActions = new Dictionary<int, List<Func<Task>>>();

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
                if (_menuListItemActions.TryGetValue(_itemIndex, out List<Func<Task>> actions))
                {
                    await actions[_listIndex]();
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

        public static void SetMenuKey(int key)
        {
            MenuController.MenuToggleKey = (Control)key;
        }

        public static void AddMenuItem(string item, Func<Task> onSelect)
        {
            MenuController.DontOpenAnyMenu = false;
            var menuItem = new MenuItem(item);
            _menu.AddMenuItem(menuItem);
            _menuItemActions.Add(menuItem.Index, onSelect);
        }

        public static void AddMenuItem(string item, Action onSelect)
        {
            AddMenuItem(item, () => { onSelect(); return BaseScript.Delay(0); });
        }

        public static void AddMenuListItem(string item, string subitem, Func<Task> onSelect)
        {
            MenuController.DontOpenAnyMenu = false;

            foreach (var menuItem in _menu.GetMenuItems())
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
            _menu.AddMenuItem(menuListItem);
            _menuListItemActions.Add(menuListItem.Index, new List<Func<Task>> { onSelect });
        }

        public static void AddMenuListItem(string item, string subitem, Action onSelect)
        {
            AddMenuListItem(item, subitem, () => { onSelect(); return BaseScript.Delay(0); });
        }

        public static void AddSubmenu(string name, Func<string, Task> onSelect, IEnumerable<string> items, int groupByLetters = 0)
        {
            var submenuItem = new MenuItem(name) { Label = "→→→" };
            var submenu = new Menu("PocceMod", name);

            _menu.AddMenuItem(submenuItem);
            MenuController.AddSubmenu(_menu, submenu);
            MenuController.BindMenuItem(_menu, submenu, submenuItem);
            MenuController.DontOpenAnyMenu = false;

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

            submenu.OnMenuClose += (_menu) =>
            {
                submenu.ResetFilter();
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
            var submenuItem = new MenuItem(name) { Label = "→→→" };
            var submenu = new Menu("PocceMod", name);

            _menu.AddMenuItem(submenuItem);
            MenuController.AddSubmenu(_menu, submenu);
            MenuController.BindMenuItem(_menu, submenu, submenuItem);
            MenuController.DontOpenAnyMenu = false;

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

            submenu.OnMenuClose += (_menu) =>
            {
                submenu.ResetFilter();
            };
        }

        public static void FilterSubmenu(string submenu, string item)
        {
            bool filter(MenuItem menuItem)
            {
                if (menuItem is MenuListItem)
                {
                    var menuListItem = (MenuListItem)menuItem;
                    var subitems = menuListItem.ListItems;
                    for (int i = 0; i < subitems.Count; ++i)
                    {
                        if (subitems[i].Contains(item))
                            return true;
                    }
                }
                else
                {
                    if (menuItem.Text.Contains(item))
                        return true;
                }

                return false;
            }

            foreach (var menu in MenuController.Menus)
            {
                if (menu.MenuSubtitle == submenu)
                {
                    menu.FilterMenuItems(filter);
                    menu.OpenMenu();
                    return;
                }
            }
        }

        public static bool JumpToSubmenuItem(string submenu, string item)
        {
            foreach (var menu in MenuController.Menus)
            {
                if (menu.MenuSubtitle == submenu)
                {
                    foreach (var menuItem in menu.GetMenuItems())
                    {
                        if (menuItem is MenuListItem)
                        {
                            var menuListItem = (MenuListItem)menuItem;
                            var subitems = menuListItem.ListItems;
                            for (int i = 0; i < subitems.Count; ++i)
                            {
                                if (subitems[i] == item)
                                {
                                    menuListItem.ListIndex = i;
                                    var index = menuItem.Index;
                                    menu.RefreshIndex(index, index > menu.MaxItemsOnScreen ? index - menu.MaxItemsOnScreen - 1 : 0);
                                    menu.OpenMenu();
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            if (menuItem.Text == item)
                            {
                                var index = menuItem.Index;
                                menu.RefreshIndex(index, index > menu.MaxItemsOnScreen ? index - menu.MaxItemsOnScreen -1 : 0);
                                menu.OpenMenu();
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
