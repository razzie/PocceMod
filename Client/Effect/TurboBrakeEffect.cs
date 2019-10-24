using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class TurboBrakeEffect : IEffect
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

            public Vector3 Angle
            {
                set { API.SetParticleFxLoopedOffsets(Handle, Offset.X, Offset.Y, Offset.Z, 90f + value.X, value.Y, value.Z); }
            }
        }

        private static readonly string[] WheelBoneNames;
        private static readonly float Power;
        public static readonly float MinSpeed;

        private readonly int _vehicle;
        private readonly int[] _wheelBones;
        private SteamFX[] _effects;

        static TurboBrakeEffect()
        {
            WheelBoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr", "wheel_lf", "wheel_rf", "wheelf" };
            Power = Config.GetConfigFloat("TurboBrakePower");
            MinSpeed = Config.GetConfigFloat("TurboBrakeMinSpeed");

            if (Power < 100f)
                Power = 100f;
        }

        public TurboBrakeEffect(int vehicle)
        {
            _vehicle = vehicle;
            _wheelBones = WheelBoneNames.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel)).Where(bone => bone != -1).ToArray();
        }

        public string Key
        {
            get { return GetKeyFrom(_vehicle); }
        }

        public bool Expired
        {
            get { return !API.DoesEntityExist(_vehicle) || API.IsEntityDead(_vehicle) || API.GetEntitySpeed(_vehicle) < MinSpeed; }
        }

        public async Task Init()
        {
            var angle = Common.DirectionToRotation(API.GetEntitySpeedVector(_vehicle, true));
            await Common.RequestPtfxAsset("core");
            _effects = _wheelBones.Select(bone => new SteamFX(_vehicle, bone) { Angle = angle }).ToArray();
        }

        public void Update()
        {
            var speed = API.GetEntitySpeedVector(_vehicle, true);
            var angle = Common.DirectionToRotation(speed);
            speed.Normalize();
            speed *= Power;

            foreach (var fx in _effects)
                fx.Angle = angle;

            API.ApplyForceToEntityCenterOfMass(_vehicle, 0, -speed.X, -speed.Y, -speed.Z, false, true, true, false);
        }

        public void Clear()
        {
            foreach (var fx in _effects)
                API.RemoveParticleFx(fx.Handle, false);
        }

        public static string GetKeyFrom(int vehicle)
        {
            return "turbobrake_" + vehicle;
        }
    }
}
