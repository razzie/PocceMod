using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class MosesEffect : IEffect
    {
        private readonly int _entity;
        private readonly float _radius;
        private readonly float _minZ;

        public MosesEffect(int entity)
        {
            _entity = entity;

            var model = (uint)API.GetEntityModel(_entity);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            _radius = AbsMax(min.X, max.X, min.Y, max.Y);
            _minZ = min.Z;
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
                    (API.IsEntityAVehicle(_entity) && !Vehicles.IsFeatureEnabled(_entity, Vehicles.FeatureFlag.MosesMode));
            }
        }

        public Task Init()
        {
            return Task.FromResult(0);
        }

        public void Update()
        {
            var coords = API.GetEntityCoords(_entity, false);
            float minZ = coords.Z + _minZ - 1f;
            float wheight = 0f;

            if (API.GetWaterHeight(coords.X, coords.Y, coords.Z, ref wheight) && minZ < wheight)
            {
                API.ModifyWater(coords.X, coords.Y, _radius, minZ);
            }
        }

        public void Clear()
        {
        }

        private static float AbsMax(params float[] values)
        {
            /*if (values.Length == 0)
                return 0;*/

            var max = Math.Abs(values[0]);

            for (int i = 1; i < values.Length; ++i)
            {
                var val = Math.Abs(values[i]);
                if (val > max)
                    max = val;
            }

            return max;
        }

        public static string GetKeyFrom(int entity)
        {
            return "Moses_" + entity;
        }
    }
}
