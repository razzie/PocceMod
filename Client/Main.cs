using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Client.Menus;
using PocceMod.Shared;
using System;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Main : BaseScript
    {
        private static MainMenu _menu;

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
                _menu.AddMenuListItem("Vehicle", "Spawn from list ↕", _menu.VehicleMenu.OpenMenu);

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
                _menu.AddMenuListItem("Prop", "Spawn from list ↕", _menu.PropMenu.OpenMenu);
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
                _menu.AddMenuListItem("Rope", "Closest ped", () => Ropes.AttachToClosest(Peds.Get(Peds.Filter.LocalPlayer | Peds.Filter.Dead | Peds.Filter.CurrentVehiclePassengers)));
                _menu.AddMenuListItem("Rope", "Closest vehicle", () => Ropes.AttachToClosest(Vehicles.Get()));
                _menu.AddMenuListItem("Rope", "Closest vehicle tow", () => Ropes.AttachToClosest(Vehicles.Get(), true));
                _menu.AddMenuListItem("Rope", "Closest prop", () => Ropes.AttachToClosest(Props.Get()));
            }

            if (Permission.CanDo(Ability.Rope) || Permission.CanDo(Ability.RopeGun))
            {
                _menu.AddMenuListItem("Rope", "Clear last", Ropes.ClearLast);
                _menu.AddMenuListItem("Rope", "Clear all", Ropes.ClearAll);
            }
            #endregion

            #region Companion
            if (Permission.CanDo(Ability.SpawnPocceCompanion))
                _menu.AddMenuListItemAsync("Companion", "Spawn pocce", CompanionMenu.PocceCompanion);

            if (Permission.CanDo(Ability.SpawnPetCompanion))
                _menu.AddMenuListItemAsync("Companion", "Spawn pet", CompanionMenu.PetCompanion);

            if (Permission.CanDo(Ability.SpawnPoccePassengers))
                _menu.AddMenuListItemAsync("Companion", "Pocce passengers", CompanionMenu.PoccePassengers);
            #endregion

            #region Riot
            if (Permission.CanDo(Ability.PocceRiot))
                _menu.AddMenuListItem("Riot", "Pocce riot", async () => await RiotMenu.PocceRiot(false));

            if (Permission.CanDo(Ability.PocceRiotArmed))
                _menu.AddMenuListItem("Riot", "Pocce riot (armed)", async () => await RiotMenu.PocceRiot(true));

            if (Permission.CanDo(Ability.PedRiot))
                _menu.AddMenuListItem("Riot", "Ped riot", async () => await RiotMenu.PedRiot(false));

            if (Permission.CanDo(Ability.PedRiotArmed))
                _menu.AddMenuListItem("Riot", "Ped riot (armed)", async () => await RiotMenu.PedRiot(true));
            #endregion

            #region Skin
            if (Permission.CanDo(Ability.IdentifySkins))
                _menu.AddMenuListItem("Skin", "Detect nearby skins", _menu.SkinMenu.DetectSkins);

            if (Permission.CanDo(Ability.ChangeSkin))
            {
                _menu.AddMenuListItem("Skin", "Choose from last detect ↕", _menu.SkinMenu.ShowLastSkins);
                _menu.AddMenuListItem("Skin", "Choose from all ↕", _menu.SkinMenu.ShowAllSkins);
                _menu.AddMenuListItem("Skin", "Choose pocce skin ↕", _menu.SkinMenu.ShowPocceSkins);
            }
            #endregion

            #region Extra
            if (Permission.CanDo(Ability.OceanWaves))
                _menu.AddMenuListItem("Extra", "Crazy ocean waves (toggle)", ExtraMenu.ToggleCrazyOceanWaves);

            if (Permission.CanDo(Ability.RappelFromHeli))
                _menu.AddMenuListItemAsync("Extra", "Rappel from heli", ExtraMenu.RappelFromHeli);

            if (Permission.CanDo(Ability.UltrabrightHeadlight))
                _menu.AddMenuListItem("Extra", "Ultrabright headlight", ExtraMenu.UltrabrightHeadlight);

            if (Permission.CanDo(Ability.EMP))
                _menu.AddMenuListItem("Extra", "EMP", () => Vehicles.EMP());

            if (Permission.CanDo(Ability.CargobobMagnet))
                _menu.AddMenuListItem("Extra", "Cargobob magnet", ExtraMenu.CargobobMagnet);

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
    }
}
