using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PocceMod.Shared
{
    public static class Config
    {
        private static Dictionary<string, string> Configuration { get; } = new Dictionary<string, string>();
        public static uint[] WeaponList { get; }
        public static uint[] TrashPedList { get; }
        public static uint[] PocceList { get; }
        public static uint[] PetList { get; }
        public static string[] ScenarioList { get; }
        public static string[] VehicleList { get; }
        public static string[] PropList { get; }

        static Config()
        {
            var items = GetConfigList("config");
            foreach (var item in items)
            {
                if (item.StartsWith(";") || !item.Contains("="))
                    continue;

                var pair = item.Split('=');
                Configuration[pair[0].Trim()] = pair[1].Trim();
            }

            WeaponList = GetConfigModels("weapons");
            TrashPedList = GetConfigModels("trashpeds");
            PocceList = GetConfigModels("pocce");
            PetList = GetConfigModels("pets");
            ScenarioList = GetConfigList("scenarios");
            VehicleList = GetConfigList("vehicles");
            PropList = GetConfigList("props");
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

        private static string[] GetConfigList(string cfg)
        {
            var original = API.LoadResourceFile(API.GetCurrentResourceName(), "config/" + cfg + ".ini");
            var custom = API.LoadResourceFile(API.GetCurrentResourceName(), "config/" + cfg + ".custom.ini");

            var origList = original?.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var customList = custom?.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (customList?.Length > 0)
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
