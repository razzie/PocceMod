using CitizenFX.Core.Native;
using System;

namespace PocceMod
{
    public static class Config
    {
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
            var content = API.LoadResourceFile(API.GetCurrentResourceName(), "config/" + cfg + ".ini");
            return content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static uint[] GetConfigModels(string cfg)
        {
            var list = GetConfigList(cfg);
            var models = new uint[list.Length];

            for (int i = 0; i < list.Length; ++i)
            {
                if (list[i].StartsWith("0x"))
                    models[i] = uint.Parse(list[i].Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    models[i] = (uint)API.GetHashKey(list[i]);
            }

            return models;
        }
    }
}
