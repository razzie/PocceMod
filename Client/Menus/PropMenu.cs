using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Linq;

namespace PocceMod.Client.Menus
{
    public class PropMenu : Submenu
    {
        public PropMenu() : base("select prop", Props.Spawn, FilteredPropList(), 10)
        {
        }

        public static bool IsValidProp(string prop)
        {
            var model = (uint)API.GetHashKey(prop);
            if (!API.IsModelValid(model))
            {
                Debug.WriteLine("[PocceMod] invalid prop: " + prop);
                return false;
            }

            return true;
        }

        public static IEnumerable<string> FilteredPropList()
        {
            return Config.PropList.Where(prop => IsValidProp(prop));
        }
    }
}
