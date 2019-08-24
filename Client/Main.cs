using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Client.Menus;
using PocceMod.Client.Menus.Dev;
using PocceMod.Shared;
using System;
using System.Collections.Generic;

namespace PocceMod.Client
{
    public class Main : BaseScript
    {
        private static MainMenu _menu;

        public Main()
        {
            if (Permission.IgnorePermissions)
                SetupMenu();
            else
                Permission.Granted += (player, group) => SetupMenu();

            EventHandlers["PocceMod:OpenMenu"] += new Action(() => _menu?.OpenMenu());

            API.RegisterCommand("pocce", new Action<int, List<object>, string>(PocceCommand), false);
        }

        private static void SetupMenu()
        {
            _menu = new MainMenu();

            #region Vehicle
            if (Permission.CanDo(Ability.SpawnVehicle))
                _menu.AddMenuListItem("Vehicle", "Spawn from list ↕", _menu.Submenu<VehicleMenu>().OpenMenu);

            if (Permission.CanDo(Ability.SpawnVehicleByName))
                _menu.AddMenuListItemAsync("Vehicle", "Spawn by name", _menu.Submenu<VehicleMenu>().SpawnByName);

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
                _menu.AddMenuListItem("Prop", "Spawn from list ↕", _menu.Submenu<PropMenu>().OpenMenu);
                _menu.AddMenuListItem("Prop", "Spawn from list (search)", async () =>
                {
                    var prop = await Common.GetUserInput("Filter props", "", 30);
                    _menu.Submenu<PropMenu>().Filter(prop);
                });
                _menu.AddMenuListItemAsync("Prop", "Clear last", Props.ClearLast);
                _menu.AddMenuListItemAsync("Prop", "Clear all", Props.ClearAll);
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
                _menu.AddMenuListItem("Rope", "Free myself", Ropes.ClearPlayer);
                _menu.AddMenuListItem("Rope", "Clear last", Ropes.ClearLast);
                _menu.AddMenuListItem("Rope", "Clear all", Ropes.ClearAll);
            }
            #endregion

            #region Companion
            if (Permission.CanDo(Ability.SpawnPocceCompanion))
                _menu.AddMenuListItemAsync("Companion", "Spawn pocce", CompanionMenu.PocceCompanion);

            if (Permission.CanDo(Ability.SpawnPetCompanion))
                _menu.AddMenuListItemAsync("Companion", "Spawn pet", CompanionMenu.PetCompanion);

            if (Permission.CanDo(Ability.SpawnCustomCompanion))
            {
                _menu.AddMenuListItem("Companion", "Spawn custom ↕", _menu.Submenu<CompanionMenu>().CustomCompanion);
                _menu.AddMenuListItemAsync("Companion", "Spawn custom by name", CompanionMenu.CustomCompanionByName);
            }

            if (Permission.CanDo(Ability.SpawnPoccePassengers))
                _menu.AddMenuListItemAsync("Companion", "Pocce passengers", CompanionMenu.PoccePassengers);
            #endregion

            #region Event
            if (Permission.CanDo(Ability.PocceParty))
                _menu.AddMenuListItemAsync("Event", "Pocce party", EventMenu.PocceParty);

            if (Permission.CanDo(Ability.MassScenario))
                _menu.AddMenuListItem("Event", "Play mass scenario", _menu.Submenu<MassScenarioMenu>().OpenMenu);

            if (Permission.CanDo(Ability.PocceRiot))
                _menu.AddMenuListItemAsync("Event", "Pocce riot", EventMenu.PocceRiot);

            if (Permission.CanDo(Ability.PocceRiotArmed))
                _menu.AddMenuListItemAsync("Event", "Pocce riot (armed)", EventMenu.PocceRiotArmed);

            if (Permission.CanDo(Ability.PedRiot))
                _menu.AddMenuListItemAsync("Event", "Ped riot", EventMenu.PedRiot);

            if (Permission.CanDo(Ability.PedRiotArmed))
                _menu.AddMenuListItemAsync("Event", "Ped riot (armed)", EventMenu.PedRiotArmed);
            #endregion

            #region Skin
            if (Permission.CanDo(Ability.IdentifySkins))
            {
                _menu.AddMenuListItem("Skin", "Detect nearby skins", _menu.Submenu<SkinMenu>().DetectSkins);
                _menu.AddMenuListItem("Skin", "Detect player skin", _menu.Submenu<SkinMenu>().DetectPlayerSkin);
            }

            if (Permission.CanDo(Ability.ChangeSkin))
            {
                _menu.AddMenuListItem("Skin", "Choose from last detect ↕", _menu.Submenu<SkinMenu>().ShowLastSkins);
                _menu.AddMenuListItem("Skin", "Choose from all ↕", _menu.Submenu<SkinMenu>().ShowAllSkins);
            }
            #endregion

            #region Upgrade
            if (Permission.CanDo(Ability.BackToTheFuture))
                _menu.AddMenuListItem("Upgrade", "Back to the Future (toggle)", Vehicles.ToggleBackToTheFuture);

            if (Permission.CanDo(Ability.UltrabrightHeadlight))
                _menu.AddMenuListItem("Upgrade", "Ultrabright headlight (toggle)", Vehicles.ToggleUltrabrightHeadlight);

            if (Permission.CanDo(Ability.CargobobMagnet))
                _menu.AddMenuListItem("Upgrade", "Cargobob magnet", Vehicles.CargobobMagnet);

            if (Permission.CanDo(Ability.CompressVehicle))
                _menu.AddMenuListItem("Upgrade", "Compress vehicle", Vehicles.CompressVehicle);

            if (Permission.CanDo(Ability.AntiGravity))
                _menu.AddMenuListItem("Upgrade", "Anti-gravity (toggle)", Vehicles.ToggleAntiGravity);

            if (Permission.CanDo(Ability.AircraftHorn))
                _menu.AddMenuListItem("Upgrade", "Aircraft horn ↕", _menu.Submenu<AircraftHornMenu>().OpenMenu);

            if (Permission.CanDo(Ability.TurboBoost))
                _menu.AddMenuListItem("Upgrade", "Turbo Boost (toggle)", Vehicles.ToggleTurboBoost);
            #endregion

            #region Extra
            if (Permission.CanDo(Ability.OceanWaves))
                _menu.AddMenuListItem("Extra", "Crazy ocean waves (toggle)", ExtraMenu.ToggleCrazyOceanWaves);

            if (Permission.CanDo(Ability.RappelFromHeli))
                _menu.AddMenuListItemAsync("Extra", "Rappel from heli", ExtraMenu.RappelFromHeli);

            if (Permission.CanDo(Ability.EMP))
                _menu.AddMenuListItemAsync("Extra", "EMP", Vehicles.EMP);

            if (Permission.CanDo(Ability.SpawnTrashPed))
                _menu.AddMenuListItemAsync("Extra", "Trash ped", ExtraMenu.SpawnTrashPed);
            
            if (Permission.CanDo(Ability.Balloons))
                _menu.AddMenuListItemAsync("Extra", "Balloons", ExtraMenu.Balloons);

            if (Permission.CanDo(Ability.FreezePosition))
                _menu.AddMenuListItem("Extra", "Freeze position (toggle)", ExtraMenu.FreezePosition);
            #endregion
        }

        private static void PocceCommand(int source, List<object> args, string raw)
        {
            if (_menu != null)
            {
                if (args.Count > 0 && args[0] is string && (string)args[0] == "debug")
                    _menu.Submenu<DebugMenu>().OpenMenu();
                else
                    _menu.OpenMenu();
            }
        }
    }
}
