using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    [MainMenuInclude]
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

        public static async Task SpawnByName()
        {
            var model = await Common.GetUserInput("Spawn prop by name", "", 30);
            if (string.IsNullOrEmpty(model))
                return;

            await Props.Spawn(model);
        }
    }
}
