using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod
{
    public sealed class PocceMod : BaseScript
    {
        private static List<int> _props = new List<int>();
        private static List<int> _ropes = new List<int>();

        public PocceMod()
        {
            SetupMenu();

            Tick += OnTick;
        }

        public static async Task SpawnTrashPed()
        {
            var ped = await Manager.SpawnPed(Config.TrashPedList);
            API.StartEntityFire(ped);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PedRiot(bool useWeapons)
        {
            int i = 0;
            var peds = Manager.GetPeds();
            var weapons = useWeapons ? Config.WeaponList : null;

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
            var weapons = useWeapons ? Config.WeaponList : null;

            for (int i = 0; i < 4; ++i)
            {
                int ped1 = await Manager.SpawnPed(Config.PocceList);
                int ped2 = await Manager.SpawnPed(Config.PocceList);

                peds.Add(ped1);
                peds.Add(ped2);

                await Manager.ArmPed(ped1, weapons);
                await Manager.ArmPed(ped2, weapons);

                API.TaskCombatPed(ped1, ped2, 0, 16);
                API.TaskCombatPed(ped2, ped1, 0, 16);
            }

            for (int i = 0; i < 4; ++i)
            {
                int ped = await Manager.SpawnPed(Config.PocceList);
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
                    var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
                    await Manager.RequestModel(pocce);
                    var ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                    API.SetEntityAsNoLongerNeeded(ref ped);
                }
            }
        }

        public static async Task PocceCompanion()
        {
            var ped = await Manager.SpawnPed(Config.PocceList);
            await Manager.MakeCompanion(ped);
            await Manager.ArmPed(ped, Config.WeaponList);
            API.SetEntityAsNoLongerNeeded(ref ped);
        }

        public static async Task PetCompanion()
        {
            var ped = await Manager.SpawnPed(Config.PetList, 28);
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

        public static async Task ChangeSkin(string hexModel)
        {
            var model = uint.Parse(hexModel.Substring(2), System.Globalization.NumberStyles.HexNumber);
            await Game.Player.ChangeModel(new Model((PedHash)model));
        }

        public static void TowClosest(IEnumerable<int> entities)
        {
            if (Manager.GetClosestEntity(entities, out int closest))
                _ropes.Add(Manager.AttachRope(closest));
            else
                Manager.Notification("nothing in range");
        }

        public static void ClearRopes()
        {
            foreach (var rope in _ropes)
            {
                var tmp_rope = rope;
                API.DeleteRope(ref tmp_rope);
            }

            _ropes.Clear();
        }
        
        public static void ClearProps()
        {
            foreach (var prop in _props)
            {
                var tmp_prop = prop;
                API.DeleteObject(ref tmp_prop);
            }

            _props.Clear();
        }

        private void SetupMenu()
        {
            var skins = new DataSource<string>();

            Manager.AddSubmenu("Spawn vehicle", async (vehicle) => await Manager.SpawnVehicle(vehicle), Config.VehicleList);
            Manager.AddSubmenu("Spawn prop", async (prop) => { var entity = await Manager.SpawnProp(prop); _props.Add(entity); }, Config.PropList, 10);

            Manager.AddMenuListItem("Spawn", async (spawn) =>
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

            Manager.AddMenuListItem("Riot", async (riot) =>
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

            Manager.AddMenuListItem("Tow", (tow) =>
            {
                switch (tow)
                {
                    case 0:
                        TowClosest(Manager.GetPeds(true, true, false));
                        break;
                    case 1:
                        TowClosest(Manager.GetVehicles(true));
                        break;
                    case 2:
                        TowClosest(Manager.GetProps(900.0f));
                        break;
                }
                return null;
            }, "Closest ped", "Closest vehicle", "Closest prop");

            Manager.AddMenuListItem("Clear", (clear) =>
            {
                switch (clear)
                {
                    case 0:
                        ClearRopes();
                        break;
                    case 1:
                        ClearProps();
                        break;
                }
                return null;
            }, "Ropes", "Props");

            Manager.AddMenuItem("Indentify skins", () => { skins.Push(IdentifyPedModels()); return null; });
            Manager.AddSubmenu("Change skin", async (skin) => await ChangeSkin(skin), skins);
        }

        private async Task OnTick()
        {
            await Delay(2000);
            await Manager.UpdateCompanions();
        }
    }
}
