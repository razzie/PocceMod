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
            string sound = Vehicles.GetCustomHorn(_vehicle);
            string soundset = null;

            if (sound.Contains(":"))
            {
                var parts = sound.Split(':');
                sound = parts[1];
                soundset = parts[0];
            }

            API.PlaySoundFromEntity(_sound, sound, _vehicle, soundset, false, 0);
            API.SetHornEnabled(_vehicle, false);
            return Task.FromResult(0);
        }

        public void Update()
        {
        }

        public void Clear()
        {
            API.StopSound(_sound);
            API.ReleaseSoundId(_sound);
            API.SetHornEnabled(_vehicle, true);
        }

        public static string GetKeyFrom(int vehicle)
        {
            return "horn_" + vehicle;
        }
    }
}
