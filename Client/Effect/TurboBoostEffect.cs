using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class TurboBoostEffect : IEffect
    {
        private enum LaunchMode
        {
            Ground,
            Air
        }

        private static readonly string[] _wheelBoneNames;

        private readonly int _vehicle;
        private readonly LaunchMode _mode;
        private readonly int[] _wheelBones;
        private readonly float _offset;
        private readonly float _torque;
        private int[] _effects;
        private int _step = 0;
        
        static TurboBoostEffect()
        {
            _wheelBoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr", "wheel_lf", "wheel_rf", "wheelf" };
        }

        public TurboBoostEffect(int vehicle)
        {
            _vehicle = vehicle;
            _mode = API.IsEntityInAir(_vehicle) ? LaunchMode.Air : LaunchMode.Ground;
            _wheelBones = _wheelBoneNames.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel)).Where(bone => bone != -1).ToArray();

            var model = (uint)API.GetEntityModel(_vehicle);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            _offset = max.Y;

            if (API.IsThisModelACar(model))
                _torque = 10f;
            else
                _torque = 50f;
        }

        public string Key
        {
            get
            {
                return "turboboost_" + _vehicle;
            }
        }

        public bool Expired
        {
            get { return _step > 10; }
        }

        public async Task Init()
        {
            await Common.RequestPtfxAsset("core");

            _effects = _wheelBones.Select(bone =>
            {
                var pos = API.GetWorldPositionOfEntityBone(_vehicle, bone);
                var offset = API.GetOffsetFromEntityGivenWorldCoords(_vehicle, pos.X, pos.Y, pos.Z);
                API.UseParticleFxAssetNextCall("core");
                return API.StartParticleFxLoopedOnEntity("ent_amb_steam", _vehicle, offset.X, offset.Y, offset.Z, 150f, 0f, 0f, 1f, false, false, false);
            }).ToArray();
        }

        public void Update()
        {
            ++_step;

            if (_mode == LaunchMode.Ground)
            {
                if (_step <= 5)
                {
                    API.ApplyForceToEntityCenterOfMass(_vehicle, 0, 0f, _step * 100, 0f, false, true, true, false);
                }
                else if (_step > 5)
                {
                    API.ApplyForceToEntity(_vehicle, 0, 0f, 0f, (10 - _step) * _torque / _offset, 0f, _offset, 0f, -1, true, true, true, false, true);
                    API.ApplyForceToEntityCenterOfMass(_vehicle, 0, 0f, 400f, 200f, false, true, true, false);
                }
            }
            else
            {
                API.ApplyForceToEntityCenterOfMass(_vehicle, 0, 0f, 400f, 200f, false, true, true, false);
            }
        }

        public void Clear()
        {
            foreach (var fx in _effects)
                API.RemoveParticleFx(fx, false);
        }
    }
}
