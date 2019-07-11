using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Mod;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod
{
    public sealed class Client : BaseScript
    {
        public Client()
        {
            var skins = new DataSource<string>();

            Hud.AddSubmenu("Spawn vehicle", async (vehicle) => await Vehicles.Spawn(vehicle), Config.VehicleList);
            Hud.AddSubmenu("Spawn prop", async (prop) => await Props.Spawn(prop), Config.PropList, 10);

            Hud.AddMenuListItem("Spawn", async (spawn) =>
            {
                switch (spawn)
                {
                    case 0:
                        await PocceCompanion();
                        break;
                    case 1:
                        await PetCompanion();
                        break;
                    case 2:
                        await PoccePassengers();
                        break;
                    case 3:
                        await SpawnTrashPed();
                        break;
                }
            }, "Pocce companion", "Pet companion", "Pocce passengers", "Trash ped");

            Hud.AddMenuListItem("Riot", async (riot) =>
            {
                switch (riot)
                {
                    case 0:
                        await PocceRiot(false);
                        break;
                    case 1:
                        await PocceRiot(true);
                        break;
                    case 2:
                        await PedRiot(false);
                        break;
                    case 3:
                        await PedRiot(true);
                        break;
                }
            }, "Pocce riot", "Armed pocce riot", "Ped riot", "Armed ped riot");

            Hud.AddMenuListItem("Rope", (tow) =>
            {
                switch (tow)
                {
                    case 0:
                        RopeClosest(Peds.Get());
                        break;
                    case 1:
                        RopeClosest(Vehicles.Get());
                        break;
                    case 2:
                        RopeClosest(Vehicles.Get(), true);
                        break;
                    case 3:
                        RopeClosest(Props.Get());
                        break;
                    case 4:
                        RappelFromHeli();
                        break;
                }
                return Delay(0);
            }, "Closest ped", "Closest vehicle", "Closest vehicle tow", "Closest prop", "Rappel from heli");

            Hud.AddMenuListItem("Clear", (clear) =>
            {
                switch (clear)
                {
                    case 0:
                        Ropes.ClearAll();
                        break;
                    case 1:
                        Ropes.ClearLast();
                        break;
                    case 2:
                        Props.ClearAll();
                        break;
                    case 3:
                        Props.ClearLast();
                        break;
                }
                return Delay(0);
            }, "Ropes", "Last rope", "Props", "Last prop");

            Hud.AddMenuListItem("Other", async (other) =>
            {
                switch (other)
                {
                    case 0:
                        await Autopilot.Toggle();
                        break;
                    case 1:
                        Vehicles.EMP();
                        break;
                    case 2:
                        CargobobMagnet();
                        break;
                }
            }, "Autopilot", "EMP", "Cargobob magnet");

            Hud.AddMenuItem("Indentify skins", () => { skins.Push(IdentifyPedModels()); return Delay(0); });
            Hud.AddSubmenu("Change skin", async (skin) => await ChangeSkin(skin), skins);
        }

        public static async Task SpawnTrashPed()
        {
            var ped = await Peds.Spawn(Config.TrashPedList);
            await Delay(500);
            Common.Burn(ped);
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
            int player = Game.Player.Character.Handle;
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
            int player = Game.Player.Character.Handle;
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
            var coords = Game.Player.Character.Position;
            var peds = Peds.Get();
            var models = new List<string>();

            foreach (var ped in peds)
            {
                var pos = new Ped(ped).Position;
                if (coords.DistanceToSquared(pos) < 4.0f)
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
                Ropes.PlayerAttach(closest, tow);
            else
                Hud.Notification("Nothing in range");
        }

        public static void CargobobMagnet()
        {
            var player = Game.Player.Character.Handle;
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

        public static void RappelFromHeli()
        {
            var player = Game.Player.Character.Handle;
            if (API.IsPedInAnyHeli(player))
            {
                API.TaskRappelFromHeli(player, 0);
            }
        }
    }
}
