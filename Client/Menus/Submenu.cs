using MenuAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class Submenu : Menu
    {
        public Submenu(string title, Func<string, Task> onSelect, IEnumerable<string> items, int groupByLetters = 0) : base("PocceMod", title)
        {
            OnItemSelect += async (_menu, _item, _index) =>
            {
                var item = _item.Text;
                await onSelect(item);
                CloseMenu();
            };

            OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                var item = _listItem.ListItems[_listIndex];
                await onSelect(item);
                CloseMenu();
            };

            OnMenuClose += (_menu) =>
            {
                var item = GetCurrentMenuItem();
                ResetFilter();
                var index = item.Index;
                RefreshIndex(index, index > MaxItemsOnScreen ? index - MaxItemsOnScreen + 1 : 0);
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
                            AddMenuItem(menuItem);
                        }
                        else
                        {
                            var menuListItem = new MenuListItem(lastItemPrefix + "*", itemList, 0);
                            AddMenuItem(menuListItem);
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
                    AddMenuItem(new MenuItem(item));
                }
            }
        }

        public void Filter(string item)
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
                        {
                            menuListItem.ListIndex = i;
                            return true;
                        }
                    }
                }
                else
                {
                    if (menuItem.Text.Contains(item))
                        return true;
                }

                return false;
            }

            FilterMenuItems(filter);
            OpenMenu();
        }

        public bool JumpToItem(string submenu, string item)
        {
            foreach (var menuItem in GetMenuItems())
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
                            RefreshIndex(index, index > MaxItemsOnScreen ? index - MaxItemsOnScreen + 1 : 0);
                            OpenMenu();
                            return true;
                        }
                    }
                }
                else
                {
                    if (menuItem.Text == item)
                    {
                        var index = menuItem.Index;
                        RefreshIndex(index, index > MaxItemsOnScreen ? index - MaxItemsOnScreen + 1 : 0);
                        OpenMenu();
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
