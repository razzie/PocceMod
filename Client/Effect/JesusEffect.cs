using CitizenFX.Core.Native;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class JesusEffect : IEffect
    {
        private readonly int _vehicle;
        private readonly float _minZ;

        public JesusEffect(int vehicle)
        {
            _vehicle = vehicle;
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

        public Task Init()
        {
            return Task.FromResult(0);
        }

        public void Update()
        {
            var coords = API.GetEntityCoords(_vehicle, false);
            float wheight = 0f;

            if (API.GetWaterHeight(coords.X, coords.Y, coords.Z, ref wheight) && coords.Z + _minZ - 1f < wheight)
            {
                var velocity = API.GetEntityVelocity(_vehicle);
                if (velocity.Z < 0f)
                    API.SetEntityVelocity(_vehicle, velocity.X, velocity.Y, 0f);

                API.ApplyForceToEntityCenterOfMass(_vehicle, 1, 0f, 0f, 0.7f, false, false, true, false);
            }
        }

        public void Clear()
        {
        }
    }
}
