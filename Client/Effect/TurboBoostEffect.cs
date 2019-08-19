using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class TurboBoostEffect : IEffect
    {
        private class SteamFX
        {
            public SteamFX(int vehicle, int bone)
            {
                var pos = API.GetWorldPositionOfEntityBone(vehicle, bone);
                Offset = API.GetOffsetFromEntityGivenWorldCoords(vehicle, pos.X, pos.Y, pos.Z);

                API.UseParticleFxAssetNextCall("core");
                Handle = API.StartParticleFxLoopedOnEntity("ent_amb_steam", vehicle, Offset.X, Offset.Y, Offset.Z, 90f, 0f, 0f, 1f, false, false, false);
            }

            public int Handle { get; }
            public Vector3 Offset { get; }

            public float Angle
            {
                set { API.SetParticleFxLoopedOffsets(Handle, Offset.X, Offset.Y, Offset.Z, 90f + value, 0f, 0f); }
            }
        }

        private static readonly string[] _wheelBoneNames;

        private readonly int _vehicle;
        private readonly int[] _wheelBones;
        private readonly float _offset;
        private SteamFX[] _effects;
        private int _angle = 0;
        
        static TurboBoostEffect()
        {
            _wheelBoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr", "wheel_lf", "wheel_rf", "wheelf" };
        }

        public TurboBoostEffect(int vehicle)
        {
            _vehicle = vehicle;
            _wheelBones = _wheelBoneNames.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel)).Where(bone => bone != -1).ToArray();

            var model = (uint)API.GetEntityModel(_vehicle);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            _offset = max.Y;
        }

        public string Key
        {
            get { return GetKeyFrom(_vehicle); }
        }

        public bool Expired
        {
            get { return !API.DoesEntityExist(_vehicle) || API.IsEntityDead(_vehicle); }
        }

        public async Task Init()
        {
            await Common.RequestPtfxAsset("core");
            _effects = _wheelBones.Select(bone => new SteamFX(_vehicle, bone)).ToArray();
        }

        public void Update()
        {
            if (_angle > 60)
                _angle = 60;
            
            foreach (var fx in _effects)
                fx.Angle = _angle;

            if (!API.IsEntityInAir(_vehicle))
                API.ApplyForceToEntity(_vehicle, 0, 0f, 0f, 20f, 0f, _offset, 0f, -1, true, true, true, false, true);

            var angleRad = _angle * (Math.PI / 180f);
            API.ApplyForceToEntityCenterOfMass(_vehicle, 0, 0f, 400f * (float)Math.Cos(angleRad), 400f * (float)Math.Sin(angleRad), false, true, true, false);

            _angle += 5;
        }

        public void Clear()
        {
            foreach (var fx in _effects)
                API.RemoveParticleFx(fx.Handle, false);
        }

        public static string GetKeyFrom(int vehicle)
        {
            return "turboboost_" + vehicle;
        }
    }
}
