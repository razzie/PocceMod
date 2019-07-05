using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod
{
    public static class Manager
    {
        private static readonly string PocceCompanionDecor = "POCCE_COMPANION";
        private static Menu _menu;
        private static Dictionary<int, Func<Task>> _menuItemActions;
        private static Dictionary<int, Func<int, Task>> _menuListItemActions;

        static Manager()
        {
            MenuController.MenuToggleKey = Control.SelectCharacterMichael;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;

            _menu = new Menu("PocceMod", "menu");
            MenuController.AddMenu(_menu);
            _menuItemActions = new Dictionary<int, Func<Task>>();
            _menuListItemActions = new Dictionary<int, Func<int, Task>>();

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
                if (_menuListItemActions.TryGetValue(_itemIndex, out Func<int, Task> action))
                {
                    await action(_listIndex);
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

        public static List<int> GetPeds(bool includeAnimals = true, bool includePlayers = false, bool includeDead = false, float rangeSquared = 900.0f)
        {
            var peds = new List<int>();
            int ped = 0;
            int handle = API.FindFirstPed(ref ped);
            var player = Game.Player.Character.Handle;
            var coords = Game.Player.Character.Position;

            if (handle == -1)
                return peds;

            do
            {
                var pos = API.GetEntityCoords(ped, true);

                if (!includeAnimals && !API.IsPedHuman(ped))
                    continue;

                if (!includePlayers && API.IsPedAPlayer(ped))
                    continue;

                if (!includeDead && API.IsPedDeadOrDying(ped, true))
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared || ped == player)
                    continue;

                peds.Add(ped);

            } while (API.FindNextPed(handle, ref ped));

            API.EndFindPed(handle);
            return peds;
        }

        public static List<int> GetVehicles(bool includeWithDriver = true, float rangeSquared = 900.0f)
        {
            var vehicles = new List<int>();
            int vehicle = 0;
            int handle = API.FindFirstVehicle(ref vehicle);
            var player = Game.Player.Character.Handle;
            var playerVehicle = API.GetVehiclePedIsIn(player, false);
            var coords = Game.Player.Character.Position;

            if (handle == -1)
                return vehicles;

            do
            {
                var pos = API.GetEntityCoords(vehicle, false);

                if (vehicle == playerVehicle)
                    continue;

                if (!includeWithDriver && !API.IsVehicleSeatFree(vehicle, -1))
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared)
                    continue;

                vehicles.Add(vehicle);

            } while (API.FindNextVehicle(handle, ref vehicle));

            API.EndFindVehicle(handle);
            return vehicles;
        }

        public static List<int> GetProps(float rangeSquared = 100.0f)
        {
            var props = new List<int>();
            int prop = 0;
            int handle = API.FindFirstObject(ref prop);
            var coords = Game.Player.Character.Position;

            if (handle == -1)
                return props;

            do
            {
                var pos = API.GetEntityCoords(prop, false);

                if (API.IsEntityAPed(prop) || API.IsEntityAVehicle(prop))
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared)
                    continue;

                props.Add(prop);

            } while (API.FindNextObject(handle, ref prop));

            API.EndFindObject(handle);
            return props;
        }

        public static bool GetClosestEntity(IEnumerable<int> entities, out int closest)
        {
            closest = -1;
            bool found = false;
            float minDist = float.MaxValue;
            var coords = Game.Player.Character.Position;

            foreach (var entity in entities)
            {
                var pos = API.GetEntityCoords(entity, API.IsEntityAPed(entity));
                var dist = coords.DistanceToSquared(pos);

                if (dist < minDist)
                {
                    closest = entity;
                    minDist = dist;
                    found = true;
                }
            }

            return found;
        }

        public static List<int> GetCompanions(IEnumerable<int> peds)
        {
            var companions = new List<int>();

            foreach (var ped in peds)
            {
                if (API.DecorGetBool(ped, PocceCompanionDecor) && API.NetworkHasControlOfEntity(ped))
                    companions.Add(ped);

                //API.DoesEntityBelongToThisScript(ped, )
            }

            return companions;
        }

        public static async Task RequestModel(uint model)
        {
            while (!API.HasModelLoaded(model))
            {
                API.RequestModel(model);
                await BaseScript.Delay(10);
            }
        }

        public static async Task<int> SpawnVehicle(string model)
        {
            var pos = Game.Player.Character.Position;
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            if (API.IsPedInAnyVehicle(Game.Player.Character.Handle, false))
            {
                Notification("Player is in a vehicle");
                return -1;
            }

            await RequestModel(hash);
            var vehicle = API.CreateVehicle(hash, pos.X, pos.Y, pos.Z + 1.0f, Game.Player.Character.Heading, true, false);
            Game.Player.Character.SetIntoVehicle(new Vehicle(vehicle), VehicleSeat.Driver);
            return vehicle;
        }

        public static async Task<int> SpawnProp(string model)
        {
            var player = Game.Player.Character.Handle;
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            await RequestModel(hash);
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                var roofBone = API.GetEntityBoneIndexByName(vehicle, "roof");
                var pos = API.GetWorldPositionOfEntityBone(vehicle, roofBone);
                var prop = API.CreateObject((int)hash, pos.X, pos.Y, pos.Z + 2.0f, true, false, true);
                //API.AttachEntityToEntityPhysically(obj, vehicle, 0, roofBone, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1000.0f, true, false, true, true, 2);
                API.AttachEntityToEntity(prop, vehicle, roofBone, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, false, false, false, false, 0, true);
                return prop;
            }
            else
            {
                var pos = Game.Player.Character.Position;
                var heading = API.GetEntityRotation(player, 0).Z;
                var prop = API.CreateObject((int)hash, pos.X + (float)Math.Cos(heading), pos.Y + (float)Math.Sin(heading), pos.Z - 1.0f, true, false, true);
                API.SetEntityCollision(prop, true, true);
                return prop;
            }
        }

        public static async Task<int> SpawnPed(uint[] modelList, int pedType = 26)
        {
            var model = modelList[API.GetRandomIntInRange(0, modelList.Length)];
            var coords = Game.Player.Character.Position;
            var pos = new Vector3();

            if (!API.GetSafeCoordForPed(coords.X, coords.Y, coords.Z, true, ref pos, 16))
            {
                pos.X = coords.X;
                pos.Y = coords.Y;
                pos.Z = coords.Z + 1.0f;
            }

            await RequestModel(model);
            return API.CreatePed(pedType, model, pos.X, pos.Y, pos.Z, 0.0f, true, false);
        }

        public static async Task ArmPed(int ped, uint[] weaponList)
        {
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await BaseScript.Delay(10);

            API.SetPedCombatAbility(ped, 100);
            API.SetPedCombatMovement(ped, 2);
            API.SetPedCombatRange(ped, 2);
            API.SetPedHearingRange(ped, float.MaxValue);
            API.SetPedCombatAttributes(ped, 5, true);
            API.SetPedCombatAttributes(ped, 17, true);
            API.SetPedCombatAttributes(ped, 46, true);
            API.SetPedCombatAttributes(ped, 1424, true);
            API.SetPedFleeAttributes(ped, 0, false);
            API.SetEntityHealth(ped, 200);

            if (weaponList != null)
            {
                API.SetPedArmour(ped, 200);
                var weapon = weaponList[API.GetRandomIntInRange(0, weaponList.Length)];
                new Ped(ped).Weapons.Give((WeaponHash)weapon, int.MaxValue, true, true);
            }
        }

        public static async Task MakeCompanion(int ped)
        {
            var player = Game.Player.Character.Handle;
            var playerGroup = API.GetPedGroupIndex(player);

            if (!API.DecorIsRegisteredAsType(PocceCompanionDecor, 2))
                API.DecorRegister(PocceCompanionDecor, 2);

            API.DecorSetBool(ped, PocceCompanionDecor, true);
            API.SetPedRelationshipGroupHash(ped, (uint)API.GetPedRelationshipGroupHash(player));

            API.ClearPedTasksImmediately(ped);
            API.TaskSetBlockingOfNonTemporaryEvents(ped, true);
            API.SetPedKeepTask(ped, true);
            await BaseScript.Delay(10);

            API.TaskGoToEntity(ped, player, -1, 5.0f, 2.0f, 0, 0);
        }

        public static async Task UpdateCompanions()
        {
            var player = Game.Player.Character.Handle;
            var peds = GetPeds();
            var companions = GetCompanions(peds);

            int target = 0;
            if (API.GetEntityPlayerIsFreeAimingAt(API.GetPlayerIndex(), ref target) || API.GetPlayerTargetEntity(API.GetPlayerIndex(), ref target))
            {
                foreach (var companion in companions)
                {
                    API.TaskCombatPed(companion, target, 0, 16);
                }
                return;
            }

            foreach (var ped in peds)
            {
                if (API.IsPedInCombat(ped, player))
                {
                    if (API.DecorGetBool(ped, PocceCompanionDecor))
                    {
                        API.ClearPedTasks(ped);
                        continue;
                    }

                    foreach (var companion in companions)
                    {
                        API.TaskCombatPed(companion, ped, 0, 16);
                    }
                    return;
                }
            }

            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                foreach (var companion in companions)
                {
                    if (!API.IsPedHuman(companion))
                        continue;

                    if (!API.AreAnyVehicleSeatsFree(vehicle))
                        break;

                    if (API.IsPedInAnyVehicle(companion, true))
                    {
                        var otherVehicle = API.GetVehiclePedIsUsing(companion);
                        if (otherVehicle != vehicle)
                        {
                            API.TaskLeaveVehicle(companion, otherVehicle, 0);
                            await BaseScript.Delay(10);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var model = (uint)API.GetEntityModel(vehicle);
                    int seats = API.GetVehicleModelNumberOfSeats(model);

                    for (int seat = -1; seat < seats; ++seat)
                    {
                        if (API.IsVehicleSeatFree(vehicle, seat))
                        {
                            API.TaskEnterVehicle(companion, vehicle, -1, 0, 2.0f, 1, 0);
                            continue;
                        }
                    }
                }
            }
            else
            {
                foreach (var companion in companions)
                {
                    var pos = API.GetEntityCoords(companion, true);
                    if (API.IsPedInAnyVehicle(companion, true))
                    {
                        var vehicle = API.GetVehiclePedIsIn(companion, false);
                        if (API.IsVehicleStopped(vehicle))
                            API.TaskLeaveVehicle(companion, vehicle, 0);
                        else
                            API.TaskLeaveVehicle(companion, vehicle, 4096);
                    }
                    else if (pos.DistanceToSquared(Game.Player.Character.Position) > 25.0f)
                    {
                        API.TaskGoToEntity(companion, player, -1, 5.0f, 2.0f, 0, 0);
                    }
                    else if (API.IsPedHuman(companion))
                    {
                        if (!API.IsPedActiveInScenario(companion))
                        {
                            var heading = API.GetEntityHeading(companion);
                            var scenario = Config.ScenarioList[API.GetRandomIntInRange(0, Config.ScenarioList.Length)];
                            API.TaskLookAtEntity(companion, player, -1, 2048, 3);
                            API.TaskStandGuard(companion, pos.X, pos.Y, pos.Z, heading, scenario);
                        }
                    }
                    else
                    {
                        API.TaskLookAtEntity(companion, player, -1, 2048, 3);
                    }
                }
            }
        }

        public static int AttachRope(int entity)
        {
            var player = Game.Player.Character.Handle;
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                return AttachRope(vehicle, entity);
            }
            else
            {
                return AttachRope(player, entity);
            }
        }

        public static int AttachRope(int entity1, int entity2)
        {
            var pos1 = API.GetEntityCoords(entity1, API.IsEntityAPed(entity1));
            var pos2 = API.GetEntityCoords(entity2, API.IsEntityAPed(entity2));
            var length = (float)Math.Sqrt(pos1.DistanceToSquared(pos2));

            int unkPtr = 0;
            var rope = API.AddRope(pos1.X, pos1.Y, pos1.Z, 0.0f, 0.0f, 0.0f, length, 1, length, 1.0f, 0.0f, false, false, false, 5.0f, true, ref unkPtr);
            API.AttachEntitiesToRope(rope, entity1, entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, length, false, false, null, null);
            return rope;
        }

        public static void AddMenuItem(string item, Func<Task> onSelect)
        {
            var menuItem = new MenuItem(item);
            _menu.AddMenuItem(menuItem);
            _menuItemActions.Add(menuItem.Index, onSelect);
        }

        public static void AddMenuListItem(string name, Func<int, Task> onSelect, params string[] items)
        {
            var menuListItem = new MenuListItem(name, new List<string>(items), 0);
            _menu.AddMenuItem(menuListItem);
            _menuListItemActions.Add(menuListItem.Index, onSelect);
        }

        public static void AddSubmenu(string name, Func<string, Task> onSelect, IEnumerable<string> items, int groupByLetters = 0)
        {
            var submenuItem = new MenuItem(name);
            var submenu = new Menu("PocceMod", name);

            _menu.AddMenuItem(submenuItem);
            MenuController.AddSubmenu(_menu, submenu);
            MenuController.BindMenuItem(_menu, submenu, submenuItem);

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

            if (groupByLetters > 0)
            {
                var itemList = new List<string>();
                string lastItemPrefix = string.Empty;
                foreach (var item in items)
                {
                    var itemPrefix = item.Substring(0, groupByLetters);
                    if (itemPrefix != lastItemPrefix)
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
                        lastItemPrefix = itemPrefix;
                    }

                    itemList.Add(item);
                }
                if (itemList.Count > 0)
                {
                    var menuListItem = new MenuListItem(lastItemPrefix, itemList, 0);
                    submenu.AddMenuItem(menuListItem);
                }
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
            var submenuItem = new MenuItem(name);
            var submenu = new Menu("PocceMod", name);

            _menu.AddMenuItem(submenuItem);
            MenuController.AddSubmenu(_menu, submenu);
            MenuController.BindMenuItem(_menu, submenu, submenuItem);

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
        }
    }
}
