using CitizenFX.Core.Native;
using MenuAPI;
using System;
using System.Collections.Generic;

namespace PocceMod.Client.Menus.Dev
{
    using FeatureFlag = Vehicles.FeatureFlag;
    using StateFlag = Vehicles.StateFlag;

    public class VehicleDebugMenu : Menu
    {
        private int _vehicle;
        private readonly Dictionary<FeatureFlag, MenuItem> _features = new Dictionary<FeatureFlag, MenuItem>();
        private readonly Dictionary<StateFlag, MenuItem> _states = new Dictionary<StateFlag, MenuItem>();

        public VehicleDebugMenu() : base("PocceMod", "vehicle debug menu")
        {
            foreach (var feature in (FeatureFlag[])Enum.GetValues(typeof(FeatureFlag)))
            {
                var menuItem = new MenuItem(feature.ToString()) { Enabled = false };
                _features.Add(feature, menuItem);
                AddMenuItem(menuItem);
            }

            foreach (var state in (StateFlag[])Enum.GetValues(typeof(StateFlag)))
            {
                var menuItem = new MenuItem(state.ToString()) { Enabled = false };
                _states.Add(state, menuItem);
                AddMenuItem(menuItem);
            }

            var hornMenuItem = new MenuItem("Custom horn") { Enabled = false };
            AddMenuItem(hornMenuItem);

            var lightMultiplierMenuItem = new MenuItem("Light multiplier") { Enabled = false };
            AddMenuItem(lightMultiplierMenuItem);

            OnMenuOpen += (menu) =>
            {
                _vehicle = Vehicle;
                Vehicles.FeatureChanged += OnFeatureChange;
                Vehicles.StateChanged += OnStateChange;

                foreach (var feature in (FeatureFlag[])Enum.GetValues(typeof(FeatureFlag)))
                {
                    _features[feature].Enabled = Vehicles.IsFeatureEnabled(_vehicle, feature);
                }

                if (Vehicles.HasCustomHorn(_vehicle))
                {
                    hornMenuItem.Enabled = true;
                    hornMenuItem.Label = Vehicles.GetCustomHorn(_vehicle);
                }

                var lightMultiplier = Vehicles.GetLightMultiplier(_vehicle);
                if (lightMultiplier > 1f)
                {
                    lightMultiplierMenuItem.Enabled = true;
                    lightMultiplierMenuItem.Label = lightMultiplier.ToString();
                }
            };

            OnMenuClose += (menu) =>
            {
                Vehicles.FeatureChanged -= OnFeatureChange;
                Vehicles.StateChanged -= OnStateChange;
                _vehicle = -1;

                foreach (var menuItem in _features.Values)
                    menuItem.Enabled = false;

                foreach (var menuItem in _states.Values)
                    menuItem.Enabled = false;

                hornMenuItem.Enabled = false;
                hornMenuItem.Label = string.Empty;

                lightMultiplierMenuItem.Enabled = false;
                lightMultiplierMenuItem.Label = string.Empty;
            };
        }

        private static int Vehicle
        {
            get
            {
                var player = API.GetPlayerPed(-1);
                return API.GetVehiclePedIsIn(player, !API.IsPedInAnyVehicle(player, false));
            }
        }

        private void OnFeatureChange(int vehicle, FeatureFlag features)
        {
            if (vehicle != _vehicle)
                return;

            foreach (var feature in (FeatureFlag[])Enum.GetValues(typeof(FeatureFlag)))
            {
                _features[feature].Enabled = (features & feature) == feature;
            }
        }

        private void OnStateChange(int vehicle, StateFlag states)
        {
            if (vehicle != _vehicle)
                return;

            foreach (var state in (StateFlag[])Enum.GetValues(typeof(StateFlag)))
            {
                _states[state].Enabled = (states & state) == state;
            }
        }
    }
}
