using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class SkinMenu : Menu
    {
        private bool _firstUse = true;
        private readonly SkinSet _allSkins = new SkinSet();
        private readonly SkinSet _lastSkins = new SkinSet();
        private SkinSet _source = null;

        public SkinMenu() : base("PocceMod", "select skin")
        {
            OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                await ChangeSkin(_listItem, _listIndex);
                //CloseMenu();
            };

            OnMenuOpen += (_menu) =>
            {
                if (_source == null)
                    return;

                if (_firstUse)
                {
                    Common.Notification("Weapon loadout is lost when chenging skin!");
                    _firstUse = false;
                }

                foreach (var items in _source.Elements)
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
            };

            OnMenuClose += (_menu) =>
            {
                ClearMenuItems();
                _source = null;
            };
        }

        public void OpenMenu(SkinSet source)
        {
            if (Permission.CanDo(Ability.ChangeSkin))
            {
                _source = source;
                OpenMenu();
            }
        }

        public void ShowAllSkins()
        {
            OpenMenu(_allSkins);
        }

        public void ShowLastSkins()
        {
            OpenMenu(_lastSkins);
        }

        public void DetectSkins()
        {
            _lastSkins.Clear();
            var peds = Peds.Get(Peds.DefaultFilters, 4f);

            foreach (var ped in peds)
            {
                var skin = new Skin(ped);
                _lastSkins.Add(skin);
                _allSkins.Add(skin);
                Common.Notification("ped: " + skin.Name);
            }

            if (_lastSkins.Count > 0)
                ShowLastSkins();
        }

        private async Task ChangeSkin(MenuListItem item, int index)
        {
            var skins = item.ItemData as List<Skin>;
            if (skins != null && skins.Count > index)
            {
                var skin = skins[index];
                await Game.Player.ChangeModel(new Model((PedHash)skin.Model));
                await skin.Restore(API.GetPlayerPed(-1));
            }
        }
    }
}
