using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    [MainMenuInclude]
    public class MassScenarioMenu : Submenu
    {
        public MassScenarioMenu() : base("select scenario", PlayMassScenario, Config.ScenarioList)
        {
        }

        public static Task PlayMassScenario(string scenario)
        {
            var peds = Peds.Get(Peds.Filter.Dead | Peds.Filter.Players | Peds.Filter.Animals | Peds.Filter.VehiclePassengers);
            foreach (var ped in peds)
            {
                API.TaskStartScenarioInPlace(ped, scenario, 0, true);
            }

            return Task.FromResult(0);
        }
    }
}
