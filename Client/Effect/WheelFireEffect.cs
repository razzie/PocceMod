using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Effect
{
    public class WheelFireEffect : IEffect
    {
        private readonly int _vehicle;
        private readonly DateTime _expires;
        private readonly List<int> _fires;

        public WheelFireEffect(int vehicle)
        {
            _vehicle = vehicle;
            _expires = DateTime.Now + TimeSpan.FromSeconds(1);
            _fires = new List<int>();
        }

        public string Key
        {
            get { return "wheel_fire_" + _vehicle; }
        }

        public bool Expired
        {
            get { return DateTime.Now > _expires; }
        }

        public Task Init()
        {
            var wheelBones = new string[] { "wheel_lr", "wheel_rr", "wheelr" }.Select(wheel => API.GetEntityBoneIndexByName(_vehicle, wheel));

            foreach (var wheel in wheelBones)
            {
                var coords = API.GetWorldPositionOfEntityBone(_vehicle, wheel);
                _fires.Add(API.StartScriptFire(coords.X, coords.Y, coords.Z, 1, true));
            }

            return Task.FromResult(0);
        }

        public void Clear()
        {
            foreach (var fire in _fires)
                API.RemoveScriptFire(fire);
        }
    }
}
