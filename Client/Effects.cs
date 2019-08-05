using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Effects : BaseScript
    {
        public interface IEffect
        {
            int Key { get; }
            bool Expired { get; }
            void Update();
            void Clear();
        }

        private static readonly List<IEffect> _effects = new List<IEffect>();
        private static readonly Dictionary<int, IEffect> _unique = new Dictionary<int, IEffect>();

        public Effects()
        {
            Tick += Update;
        }

        public static bool Add(IEffect effect, bool unique = false)
        {
            if (unique)
            {
                if (_unique.ContainsKey(effect.Key))
                    return false;

                _unique.Add(effect.Key, effect);
            }

            _effects.Add(effect);
            return true;
        }

        public static async Task<bool> AddEMPEffect(int vehicle)
        {
            if (_unique.ContainsKey(vehicle))
                return false;

            await Common.RequestPtfxAsset("core");
            var effect = new EMPEffect(vehicle);
            return Add(effect, true);
        }

        public static bool AddWheelFireEffect(int vehicle)
        {
            var effect = new WheelFireEffect(vehicle);
            return Add(effect);
        }

        private static Task Update()
        {
            foreach (var effect in _effects.ToArray())
            {
                effect.Update();

                if (effect.Expired)
                {
                    _effects.Remove(effect);
                    _unique.Remove(effect.Key);
                    effect.Clear();
                }
            }

            return Delay(100);
        }

        private class EMPEffect : IEffect
        {
            private readonly int _vehicle;
            private readonly int _effect;

            public EMPEffect(int vehicle)
            {
                _vehicle = vehicle;

                API.UseParticleFxAssetNextCall("core");
                var engineBone = API.GetEntityBoneIndexByName(_vehicle, "engine");
                _effect = API.StartParticleFxLoopedOnEntityBone("ent_amb_elec_crackle", _vehicle, 0f, 0f, 0.1f, 0f, 0f, 0f, engineBone, 1f, false, false, false);
            }

            public int Key
            {
                get { return _vehicle; }
            }

            public bool Expired
            {
                get { return !API.DoesEntityExist(_vehicle) || API.GetVehicleEngineHealth(_vehicle) > 100f; }
            }

            public void Update()
            {
            }

            public void Clear()
            {
                Vehicles.SetState(_vehicle, Vehicles.StateFlag.EMP, false);
                API.StopParticleFxLooped(_effect, false);
                API.RemoveParticleFx(_effect, false);
            }
        }

        private class WheelFireEffect : IEffect
        {
            private readonly DateTime _expires;
            private readonly int[] _fires;
            private readonly int _key;

            public WheelFireEffect(int vehicle)
            {
                var wheelBones = new string[] { "wheel_lr", "wheel_rr", "wheelr" }.Select(wheel => API.GetEntityBoneIndexByName(vehicle, wheel));
                var fires = new List<int>();

                foreach (var wheel in wheelBones)
                {
                    var coords = API.GetWorldPositionOfEntityBone(vehicle, wheel);
                    fires.Add(API.StartScriptFire(coords.X, coords.Y, coords.Z, 3, true));
                }

                _expires = DateTime.Now + TimeSpan.FromSeconds(1);
                _fires = fires.ToArray();
                _key = vehicle;
            }

            public int Key
            {
                get { return _key; }
            }

            public bool Expired
            {
                get { return DateTime.Now > _expires; }
            }

            public void Update()
            {
            }

            public void Clear()
            {
                foreach (var fire in _fires)
                    API.RemoveScriptFire(fire);
            }
        }
    }
}
