using CitizenFX.Core.Native;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class JesusEffect : IEffect
    {
        private class Platform
        {
            private static int Model { get; }

            private readonly int _entity;
            private readonly int _bone;
            private readonly int _platform;

            static Platform()
            {
                Model = API.GetHashKey("p_oil_slick_01");
            }

            public Platform(int entity, int bone)
            {
                _entity = entity;
                _bone = bone;
                _platform = API.CreateObject(Model, 0f, 0f, 0f, false, false, false);
                API.SetEntityAlpha(_platform, 0, 1);
                API.SetEntityDynamic(_platform, false);
            }

            public static Task Init()
            {
                return Common.RequestModel((uint)Model);
            }

            public void Update()
            {
                var coords = (_bone == -1) ? API.GetEntityCoords(_entity, false) : API.GetWorldPositionOfEntityBone(_entity, _bone);
                float wheight = 0f;

                if (API.GetWaterHeight(coords.X, coords.Y, coords.Z, ref wheight))
                {
                    API.SetEntityCoords(_platform, coords.X, coords.Y, wheight, true, true, true, false);
                }
                else
                {
                    API.SetEntityCoords(_platform, 0f, 0f, 0f, true, true, true, false);
                }
            }

            public void Clear()
            {
                var platform = _platform;
                API.DeleteObject(ref platform);
            }
        }

        private static readonly string[] BoneNames;

        private readonly int _entity;
        private readonly float _minZ;
        private readonly int[] _bones;
        private readonly Platform[] _platforms;

        static JesusEffect()
        {
            BoneNames = new string[] { "wheel_lr", "wheel_rr", "wheelr", "wheel_lf", "wheel_rf", "wheelf" };
        }

        public JesusEffect(int entity)
        {
            _entity = entity;
            _bones = BoneNames.Select(bone => API.GetEntityBoneIndexByName(_entity, bone))
                .Where(bone => bone != -1)
                .Concat(new int[] { -1 })
                .ToArray();
            _platforms = new Platform[_bones.Length];
            Common.GetEntityMinMaxZ(_entity, out _minZ, out float _);
        }

        public string Key
        {
            get { return GetKeyFrom(_entity); }
        }

        public bool Expired
        {
            get
            {
                return !API.DoesEntityExist(_entity) ||
                    (API.IsEntityAVehicle(_entity) && !Vehicles.IsFeatureEnabled(_entity, Vehicles.FeatureFlag.JesusMode));
            }
        }

        public async Task Init()
        {
            await Platform.Init();

            for (int i = 0; i < _platforms.Length; ++i)
                _platforms[i] = new Platform(_entity, _bones[i]);
        }

        public void Update()
        {
            var coords = API.GetEntityCoords(_entity, false);
            float wheight = 0f;

            if (API.GetWaterHeight(coords.X, coords.Y, coords.Z, ref wheight) && coords.Z + _minZ - 1f < wheight)
            {
                var velocity = API.GetEntityVelocity(_entity);
                if (velocity.Z < 0f)
                {
                    if (velocity.Z < -1f)
                        velocity.Z /= 2;
                    else
                        velocity.Z = 0f;

                    API.SetEntityVelocity(_entity, velocity.X, velocity.Y, velocity.Z);
                }

                API.ApplyForceToEntityCenterOfMass(_entity, 1, 0f, 0f, 0.7f, false, false, true, false);
            }

            foreach (var platform in _platforms)
                platform.Update();
        }

        public void Clear()
        {
            foreach (var platform in _platforms)
                platform?.Clear();
        }

        public static string GetKeyFrom(int entity)
        {
            return "Jesus_" + entity;
        }
    }
}
