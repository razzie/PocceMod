using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod
{
    public sealed class PocceMod : BaseScript
    {
        private Menu skinMenu;

        public PocceMod()
        {
            SetupMenu();

            Tick += OnTick;
        }

        public static async Task SpawnTrashPed()
        {
            var ped = await Manager.SpawnPed(Manager.TrashPedList);
            API.StartEntityFire(ped);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PedRiot(bool useWeapons)
        {
            int i = 0;
            var peds = Manager.GetPeds();
            var weapons = useWeapons ? Manager.WeaponList : null;

            if (peds.Count < 2)
                return;

            foreach (int ped in peds)
            {
                if (API.IsPedInAnyVehicle(ped, false))
                {
                    var vehicle = API.GetVehiclePedIsIn(ped, false);
                    API.TaskLeaveVehicle(ped, vehicle, 1);
                    await Delay(1000);
                }

                API.ClearPedTasks(ped);

                await Manager.ArmPed(ped, weapons);

                int enemyPed;
                if (i % 2 == 0)
                    enemyPed = peds[(i + 1) % peds.Count];
                else if (i == peds.Count - 1)
                    enemyPed = peds[0];
                else
                    enemyPed = peds[i - 1];
                API.TaskCombatPed(ped, enemyPed, 0, 16);

                int tmp_ped = ped; API.SetEntityAsNoLongerNeeded(ref tmp_ped);

                ++i;
            }
        }

        public static async Task PocceRiot(bool useWeapons)
        {
            var peds = new List<int>();
            var weapons = useWeapons ? Manager.WeaponList : null;

            for (int i = 0; i < 4; ++i)
            {
                int ped1 = await Manager.SpawnPed(Manager.PocceList);
                int ped2 = await Manager.SpawnPed(Manager.PocceList);

                peds.Add(ped1);
                peds.Add(ped2);

                await Manager.ArmPed(ped1, weapons);
                await Manager.ArmPed(ped2, weapons);

                API.TaskCombatPed(ped1, ped2, 0, 16);
                API.TaskCombatPed(ped2, ped1, 0, 16);
            }

            for (int i = 0; i < 4; ++i)
            {
                int ped = await Manager.SpawnPed(Manager.PocceList);
                peds.Add(ped);
                await Manager.ArmPed(ped, weapons);
                API.TaskCombatPed(ped, Game.Player.Character.Handle, 0, 16);
            }

            foreach (int ped in peds)
            {
                int tmp_ped = ped;
                API.SetEntityAsNoLongerNeeded(ref tmp_ped);
            }
        }

        public static async Task PoccePassengers()
        {
            int player = Game.Player.Character.Handle;

            if (!API.IsPedInAnyVehicle(player, true))
            {
                Manager.Notification("Player is not in a vehicle");
                return;
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            var vehicleModel = API.GetEntityModel(vehicle);
            var seats = API.GetVehicleModelNumberOfSeats((uint)vehicleModel);

            for (int seat = -1; seat < seats; ++seat)
            {
                if (API.IsVehicleSeatFree(vehicle, seat))
                {
                    var pocce = Manager.PocceList[API.GetRandomIntInRange(0, Manager.PocceList.Length)];
                    await Manager.RequestModel(pocce);
                    var ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                    API.SetEntityAsNoLongerNeeded(ref ped);
                }
            }
        }

        public static async Task PocceCompanion()
        {
            var ped = await Manager.SpawnPed(Manager.PocceList);
            await Manager.MakeCompanion(ped);
            await Manager.ArmPed(ped, Manager.WeaponList);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PetCompanion()
        {
            var ped = await Manager.SpawnPed(Manager.PetList, 28);
            await Manager.MakeCompanion(ped);
            await Manager.ArmPed(ped, null);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static List<string> IdentifyPedModels()
        {
            var coords = Game.Player.Character.Position;
            var peds = Manager.GetPeds(true, true);
            var models = new List<string>();

            foreach (var ped in peds)
            {
                var pos = new Ped(ped).Position;
                if (coords.DistanceToSquared(pos) < 4.0f)
                {
                    var model = string.Format("0x{0:X8}", API.GetEntityModel(ped));
                    models.Add(model);
                    Manager.Notification("ped:" + model);
                }
            }

            return models;
        }

        private void AddSkins(List<string> models)
        {
            foreach (var model in models)
            {
                bool newSkin = true;
                var skins = skinMenu.GetMenuItems();
                foreach (var skin in skins)
                {
                    if (skin.Text == model)
                    {
                        newSkin = false;
                        break;
                    }
                }
                if (newSkin)
                {
                    skinMenu.AddMenuItem(new MenuItem(model));
                }
            }
        }

        private void SetupMenu()
        {
            MenuController.MenuToggleKey = Control.SelectCharacterMichael;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;

            Menu menu = new Menu("PocceMod", "menu");
            MenuController.AddMenu(menu);

            var spawnVehicleButton = new MenuItem("Spawn vehicle");
            menu.AddMenuItem(spawnVehicleButton);
            var spawnPropButton = new MenuItem("Spawn prop");
            menu.AddMenuItem(spawnPropButton);
            menu.AddMenuItem(new MenuItem("Spawn trash ped"));
            menu.AddMenuItem(new MenuItem("Ped riot"));
            menu.AddMenuItem(new MenuItem("Ped riot with weapons"));
            menu.AddMenuItem(new MenuItem("Pocce riot"));
            menu.AddMenuItem(new MenuItem("Pocce riot with weapons"));
            menu.AddMenuItem(new MenuItem("Pocce passengers"));
            menu.AddMenuItem(new MenuItem("Pocce companion"));
            menu.AddMenuItem(new MenuItem("Pet companion"));
            menu.AddMenuItem(new MenuItem("Identify skins"));
            var skinMenuButton = new MenuItem("Change skin");
            menu.AddMenuItem(skinMenuButton);

            menu.OnItemSelect += async (_menu, _item, _index) =>
            {
                switch (_index)
                {
                    // submenus
                    case 0:
                    case 1:
                    case 11:
                        return;

                    case 2:
                        await SpawnTrashPed();
                        break;

                    case 3:
                        await PedRiot(false);
                        break;

                    case 4:
                        await PedRiot(true);
                        break;

                    case 5:
                        await PocceRiot(false);
                        break;

                    case 6:
                        await PocceRiot(true);
                        break;

                    case 7:
                        await PoccePassengers();
                        break;

                    case 8:
                        await PocceCompanion();
                        break;

                    case 9:
                        await PetCompanion();
                        break;

                    case 10:
                        var models = IdentifyPedModels();
                        AddSkins(models);
                        break;
                }

                menu.CloseMenu();
            };


            Menu vehicleMenu = new Menu("PocceMod", "select vehicle");
            MenuController.AddSubmenu(menu, vehicleMenu);
            MenuController.BindMenuItem(menu, vehicleMenu, spawnVehicleButton);

            string vehicles = API.LoadResourceFile(API.GetCurrentResourceName(), "vehicles.ini");
            foreach (var vehicle in vehicles.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                vehicleMenu.AddMenuItem(new MenuItem(vehicle));
            }

            vehicleMenu.OnItemSelect += async (_menu, _item, _index) =>
            {
                var vehicle = await Manager.SpawnVehicle(_item.Text);
                API.SetEntityAsNoLongerNeeded(ref vehicle);
                vehicleMenu.CloseMenu();
            };


            Menu propMenu = new Menu("PocceMod", "select prop");
            MenuController.AddSubmenu(menu, propMenu);
            MenuController.BindMenuItem(menu, propMenu, spawnPropButton);

            var propList = new List<string>();
            string lastPropPrefix = string.Empty;
            string props = API.LoadResourceFile(API.GetCurrentResourceName(), "props.ini");
            foreach (var prop in props.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var propPrefix = prop.Substring(0, 10);
                if (propPrefix != lastPropPrefix)
                {
                    if (propList.Count > 0)
                    {
                        if (propList.Count == 1)
                        {
                            var propItem = new MenuItem(propList[0]);
                            propMenu.AddMenuItem(propItem);
                        }
                        else
                        {
                            var propListItem = new MenuListItem(lastPropPrefix + "*", propList, 0);
                            propMenu.AddMenuItem(propListItem);
                        }
                        propList = new List<string>();
                    }
                    lastPropPrefix = propPrefix;
                }

                propList.Add(prop);
            }
            if (propList.Count > 0)
            {
                var propListItem = new MenuListItem(lastPropPrefix, propList, 0);
                propMenu.AddMenuItem(propListItem);
            }

            propMenu.OnItemSelect += async (_menu, _item, _index) =>
            {
                var prop = await Manager.SpawnProp(_item.Text);
                API.SetEntityAsNoLongerNeeded(ref prop);
                propMenu.CloseMenu();
            };

            propMenu.OnListItemSelect += async (_menu, _listItem, _listIndex, _itemIndex) =>
            {
                var prop = await Manager.SpawnProp(_listItem.ListItems[_listIndex]);
                API.SetEntityAsNoLongerNeeded(ref prop);
                propMenu.CloseMenu();
            };


            skinMenu = new Menu("PocceMod", "identified skins");
            MenuController.AddSubmenu(menu, skinMenu);
            MenuController.BindMenuItem(menu, skinMenu, skinMenuButton);

            skinMenu.OnItemSelect += async (_menu, _item, _index) =>
            {
                var model = uint.Parse(_item.Text.Substring(2), System.Globalization.NumberStyles.HexNumber);
                await Game.Player.ChangeModel(new Model((PedHash)model));
                skinMenu.CloseMenu();
            };
        }

        private async Task OnTick()
        {
            await Delay(2000);
            await Manager.UpdateCompanions();
        }
    }
}
