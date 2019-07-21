using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class SkinMenu : Menu
    {
        private bool _firstUse = true;
        private readonly SkinSet _allSkins = new SkinSet();
        private readonly SkinSet _lastSkins = new SkinSet();

        public enum SkinSelection
        {
            AllSkins,
            LastSkins
        }

        public SkinMenu() : base("PocceMod", "select skin")
        {
            OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                await ChangeSkin(_listItem, _listIndex);
                //CloseMenu();
            };

            OnMenuOpen += (_menu) =>
            {
                if (_firstUse)
                {
                    Common.Notification("Weapon loadout is lost when chenging skin!");
                    _firstUse = false;
                }

                var skinset = Selection == SkinSelection.AllSkins ? _allSkins : _lastSkins;

                foreach (var items in skinset.Elements)
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
            };
        }

        public SkinSelection Selection { get; set; } = SkinSelection.AllSkins;

        public void ShowAllSkins()
        {
            Selection = SkinSelection.AllSkins;
            OpenMenu();
        }

        public void ShowLastSkins()
        {
            Selection = SkinSelection.LastSkins;
            OpenMenu();
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
