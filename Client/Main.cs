using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Client.Menus;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Main : BaseScript
    {
        private static MainMenu _menu;
        private static bool _crazyOceanWaves = false;

        public Main()
        {
            Permission.Granted += (player, group) => SetupMenu();

            EventHandlers["PocceMod:Burn"] += new Action<int>(async entity =>
            {
                API.StartEntityFire(await Common.WaitForNetEntity(entity));
            });
        }

        private static void SetupMenu()
        {
            _menu = new MainMenu();

            #region Vehicle
            if (Permission.CanDo(Ability.SpawnVehicle))
                _menu.AddMenuListItem("Vehicle", "Spawn from list ☰", _menu.VehicleMenu.OpenMenu);

            if (Permission.CanDo(Ability.SpawnVehicleByName))
                _menu.AddMenuListItemAsync("Vehicle", "Spawn by name", VehicleMenu.SpawnByName);

            if (Permission.CanDo(Ability.Autopilot))
                _menu.AddMenuListItemAsync("Vehicle", "Autopilot (toggle)", Autopilot.Toggle);

            if (Permission.CanDo(Ability.TeleportToClosestVehicle))
            {
                _menu.AddMenuListItem("Vehicle", "TP into closest", () => VehicleMenu.TeleportToClosestVehicle());
                _menu.AddMenuListItem("Vehicle", "TP into closest as passenger", () => VehicleMenu.TeleportToClosestVehicle(true));
            }
            #endregion

            #region Prop
            if (Permission.CanDo(Ability.SpawnProp))
            {
                _menu.AddMenuListItem("Prop", "Spawn from list ☰", _menu.PropMenu.OpenMenu);
                _menu.AddMenuListItem("Prop", "Spawn from list (search)", async () =>
                {
                    var prop = await Common.GetUserInput("Filter props", "", 30);
                    _menu.PropMenu.Filter(prop);
                });
                _menu.AddMenuListItem("Prop", "Clear last", Props.ClearLast);
                _menu.AddMenuListItem("Prop", "Clear all", Props.ClearAll);
            }
            #endregion

            #region Rope
            if (Permission.CanDo(Ability.RopeGun))
                _menu.AddMenuListItem("Rope", "Equip rope gun", Ropes.EquipRopeGun);

            if (Permission.CanDo(Ability.Rope))
            {
                _menu.AddMenuListItem("Rope", "Closest ped", () => RopeClosest(Peds.Get(Peds.Filter.LocalPlayer | Peds.Filter.Dead | Peds.Filter.CurrentVehiclePassengers)));
                _menu.AddMenuListItem("Rope", "Closest vehicle", () => RopeClosest(Vehicles.Get()));
                _menu.AddMenuListItem("Rope", "Closest vehicle tow", () => RopeClosest(Vehicles.Get(), true));
                _menu.AddMenuListItem("Rope", "Closest prop", () => RopeClosest(Props.Get()));
            }

            if (Permission.CanDo(Ability.Rope) || Permission.CanDo(Ability.RopeGun))
            {
                _menu.AddMenuListItem("Rope", "Clear last", Ropes.ClearLast);
                _menu.AddMenuListItem("Rope", "Clear all", Ropes.ClearAll);
            }
            #endregion

            #region Companion
            if (Permission.CanDo(Ability.SpawnPocceCompanion))
                _menu.AddMenuListItemAsync("Companion", "Spawn pocce", PocceCompanion);

            if (Permission.CanDo(Ability.SpawnPetCompanion))
                _menu.AddMenuListItemAsync("Companion", "Spawn pet", PetCompanion);

            if (Permission.CanDo(Ability.SpawnPoccePassengers))
                _menu.AddMenuListItemAsync("Companion", "Pocce passengers", PoccePassengers);
            #endregion

            #region Riot
            if (Permission.CanDo(Ability.PocceRiot))
                _menu.AddMenuListItem("Riot", "Pocce riot", async () => await PocceRiot(false));

            if (Permission.CanDo(Ability.PocceRiotArmed))
                _menu.AddMenuListItem("Riot", "Pocce riot (armed)", async () => await PocceRiot(true));

            if (Permission.CanDo(Ability.PedRiot))
                _menu.AddMenuListItem("Riot", "Ped riot", async () => await PedRiot(false));

            if (Permission.CanDo(Ability.PedRiotArmed))
                _menu.AddMenuListItem("Riot", "Ped riot (armed)", async () => await PedRiot(true));
            #endregion

            #region Skin
            if (Permission.CanDo(Ability.IdentifySkins))
            {
                if (Permission.CanDo(Ability.ChangeSkin))
                {
                    _menu.AddMenuListItem("Skin", "Detect nearby skins", () => _menu.SkinMenu.DataSource.Push(SkinMenu.DetectSkins()));
                    //_menu.AddMenuListItem("Skin", "Choose from last detect ☰", ???);
                    _menu.AddMenuListItem("Skin", "Choose from all ☰", _menu.SkinMenu.OpenMenu);
                }
                else
                {
                    _menu.AddMenuListItem("Skin", "Detect nearby skins", () => SkinMenu.DetectSkins());
                }
            }
            #endregion

            #region Extra
            if (Permission.CanDo(Ability.OceanWaves))
            {
                _menu.AddMenuListItem("Extra", "Crazy ocean waves (toggle)", () =>
                {
                    _crazyOceanWaves = !_crazyOceanWaves;
                    if (_crazyOceanWaves)
                        API.SetWavesIntensity(8f);
                    else
                        API.ResetWavesIntensity();
                });
            }

            if (Permission.CanDo(Ability.RappelFromHeli))
                _menu.AddMenuListItemAsync("Extra", "Rappel from heli", RappelFromHeli);

            if (Permission.CanDo(Ability.UltrabrightHeadlight))
                _menu.AddMenuListItem("Extra", "Ultrabright headlight", UltrabrightHeadlight);

            if (Permission.CanDo(Ability.EMP))
                _menu.AddMenuListItem("Extra", "EMP", () => Vehicles.EMP());

            if (Permission.CanDo(Ability.CargobobMagnet))
                _menu.AddMenuListItem("Extra", "Cargobob magnet", CargobobMagnet);

            if (Permission.CanDo(Ability.SpawnTrashPed))
                _menu.AddMenuListItemAsync("Extra", "Trash ped", SpawnTrashPed);
            #endregion
        }

        public static async Task SpawnTrashPed()
        {
            var ped = await Peds.Spawn(Config.TrashPedList);
            TriggerServerEvent("PocceMod:Burn", API.PedToNet(ped));
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PedRiot(bool useWeapons)
        {
            int i = 0;
            var peds = Peds.Get(Peds.Filter.Dead | Peds.Filter.Players | (useWeapons ? Peds.Filter.Animals : Peds.Filter.None)); // do not include animals when using weapons
            var weapons = useWeapons ? Config.WeaponList : null;

            if (peds.Count < 2)
                return;

            foreach (int ped in peds)
            {
                if (API.IsPedInAnyVehicle(ped, false))
                {
                    var vehicle = API.GetVehiclePedIsIn(ped, false);
                    API.TaskLeaveVehicle(ped, vehicle, 1);
                }

                await Delay(1000); // let them get out of vehicles
                API.ClearPedTasks(ped);

                await Peds.Arm(ped, weapons);

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
            var weapons = useWeapons ? Config.WeaponList : null;

            for (int i = 0; i < 4; ++i)
            {
                int ped1 = await Peds.Spawn(Config.PocceList);
                int ped2 = await Peds.Spawn(Config.PocceList);

                peds.Add(ped1);
                peds.Add(ped2);

                await Peds.Arm(ped1, weapons);
                await Peds.Arm(ped2, weapons);

                API.TaskCombatPed(ped1, ped2, 0, 16);
                API.TaskCombatPed(ped2, ped1, 0, 16);
            }

            for (int i = 0; i < 4; ++i)
            {
                int ped = await Peds.Spawn(Config.PocceList);
                peds.Add(ped);
                await Peds.Arm(ped, weapons);
                API.TaskCombatPed(ped, API.GetPlayerPed(-1), 0, 16);
            }

            foreach (int ped in peds)
            {
                int tmp_ped = ped;
                API.SetEntityAsNoLongerNeeded(ref tmp_ped);
            }
        }

        public static async Task PoccePassengers()
        {
            int player = API.GetPlayerPed(-1);
            if (!API.IsPedInAnyVehicle(player, true))
            {
                Common.Notification("Player is not in a vehicle");
                return;
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            while (Vehicles.GetFreeSeat(vehicle, out int seat))
            {
                var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
                await Common.RequestModel(pocce);
                var ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                API.SetEntityAsNoLongerNeeded(ref ped);
            }
        }

        public static async Task PocceCompanion()
        {
            int ped;
            int player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (Vehicles.GetFreeSeat(vehicle, out int seat))
                {
                    var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
                    await Common.RequestModel(pocce);
                    ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                }
                else if (API.GetEntitySpeed(vehicle) > 0.1f)
                {
                    Common.Notification("Player is in a moving vehicle and there are no free seats");
                    return;
                }
                else
                {
                    ped = await Peds.Spawn(Config.PocceList);
                }
            }
            else
            {
                ped = await Peds.Spawn(Config.PocceList);
            }

            Companions.Add(ped);
            await Peds.Arm(ped, Config.WeaponList);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PetCompanion()
        {
            int player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                Common.Notification("Don't spawn that poor pet on a heli");
                return;
            }
            else if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (API.GetVehicleDashboardSpeed(vehicle) > 0.1f)
                {
                    Common.Notification("Player is in a moving vehicle");
                    return;
                }
            }

            var ped = await Peds.Spawn(Config.PetList, 28);
            Companions.Add(ped);
            await Peds.Arm(ped, null);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static void RopeClosest(IEnumerable<int> entities, bool tow = false)
        {
            if (Common.GetClosestEntity(entities, out int closest))
                Ropes.PlayerAttach(closest, Vector3.Zero, tow ? Ropes.Mode.Tow : Ropes.Mode.Normal);
            else
                Common.Notification("Nothing in range");
        }

        public static void CargobobMagnet()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                if (API.IsCargobobMagnetActive(heli))
                {
                    API.SetCargobobPickupMagnetActive(heli, false);
                    API.RetractCargobobHook(heli);
                }
                else
                {
                    API.EnableCargobobHook(heli, 1);
                    API.SetCargobobPickupMagnetActive(heli, true);
                }
            }
        }

        public static void UltrabrightHeadlight()
        {
            var player = API.GetPlayerPed(-1);
            if (!API.IsPedInAnyVehicle(player, true))
            {
                Common.Notification("Player is not in a vehicle");
                return;
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            Vehicles.EnableUltrabrightHeadlight(vehicle);
            Common.Notification("Use arrow up/down keys to change brightness");
        }

        public static async Task RappelFromHeli()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                var heli = API.GetVehiclePedIsIn(player, false);
                if (!Vehicles.GetPedSeat(heli, player, out int seat))
                    return;

                switch (seat)
                {
                    case -1:
                        if (API.AreAnyVehicleSeatsFree(heli))
                        {
                            await Autopilot.Activate();
                            API.TaskRappelFromHeli(player, 0);
                        }
                        break;

                    case 0:
                        if (Vehicles.GetFreeSeat(heli, out int goodSeat, true))
                        {
                            API.SetPedIntoVehicle(player, heli, goodSeat);
                            API.TaskRappelFromHeli(player, 0);
                        }
                        break;

                    default:
                        API.TaskRappelFromHeli(player, 0);
                        break;
                }
            }
            else
            {
                Common.Notification("Player is not in a heli");
            }
        }
    }
}
