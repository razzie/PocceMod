using CitizenFX.Core.Native;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class JesusEffect : IEffect
    {
        private const string Platform = "p_oil_slick_01";
        private static readonly string[] WheelBoneNames;

        private readonly int _vehicle;
        private readonly float _minZ;
        private readonly int[] _wheelBones;
        private readonly int[] _platforms;

        static JesusEffect()
        {
            WheelBoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr", "wheel_lf", "wheel_rf", "wheelf" };
        }

        public JesusEffect(int vehicle)
        {
            _vehicle = vehicle;
            _wheelBones = WheelBoneNames.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel)).Where(bone => bone != -1).ToArray();
            _platforms = new int[_wheelBones.Length];
            Common.GetEntityMinMaxZ(_vehicle, out _minZ, out float _);
        }

        public string Key
        {
            get { return "Jesus_" + _vehicle; }
        }

        public bool Expired
        {
            get { return !API.DoesEntityExist(_vehicle) || !Vehicles.IsFeatureEnabled(_vehicle, Vehicles.FeatureFlag.JesusMode); }
        }

        public async Task Init()
        {
            var model = API.GetHashKey(Platform);
            await Common.RequestModel((uint)model);

            for (int i = 0; i < _platforms.Length; ++i)
            {
                int platform = API.CreateObject(model, 0f, 0f, 0f, false, false, false);
                API.SetEntityAlpha(platform, 0, 1);
                API.SetEntityDynamic(platform, false);
                _platforms[i] = platform;
            }
        }

        public void Update()
        {
            var coords = API.GetEntityCoords(_vehicle, false);
            float wheight = 0f;

            if (API.GetWaterHeight(coords.X, coords.Y, coords.Z, ref wheight) && coords.Z + _minZ - 1f < wheight)
            {
                var velocity = API.GetEntityVelocity(_vehicle);
                if (velocity.Z < 0f)
                {
                    if (velocity.Z < -1f)
                        velocity.Z /= 2;
                    else
                        velocity.Z = 0f;

                    API.SetEntityVelocity(_vehicle, velocity.X, velocity.Y, velocity.Z);
                }

                API.ApplyForceToEntityCenterOfMass(_vehicle, 1, 0f, 0f, 0.7f, false, false, true, false);
            }

            for (int i = 0; i < _platforms.Length; ++i)
            {
                var bone = _wheelBones[i];
                var boneCoords = API.GetWorldPositionOfEntityBone(_vehicle, bone);
                float boneWheight = 0f;

                if (API.GetWaterHeight(boneCoords.X, boneCoords.Y, boneCoords.Z, ref boneWheight))
                {
                    API.SetEntityCoords(_platforms[i], boneCoords.X, boneCoords.Y, boneWheight, true, true, true, false);
                }
                else
                {
                    API.SetEntityCoords(_platforms[i], 0f, 0f, 0f, true, true, true, false);
                }
            }
        }

        public void Clear()
        {
            foreach (int platform in _platforms)
            {
                int entity = platform;
                API.DeleteObject(ref entity);
            }
        }
    }
}
