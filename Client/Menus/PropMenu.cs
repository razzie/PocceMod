using PocceMod.Shared;

namespace PocceMod.Client.Menus
{
    public class PropMenu : Submenu
    {
        public PropMenu() : base("select prop", Props.Spawn, Config.PropList, 10)
        {
        }
    }
}
