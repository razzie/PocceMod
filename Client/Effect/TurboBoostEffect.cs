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

        private static readonly string[] WheelBoneNames;
        private static readonly float Power;
        private static readonly TimeSpan ChargeSec;
        private static readonly float RechargeRate;
        private static readonly int MaxAngle;
        private static readonly bool IsMappedToHorn;
        private static readonly Dictionary<int, DateTime> _rechargeDB = new Dictionary<int, DateTime>();

        private readonly int _vehicle;
        private readonly int[] _wheelBones;
        private readonly float _offset;
        private readonly DateTime _created;
        private DateTime _timeout;
        private SteamFX[] _effects;
        private int _angle = 0;
        
        static TurboBoostEffect()
        {
            WheelBoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr", "wheel_lf", "wheel_rf", "wheelf" };
            Power = Config.GetConfigFloat("TurboBoostPower");
            ChargeSec = TimeSpan.FromSeconds(Config.GetConfigFloat("TurboBoostChargeSec"));
            RechargeRate = Config.GetConfigFloat("TurboBoostRechargeRate");
            MaxAngle = Config.GetConfigInt("TurboBoostMaxAngle");
            IsMappedToHorn = Config.GetConfigInt("TurboBoostKey") == 86;
        }

        public TurboBoostEffect(int vehicle)
        {
            _vehicle = vehicle;
            _wheelBones = WheelBoneNames.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel)).Where(bone => bone != -1).ToArray();

            var model = (uint)API.GetEntityModel(_vehicle);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            _offset = max.Y;
            _created = DateTime.Now;
            _timeout = _created + ChargeSec;
        }

        public string Key
        {
            get { return GetKeyFrom(_vehicle); }
        }

        public bool Expired
        {
            get { return !API.DoesEntityExist(_vehicle) || API.IsEntityDead(_vehicle) || DateTime.Now > _timeout; }
        }

        public async Task Init()
        {
            if (_rechargeDB.TryGetValue(_vehicle, out DateTime nextCharge) && DateTime.Now < nextCharge)
            {
                _timeout = DateTime.MinValue;
                return;
            }

            if (IsMappedToHorn)
                API.SetHornEnabled(_vehicle, false);

            await Common.RequestPtfxAsset("core");
            _effects = _wheelBones.Select(bone => new SteamFX(_vehicle, bone)).ToArray();
        }

        public void Update()
        {
            if (_angle > MaxAngle)
                _angle = MaxAngle;
            
            foreach (var fx in _effects)
                fx.Angle = _angle;

            if (!API.IsEntityInAir(_vehicle))
                API.ApplyForceToEntity(_vehicle, 0, 0f, 0f, 20f, 0f, _offset, 0f, -1, true, true, true, false, true);

            var angleRad = _angle * (Math.PI / 180f);
            API.ApplyForceToEntityCenterOfMass(_vehicle, 0, 0f, Power * (float)Math.Cos(angleRad), Power * (float)Math.Sin(angleRad), false, true, true, false);

            _angle += 5;
        }

        public void Clear()
        {
            foreach (var fx in _effects ?? Enumerable.Empty<SteamFX>())
                API.RemoveParticleFx(fx.Handle, false);

            if (IsMappedToHorn)
                API.SetHornEnabled(_vehicle, true);

            var now = DateTime.Now;
            if (_rechargeDB.TryGetValue(_vehicle, out DateTime nextCharge) && nextCharge > now)
                return;

            _rechargeDB[_vehicle] = now + TimeSpan.FromMilliseconds((now - _created).TotalMilliseconds / RechargeRate);
        }

        public static string GetKeyFrom(int vehicle)
        {
            return "turboboost_" + vehicle;
        }
    }
}
