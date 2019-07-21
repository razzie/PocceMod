using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class SkinMenu : Menu
    {
        public SkinMenu() : base("PocceMod", "select skin")
        {
            DataSource = new DataSource<string>();

            OnItemSelect += async (_menu, _item, _index) =>
            {
                var item = _item.Text;
                await ChangeSkin(item);
                CloseMenu();
            };

            OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                var item = _listItem.ListItems[_listIndex];
                await ChangeSkin(item);
                CloseMenu();
            };

            OnMenuOpen += (_menu) =>
            {
                var items = DataSource.Pull();
                foreach (var item in items)
                {
                    bool isNewItem = true;
                    foreach (var menuItem in GetMenuItems())
                    {
                        if (menuItem.Text == item)
                        {
                            isNewItem = false;
                            break;
                        }
                    }

                    if (isNewItem)
                    {
                        AddMenuItem(new MenuItem(item));
                    }
                }
            };
        }

        public DataSource<string> DataSource
        {
            get; private set;
        }

        public static List<string> DetectSkins()
        {
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);
            var peds = Peds.Get();
            var models = new List<string>();

            foreach (var ped in peds)
            {
                var pos = API.GetEntityCoords(ped, true);
                if (coords.DistanceToSquared(pos) < 4f)
                {
                    var model = string.Format("0x{0:X8}", API.GetEntityModel(ped));
                    models.Add(model);
                    Common.Notification("ped:" + model);
                }
            }

            return models;
        }

        public static async Task ChangeSkin(string hexModel)
        {
            var model = uint.Parse(hexModel.Substring(2), System.Globalization.NumberStyles.HexNumber);
            await Game.Player.ChangeModel(new Model((PedHash)model));
        }
    }
}
