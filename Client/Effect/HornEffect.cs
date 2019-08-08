using CitizenFX.Core.Native;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class HornEffect : IEffect
    {
        private readonly int _vehicle;
        private int _sound;

        public HornEffect(int vehicle)
        {
            _vehicle = vehicle;
        }

        public string Key
        {
            get { return GetKeyFrom(_vehicle); }
        }

        public bool Expired
        {
            get { return !API.DoesEntityExist(_vehicle) || API.IsEntityDead(_vehicle) || API.IsVehicleSeatFree(_vehicle, -1); }
        }

        public Task Init()
        {
            _sound = API.GetSoundId();
            API.PlaySoundFromEntity(_sound, Vehicles.GetAircraftHorn(_vehicle), _vehicle, null, false, 0);
            return Task.FromResult(0);
        }

        public void Clear()
        {
            API.StopSound(_sound);
            API.ReleaseSoundId(_sound);
        }

        public static string GetKeyFrom(int vehicle)
        {
            return "horn_" + vehicle;
        }
    }
}
