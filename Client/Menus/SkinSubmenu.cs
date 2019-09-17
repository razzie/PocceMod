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
        private string _lastSkinName;
        private Skin _lastSkin;

        public SkinSubmenu(Func<uint, Skin, Task> onSelect) : base("PocceMod", "select skin")
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

                _lastSkinName = model;
                _lastSkin = null;
            };

            OnListItemSelect += async (menu, listItem, listIndex, itemIndex) =>
            {
                var skins = listItem.ItemData as List<Skin>;
                if (skins != null && skins.Count > listIndex)
                {
                    var skin = skins[listIndex];
                    await onSelect(skin.Model, skin);

                    _lastSkinName = skin.Name;
                    _lastSkin = skin;
                }
            };

            OnMenuOpen += (menu) =>
            {
                if (_source == null)
                    return;

                foreach (var item in _source.Skins)
                    AddSkinVariations(item);

                JumpToSkin(_lastSkinName, _lastSkin);
            };

            OnMenuClose += (menu) =>
            {
                ClearMenuItems();
                _source = null;
            };
        }

        private void AddSkinVariations(SkinVariations var)
        {
            if (var.Skins.Count > 0)
            {
                var list = new List<string>();
                for (int i = 0; i < var.Skins.Count; ++i)
                {
                    list.Add("#" + i);
                }

                AddMenuItem(new MenuListItem(var.Model, list, 0) { ItemData = var.Skins });
            }
            else
            {
                AddMenuItem(new MenuItem(var.Model));
            }
        }

        private void JumpToSkin(string model, Skin skin)
        {
            foreach (var menuItem in GetMenuItems())
            {
                if (menuItem.Text == model)
                {
                    var index = menuItem.Index;
                    RefreshIndex(index, index > MaxItemsOnScreen ? index - MaxItemsOnScreen + 1 : 0);

                    if (skin != null && menuItem is MenuListItem)
                    {
                        List<Skin> skins = menuItem.ItemData;
                        var menuListItem = (MenuListItem)menuItem;
                        menuListItem.ListIndex = skins.IndexOf(_lastSkin);
                    }

                    return;
                }
            }
        }

        protected void OpenMenu(SkinSet source)
        {
            _source = source;
            OpenMenu();
        }
    }
}
