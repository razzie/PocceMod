using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Main : BaseScript
    {
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
            if (Permission.CanDo(Ability.SpawnVehicle))
                Hud.AddSubmenu("Spawn vehicle", async (vehicle) => await Vehicles.Spawn(vehicle), Config.VehicleList);

            if (Permission.CanDo(Ability.SpawnProp))
            {
                Hud.AddSubmenu("Spawn prop", async (prop) => await Props.Spawn(prop), Config.PropList, 10);
                API.RegisterCommand("prop", new Action<int, List<object>, string>(PropCmd), false);
            }

            if (Permission.CanDo(Ability.SpawnPocceCompanion))
                Hud.AddMenuListItem("Spawn ped", "Pocce companion", PocceCompanion);

            if (Permission.CanDo(Ability.SpawnPetCompanion))
                Hud.AddMenuListItem("Spawn ped", "Pet companion", PetCompanion);

            if (Permission.CanDo(Ability.SpawnPoccePassengers))
                Hud.AddMenuListItem("Spawn ped", "Pocce passengers", PoccePassengers);

            if (Permission.CanDo(Ability.SpawnTrashPed))
                Hud.AddMenuListItem("Spawn ped", "Trash ped", SpawnTrashPed);

            if (Permission.CanDo(Ability.Rope))
            {
                Hud.AddMenuListItem("Rope", "Closest ped", () => RopeClosest(Peds.Get(Peds.Filter.Dead | Peds.Filter.CurrentVehiclePassengers)));
                Hud.AddMenuListItem("Rope", "Closest vehicle", () => RopeClosest(Vehicles.Get()));
                Hud.AddMenuListItem("Rope", "Closest vehicle tow", () => RopeClosest(Vehicles.Get(), true));
                Hud.AddMenuListItem("Rope", "Closest prop", () => RopeClosest(Props.Get()));
            }

            if (Permission.CanDo(Ability.RopeGun))
                Hud.AddMenuListItem("Rope", "Equip rope gun", () => Ropes.EquipRopeGun());

            if (Permission.CanDo(Ability.RappelFromHeli))
                Hud.AddMenuListItem("Rope", "Rappel from heli", () => RappelFromHeli());

            if (Permission.CanDo(Ability.Rope) || Permission.CanDo(Ability.RopeGun))
            {
                Hud.AddMenuListItem("Clear", "Ropes", () => Ropes.ClearAll());
                Hud.AddMenuListItem("Clear", "Last rope", () => Ropes.ClearLast());
            }

            if (Permission.CanDo(Ability.SpawnProp))
            {
                Hud.AddMenuListItem("Clear", "Props", () => Props.ClearAll());
                Hud.AddMenuListItem("Clear", "Last prop", () => Props.ClearLast());
            }

            if (Permission.CanDo(Ability.TeleportToClosestVehicle))
            {
                Hud.AddMenuListItem("Teleport", "Closest vehicle", () => TeleportToClosestVehicle());
                Hud.AddMenuListItem("Teleport", "Closest vehicle as passenger", () => TeleportToClosestVehicle(true));
            }

            if (Permission.CanDo(Ability.OceanWaves))
            {
                Hud.AddMenuListItem("Ocean waves", "High", () => API.SetWavesIntensity(8f));
                Hud.AddMenuListItem("Ocean waves", "Mid", () => API.SetWavesIntensity(2f));
                Hud.AddMenuListItem("Ocean waves", "Low", () => API.SetWavesIntensity(0f));
                Hud.AddMenuListItem("Ocean waves", "Reset", () => API.ResetWavesIntensity());
            }

            if (Permission.CanDo(Ability.PocceRiot))
                Hud.AddMenuListItem("Riot", "Pocce riot", async () => await PocceRiot(false));

            if (Permission.CanDo(Ability.PocceRiotArmed))
                Hud.AddMenuListItem("Riot", "Armed pocce riot", async () => await PocceRiot(true));

            if (Permission.CanDo(Ability.PedRiot))
                Hud.AddMenuListItem("Riot", "Ped riot", async () => await PedRiot(false));

            if (Permission.CanDo(Ability.PedRiotArmed))
                Hud.AddMenuListItem("Riot", "Armed ped riot", async () => await PedRiot(true));

            if (Permission.CanDo(Ability.Autopilot))
                Hud.AddMenuListItem("Other", "Autopilot", Autopilot.Toggle);

            if (Permission.CanDo(Ability.EMP))
                Hud.AddMenuListItem("Other", "EMP", () => Vehicles.EMP());

            if (Permission.CanDo(Ability.CargobobMagnet))
                Hud.AddMenuListItem("Other", "Cargobob magnet", () => CargobobMagnet());

            if (Permission.CanDo(Ability.UltrabrightHeadlight))
                Hud.AddMenuListItem("Other", "Ultrabright headlight", () => UltrabrightHeadlight());

            if (Permission.CanDo(Ability.IdentifySkins))
            {
                if (Permission.CanDo(Ability.ChangeSkin))
                {
                    var skins = new DataSource<string>();
                    Hud.AddMenuItem("Indentify skins", () => skins.Push(IdentifyPedModels()));
                    Hud.AddSubmenu("Change skin", async (skin) => await ChangeSkin(skin), skins);
                }
                else
                {
                    Hud.AddMenuItem("Indentify skins", () => IdentifyPedModels());
                }
            }

            var menukey = Config.GetConfigInt("MenuKey");
            if (menukey > 0)
            {
                Hud.SetMenuKey(menukey);
            }
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
                Hud.Notification("Player is not in a vehicle");
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
                    Hud.Notification("Player is in a moving vehicle and there are no free seats");
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
                Hud.Notification("Don't spawn that poor pet on a heli");
                return;
            }
            else if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (API.GetVehicleDashboardSpeed(vehicle) > 0.1f)
                {
                    Hud.Notification("Player is in a moving vehicle");
                    return;
                }
            }

            var ped = await Peds.Spawn(Config.PetList, 28);
            Companions.Add(ped);
            await Peds.Arm(ped, null);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static List<string> IdentifyPedModels()
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
                    Hud.Notification("ped:" + model);
                }
            }

            return models;
        }

        public static async Task ChangeSkin(string hexModel)
        {
            var model = uint.Parse(hexModel.Substring(2), System.Globalization.NumberStyles.HexNumber);
            await Game.Player.ChangeModel(new Model((PedHash)model));
        }

        public static void RopeClosest(IEnumerable<int> entities, bool tow = false)
        {
            if (Common.GetClosestEntity(entities, out int closest))
                Ropes.PlayerAttach(closest, tow ? Ropes.Mode.Tow : Ropes.Mode.Normal);
            else
                Hud.Notification("Nothing in range");
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
                Hud.Notification("Player is not in a vehicle");
                return;
            }

            var vehicle = API.GetVehiclePedIsIn(player, false);
            Vehicles.EnableUltrabrightHeadlight(vehicle);
            Hud.Notification("Use arrow up/down keys to change brightness");
        }

        public static void RappelFromHeli()
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyHeli(player))
            {
                API.TaskRappelFromHeli(player, 0);
            }
        }

        public static void TeleportToClosestVehicle(bool forcePassenger = false)
        {
            var vehicles = Vehicles.Get();
            if (Common.GetClosestEntity(vehicles, out int vehicle))
            {
                if (Vehicles.GetFreeSeat(vehicle, out int seat, forcePassenger))
                {
                    var player = API.GetPlayerPed(-1);
                    API.SetPedIntoVehicle(player, vehicle, seat);
                }
                else
                {
                    Hud.Notification("Closest vehicle doesn't have a free seat");
                }
            }
            else
            {
                Hud.Notification("No vehicles in range");
            }
        }

        private static void PropCmd(int source, List<object> args, string raw)
        {
            if (args.Count == 0)
                return;

            Prop(args[0].ToString());
        }

        public static void Prop(string prop)
        {
            if (!Permission.CanDo(Ability.SpawnProp))
                return;

            Hud.FilterSubmenu("Spawn prop", prop);
        }
    }
}
