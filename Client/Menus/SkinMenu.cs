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
        private readonly SkinSet _allSkins = new SkinSet();
        private readonly SkinSet _lastSkins = new SkinSet();
        private SkinSet _source = null;

        public SkinMenu() : base("PocceMod", "select skin")
        {
            foreach (var pocce in Config.PocceList)
            {
                _allSkins.Add(Skin.ModelToName(pocce));
            }

            OnItemSelect += async (_menu, _item, _index) =>
            {
                await ChangeSkin(_item.Text);
            };

            OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                await ChangeSkin(_listItem, _listIndex);
            };

            OnMenuOpen += (_menu) =>
            {
                if (_source == null)
                    return;

                foreach (var items in _source.Elements)
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
                var skin = Skin.FromPed(ped);
                _lastSkins.Add(skin);
                _allSkins.Add(skin);
                Common.Notification("model: " + skin.Name);
            }

            if (_lastSkins.Count > 0)
                ShowLastSkins();
        }

        public void DetectPlayerSkin()
        {
            _lastSkins.Clear();

            var skin = Skin.FromPed(API.GetPlayerPed(-1));
            _lastSkins.Add(skin);
            _allSkins.Add(skin);
            Common.Notification("model: " + skin.Name);

            if (_lastSkins.Count > 0)
                ShowLastSkins();
        }

        private static async Task ChangeSkin(MenuListItem item, int index)
        {
            var skins = item.ItemData as List<Skin>;
            if (skins != null && skins.Count > index)
            {
                var skin = skins[index];
                await ChangeSkin(skin.Model);
                skin.Restore(API.GetPlayerPed(-1));
            }
        }

        private static Task ChangeSkin(string model)
        {
            uint hash;
            if (model.StartsWith("0x"))
                hash = uint.Parse(model.Substring(2), System.Globalization.NumberStyles.HexNumber);
            else
                hash = (uint)API.GetHashKey(model);

            return ChangeSkin(hash);
        }

        private static async Task ChangeSkin(uint model)
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyVehicle(player, false))
            {
                Common.Notification("Skin change is not allowed in vehicles");
                return;
            }

            var loadout = Weapons.Get(player);

            await Common.RequestModel(model);
            API.SetPlayerModel(API.PlayerId(), model);
            player = API.GetPlayerPed(-1); // new ped was created for the player
            
            API.ClearAllPedProps(player);
            API.ClearPedDecorations(player);
            API.ClearPedFacialDecorations(player);
            API.SetPedRandomComponentVariation(player, false);

            Weapons.Give(player, loadout);
        }
    }
}
