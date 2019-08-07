using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PocceMod.Shared
{
    public static class Config
    {
        private static Dictionary<string, string> _config;
        private static Dictionary<string, string> Configuration
        {
            get
            {
                if (_config == null)
                {
                    _config = new Dictionary<string, string>();
                    var items = GetConfigList("config");
                    foreach (var item in items)
                    {
                        if (item.StartsWith(";") || !item.Contains("="))
                            continue;

                        var pair = item.Split('=');
                        _config.Add(pair[0].Trim(), pair[1].Trim());
                    }
                }
                return _config;
            }
        }

        public static bool GetConfigString(string item, out string value)
        {
            var success = Configuration.TryGetValue(item, out value);

            if (!success)
                Debug.WriteLine("[PocceMod] missing config item: " + item);

            return success;
        }

        public static int GetConfigInt(string item)
        {
            if (GetConfigString(item, out string value) && int.TryParse(value, out int result))
                return result;
            else
                return 0;
        }

        public static bool GetConfigBool(string item)
        {
            if (GetConfigString(item, out string value) && bool.TryParse(value, out bool result))
                return result;
            else
                return false;
        }

        private static uint[] _weaponList = null;
        public static uint[] WeaponList
        {
            get
            {
                if (_weaponList == null)
                    _weaponList = GetConfigModels("weapons");
                return _weaponList;
            }
        }

        private static uint[] _trashPedList = null;
        public static uint[] TrashPedList
        {
            get
            {
                if (_trashPedList == null)
                    _trashPedList = GetConfigModels("trashpeds");
                return _trashPedList;
            }
        }

        private static uint[] _pocceList = null;
        public static uint[] PocceList
        {
            get
            {
                if (_pocceList == null)
                    _pocceList = GetConfigModels("pocce");
                return _pocceList;
            }
        }

        private static uint[] _petList = null;
        public static uint[] PetList
        {
            get
            {
                if (_petList == null)
                    _petList = GetConfigModels("pets");
                return _petList;
            }
        }

        private static string[] _scenarioList = null;
        public static string[] ScenarioList
        {
            get
            {
                if (_scenarioList == null)
                    _scenarioList = GetConfigList("scenarios");
                return _scenarioList;
            }
        }

        private static string[] _vehicleList = null;
        public static string[] VehicleList
        {
            get
            {
                if (_vehicleList == null)
                    _vehicleList = GetConfigList("vehicles");
                return _vehicleList;
            }
        }

        private static string[] _propList = null;
        public static string[] PropList
        {
            get
            {
                if (_propList == null)
                    _propList = GetConfigList("props");
                return _propList;
            }
        }

        private static string[] GetConfigList(string cfg)
        {
            var original = API.LoadResourceFile(API.GetCurrentResourceName(), "config/" + cfg + ".ini");
            var custom = API.LoadResourceFile(API.GetCurrentResourceName(), "config/" + cfg + ".custom.ini");

            var origList = original.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var customList = custom.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (customList.Length > 0)
                return origList.Concat(customList).ToArray();
            else
                return origList;
        }

        private static uint[] GetConfigModels(string cfg)
        {
            var list = GetConfigList(cfg);
            var models = new uint[list.Length];

            for (int i = 0; i < list.Length; ++i)
            {
                var model = list[i];
                if (model.StartsWith("0x"))
                    models[i] = uint.Parse(model.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    models[i] = (uint)API.GetHashKey(model);
            }

            return models;
        }
    }
}
