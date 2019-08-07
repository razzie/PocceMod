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
            string Key { get; }
            bool Expired { get; }
            Task Init();
            void Clear();
        }

        private static readonly List<IEffect> _effects = new List<IEffect>();

        public Effects()
        {
            Tick += Update;
        }

        public static async Task<bool> Add(IEffect effect, bool unique = false)
        {
            if (unique && _effects.Any(e => e.Key == effect.Key))
                return false;

            _effects.Add(effect);
            await effect.Init();
            return true;
        }

        public static void Remove(string key)
        {
            foreach (var effect in _effects.Where(e => e.Key == key).ToArray())
            {
                _effects.Remove(effect);
                effect.Clear();
            }
        }

        public static Task<bool> AddEMPEffect(int vehicle) => Add(new EMPEffect(vehicle), true);

        public static Task<bool> AddWheelFireEffect(int vehicle) => Add(new WheelFireEffect(vehicle));

        public static Task<bool> AddHornEffect(int vehicle) => Add(new HornEffect(vehicle), true);

        private static Task Update()
        {
            foreach (var effect in _effects.ToArray())
            {
                if (effect.Expired)
                {
                    _effects.Remove(effect);
                    effect.Clear();
                }
            }

            return Delay(100);
        }

        private class EMPEffect : IEffect
        {
            private readonly int _vehicle;
            private int _effect;

            public EMPEffect(int vehicle)
            {
                _vehicle = vehicle;
            }

            public string Key
            {
                get { return "emp_" + _vehicle; }
            }

            public bool Expired
            {
                get { return !API.DoesEntityExist(_vehicle) || API.GetVehicleEngineHealth(_vehicle) > 100f; }
            }

            public async Task Init()
            {
                await Common.RequestPtfxAsset("core");
                API.UseParticleFxAssetNextCall("core");

                var engineBone = API.GetEntityBoneIndexByName(_vehicle, "engine");
                _effect = API.StartParticleFxLoopedOnEntityBone("ent_amb_elec_crackle", _vehicle, 0f, 0f, 0.1f, 0f, 0f, 0f, engineBone, 1f, false, false, false);
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
            private readonly int _vehicle;
            private readonly DateTime _expires;
            private readonly List<int> _fires;

            public WheelFireEffect(int vehicle)
            {
                _vehicle = vehicle;
                _expires = DateTime.Now + TimeSpan.FromSeconds(1);
                _fires = new List<int>();
            }

            public string Key
            {
                get { return "wheel_fire_" + _vehicle; }
            }

            public bool Expired
            {
                get { return DateTime.Now > _expires; }
            }

            public Task Init()
            {
                var wheelBones = new string[] { "wheel_lr", "wheel_rr", "wheelr" }.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel));

                foreach (var wheel in wheelBones)
                {
                    var coords = API.GetWorldPositionOfEntityBone(_vehicle, wheel);
                    _fires.Add(API.StartScriptFire(coords.X, coords.Y, coords.Z, 1, true));
                }

                return Task.FromResult(0);
            }

            public void Clear()
            {
                foreach (var fire in _fires)
                    API.RemoveScriptFire(fire);
            }
        }

        private class HornEffect : IEffect
        {
            private readonly int _vehicle;
            private int _sound;

            public HornEffect(int vehicle)
            {
                _vehicle = vehicle;
            }

            public string Key
            {
                get { return "horn_" + _vehicle; }
            }

            public bool Expired
            {
                get { return !API.DoesEntityExist(_vehicle) || API.IsEntityDead(_vehicle) || API.IsVehicleSeatFree(_vehicle, -1); }
            }

            public Task Init()
            {
                _sound = API.GetSoundId();
                API.PlaySoundFromEntity(_sound, "SIRENS_AIRHORN", _vehicle, null, false, 0);
                return Task.FromResult(0);
            }

            public void Clear()
            {
                API.StopSound(_sound);
                API.ReleaseSoundId(_sound);
            }
        }
    }
}
