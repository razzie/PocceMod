using CitizenFX.Core.Native;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class SkinSubmenu : Menu
    {
        private SkinSet _source = null;

        public SkinSubmenu(Func<uint, Skin, Task> onSelect, bool closeOnSelect) : base("PocceMod", "select skin")
        {
            OnItemSelect += async (menu, item, index) =>
            {
                var model = item.Text;
                uint hash;
                if (model.StartsWith("0x"))
                    hash = uint.Parse(model.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    hash = (uint)API.GetHashKey(model);

                await onSelect(hash, null);

                if (closeOnSelect)
                    CloseMenu();
            };

            OnListItemSelect += async (menu, listItem, listIndex, itemIndex) =>
            {
                var skins = listItem.ItemData as List<Skin>;
                if (skins != null && skins.Count > listIndex)
                {
                    var skin = skins[listIndex];
                    await onSelect(skin.Model, skin);
                }

                if (closeOnSelect)
                    CloseMenu();
            };

            OnMenuOpen += (menu) =>
            {
                if (_source == null)
                    return;

                foreach (var items in _source.Skins)
                {
                    if (items.Value.Count > 0)
                    {
                        var list = new List<string>();
                        for (int i = 0; i < items.Value.Count; ++i)
                        {
                            list.Add("#" + i);
                        }

                        var menuItem = new MenuListItem(items.Key, list, 0);
                        menuItem.ItemData = items.Value;
                        AddMenuItem(menuItem);
                    }
                    else
                    {
                        var menuItem = new MenuItem(items.Key);
                        AddMenuItem(menuItem);
                    }
                }
            };

            OnMenuClose += (menu) =>
            {
                ClearMenuItems();
                _source = null;
            };
        }

        protected void OpenMenu(SkinSet source)
        {
            _source = source;
            OpenMenu();
        }
    }
}
