using CitizenFX.Core.Native;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class EMPEffect : IEffect
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

        public void Update()
        {
        }

        public void Clear()
        {
            Vehicles.SetState(_vehicle, Vehicles.StateFlag.EMP, false);
            API.StopParticleFxLooped(_effect, false);
            API.RemoveParticleFx(_effect, false);
        }
    }
}
