using CitizenFX.Core.Native;
using MenuAPI;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public class CompanionMenu : Menu
    {
        private SkinSet _source = null;

        public CompanionMenu() : base("PocceMod", "select skin")
        {
            OnItemSelect += async (_menu, _item, _index) =>
            {
                await OnSelect(_item.Text);
            };

            OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                await OnSelect(_listItem, _listIndex);
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

        private static async Task<int> SpawnHuman(uint model)
        {
            if (!API.IsModelAPed(model))
            {
                Common.Notification(Skin.ModelToName(model) + " is not a ped model");
                return -1;
            }

            int ped;
            int player = API.GetPlayerPed(-1);
            var coords = API.GetEntityCoords(player, true);

            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (Vehicles.GetFreeSeat(vehicle, out int seat))
                {
                    await Common.RequestModel(model);
                    ped = API.CreatePedInsideVehicle(vehicle, 26, model, seat, true, false);
                    API.SetModelAsNoLongerNeeded(model);
                }
                else if (API.GetEntitySpeed(vehicle) > 0.1f)
                {
                    Common.Notification("Player is in a moving vehicle and there are no free seats");
                    return -1;
                }
                else
                {
                    ped = await Peds.Spawn(model, coords, true);
                }
            }
            else
            {
                ped = await Peds.Spawn(model, coords, true);
            }

            Companions.Add(ped);
            await Peds.Arm(ped, Config.WeaponList);
            return ped;
        }

        private static async Task<int> SpawnNonhuman(uint model)
        {
            if (!API.IsModelAPed(model))
            {
                Common.Notification(Skin.ModelToName(model) + " is not a ped model");
                return -1;
            }

            int player = API.GetPlayerPed(-1);
            var coords = API.GetEntityCoords(player, true);

            if (API.IsPedInAnyHeli(player))
            {
                Common.Notification("Don't spawn that poor pet on a heli");
                return -1;
            }
            else if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (API.GetVehicleDashboardSpeed(vehicle) > 0.1f)
                {
                    Common.Notification("Player is in a moving vehicle");
                    return -1;
                }
            }

            var ped = await Peds.Spawn(model, coords, true, 28);
            Companions.Add(ped);
            await Peds.Arm(ped, null);
            return ped;
        }

        public static async Task PocceCompanion()
        {
            var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
            var ped = await SpawnHuman(pocce);
            API.SetPedAsNoLongerNeeded(ref ped);
        }

        public static async Task PetCompanion()
        {
            var pet = Config.PetList[API.GetRandomIntInRange(0, Config.PetList.Length)];
            var ped = await SpawnNonhuman(pet);
            API.SetPedAsNoLongerNeeded(ref ped);
        }

        private static async Task OnSelect(MenuListItem item, int index)
        {
            var skins = item.ItemData as List<Skin>;
            if (skins != null && skins.Count > index)
            {
                var skin = skins[index];
                await CustomCompanion(skin.Model, skin);
            }
        }

        private static Task OnSelect(string model)
        {
            uint hash;
            if (model.StartsWith("0x"))
                hash = uint.Parse(model.Substring(2), System.Globalization.NumberStyles.HexNumber);
            else
                hash = (uint)API.GetHashKey(model);

            return CustomCompanion(hash, null);
        }

        public void CustomCompanion()
        {
            _source = (ParentMenu as MainMenu).SkinMenu.DetectedSkins;
            OpenMenu();
        }

        public static async Task CustomCompanion(uint model, Skin skin)
        {
            if (skin != null)
            {
                var ped = skin.IsHuman ? await SpawnHuman(model) : await SpawnNonhuman(model);
                skin.Restore(ped);
                API.SetPedAsNoLongerNeeded(ref ped);
            }
            else
            {
                var ped = await SpawnHuman(model);
                API.SetPedAsNoLongerNeeded(ref ped);
            }
        }

        public static async Task CustomCompanionByName()
        {
            var model = await Common.GetUserInput("Spawn companion by model name", "", 30);
            if (string.IsNullOrEmpty(model))
                return;

            var hash = (uint)API.GetHashKey(model);
            var ped = await SpawnHuman(hash);
            API.SetPedAsNoLongerNeeded(ref ped);
        }

        public static async Task PoccePassengers()
        {
            if (!Common.EnsurePlayerIsInVehicle(out int player, out int vehicle))
                return;

            while (Vehicles.GetFreeSeat(vehicle, out int seat))
            {
                var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
                await Common.RequestModel(pocce);
                var ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                API.SetModelAsNoLongerNeeded(pocce);
                API.SetEntityAsNoLongerNeeded(ref ped);
            }
        }
    }
}
