using CitizenFX.Core;
using PocceMod.Client.Effect;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Effects : BaseScript
    {
        private static readonly List<IEffect> _effects = new List<IEffect>();

        public Effects()
        {
            Tick += Update;
        }

        public static async Task<bool> Add(IEffect effect, bool unique)
        {
            if (unique && _effects.Any(e => e.Key == effect.Key))
                return false;

            _effects.Add(effect);
            await effect.Init();
            return true;
        }

        public static void Remove(string key)
        {
            foreach (var effect in _effects.Where(e => e.Key == key).ToArray())
            {
                _effects.Remove(effect);
                effect.Clear();
            }
        }

        public static Task<bool> AddEMPEffect(int vehicle) => Add(new EMPEffect(vehicle), true);

        public static Task<bool> AddWheelFireEffect(int vehicle) => Add(new WheelFireEffect(vehicle), true);

        public static Task<bool> AddTurboBoostEffect(int vehicle) => Add(new TurboBoostEffect(vehicle), true);

        public static Task<bool> AddHornEffect(int vehicle) => Add(new HornEffect(vehicle), true);

        public static void RemoveHornEffect(int vehicle) => Remove(HornEffect.GetKeyFrom(vehicle));

        private static Task Update()
        {
            foreach (var effect in _effects.ToArray())
            {
                effect.Update();

                if (effect.Expired)
                {
                    _effects.Remove(effect);
                    effect.Clear();
                }
            }

            return Delay(100);
        }
    }
}
