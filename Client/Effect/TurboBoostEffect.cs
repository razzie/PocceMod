using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class TurboBoostEffect : IEffect
    {
        public enum Mode
        {
            Custom,
            Horizontal,
            Vertical
        }

        private class SteamFX
        {
            public SteamFX(int vehicle, int bone)
            {
                var pos = API.GetWorldPositionOfEntityBone(vehicle, bone);
                Offset = API.GetOffsetFromEntityGivenWorldCoords(vehicle, pos.X, pos.Y, pos.Z);

                API.UseParticleFxAssetNextCall("core");
                Handle = API.StartParticleFxLoopedOnEntity("ent_amb_steam", vehicle, Offset.X, Offset.Y, Offset.Z, 90f + StartAngle, 0f, 0f, 1f, false, false, false);
            }

            public int Handle { get; }
            public Vector3 Offset { get; }

            public float Angle
            {
                set { API.SetParticleFxLoopedOffsets(Handle, Offset.X, Offset.Y, Offset.Z, 90f + value, 0f, 0f); }
            }
        }

        private static readonly string[] WheelBoneNames;
        private static readonly float Power;
        private static readonly TimeSpan ChargeSec;
        private static readonly float RechargeRate;
        private static readonly float StartAngle;
        private static readonly float EndAngle;
        private static readonly bool IsMappedToHorn;
        private static readonly Dictionary<int, DateTime> _rechargeDB = new Dictionary<int, DateTime>();

        private readonly int _vehicle;
        private readonly Mode _mode;
        private readonly int[] _wheelBones;
        private readonly float _offset;
        private readonly bool _blocked;
        private readonly DateTime _created;
        private readonly DateTime _timeout;
        private SteamFX[] _effects;
        private float _angle;
        private readonly float _angleStep;
        
        static TurboBoostEffect()
        {
            WheelBoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr", "wheel_lf", "wheel_rf", "wheelf" };
            Power = Config.GetConfigFloat("TurboBoostPower");
            ChargeSec = TimeSpan.FromSeconds(Config.GetConfigFloat("TurboBoostChargeSec"));
            RechargeRate = Config.GetConfigFloat("TurboBoostRechargeRate");
            StartAngle = Config.GetConfigFloat("TurboBoostStartAngle");
            EndAngle = Config.GetConfigFloat("TurboBoostEndAngle");
            IsMappedToHorn = Config.GetConfigInt("TurboBoostKey") == 86;

            if (Power < 100f)
                Power = 100f;

            if (ChargeSec.TotalMilliseconds < 1000)
                ChargeSec = TimeSpan.FromMilliseconds(1000);

            if (RechargeRate < 0.25f)
                RechargeRate = 0.25f;
        }

        public TurboBoostEffect(int vehicle, Mode mode)
        {
            _vehicle = vehicle;
            _mode = mode;
            _wheelBones = WheelBoneNames.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel)).Where(bone => bone != -1).ToArray();

            var model = (uint)API.GetEntityModel(_vehicle);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            _offset = max.Y;
            _created = DateTime.Now;
            _timeout = _created + ChargeSec;

            switch (_mode)
            {
                case Mode.Custom:
                    _angle = StartAngle;
                    _angleStep = (EndAngle - StartAngle) / ((int)ChargeSec.TotalMilliseconds / 200);
                    break;

                case Mode.Horizontal:
                    _angle = 0f;
                    _angleStep = 0f;
                    break;

                case Mode.Vertical:
                    _angle = 90f;
                    _angleStep = 0f;
                    break;
            }

            _blocked = _rechargeDB.TryGetValue(_vehicle, out DateTime nextCharge) && _created < nextCharge;
        }

        public string Key
        {
            get { return GetKeyFrom(_vehicle); }
        }

        public bool Expired
        {
            get { return _blocked || !API.DoesEntityExist(_vehicle) || API.IsEntityDead(_vehicle) || DateTime.Now > _timeout; }
        }

        public async Task Init()
        {
            if (_blocked)
                return;

            if (IsMappedToHorn)
                API.SetHornEnabled(_vehicle, false);

            await Common.RequestPtfxAsset("core");
            _effects = _wheelBones.Select(bone => new SteamFX(_vehicle, bone) { Angle = _angle }).ToArray();
        }

        public void Update()
        {
            if (_blocked)
                return;

            if (_angleStep > 0)
            {
                if (_angle > EndAngle)
                    _angle = EndAngle;
            }
            else if (_angleStep < 0)
            {
                if (_angle < EndAngle)
                    _angle = EndAngle;
            }
            
            foreach (var fx in _effects)
                fx.Angle = _angle;

            if (!API.IsEntityInAir(_vehicle))
                API.ApplyForceToEntity(_vehicle, 0, 0f, 0f, 20f, 0f, _offset, 0f, -1, true, true, true, false, true);

            var angleRad = _angle * (Math.PI / 180f);
            API.ApplyForceToEntityCenterOfMass(_vehicle, 0, 0f, Power * (float)Math.Cos(angleRad), Power * (float)Math.Sin(angleRad), false, true, true, false);

            _angle += _angleStep;
        }

        public void Clear()
        {
            if (_blocked)
                return;

            foreach (var fx in _effects)
                API.RemoveParticleFx(fx.Handle, false);

            if (IsMappedToHorn)
                API.SetHornEnabled(_vehicle, true);

            var now = DateTime.Now;
            _rechargeDB[_vehicle] = now + TimeSpan.FromMilliseconds((now - _created).TotalMilliseconds / RechargeRate);
        }

        public static string GetKeyFrom(int vehicle)
        {
            return "turboboost_" + vehicle;
        }
    }
}
