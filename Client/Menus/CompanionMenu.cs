using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    [MainMenuInclude]
    public class CompanionMenu : SkinSubmenu
    {
        public CompanionMenu() : base(Companions.Spawn, true)
        {
        }

        public static async Task PocceCompanion()
        {
            var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
            var ped = await Companions.SpawnHuman(pocce);
            API.SetPedAsNoLongerNeeded(ref ped);
        }

        public static async Task PetCompanion()
        {
            var pet = Config.PetList[API.GetRandomIntInRange(0, Config.PetList.Length)];
            var ped = await Companions.SpawnNonhuman(pet);
            API.SetPedAsNoLongerNeeded(ref ped);
        }

        public void CustomCompanion()
        {
            var source = (ParentMenu as MainMenu)?.Submenu<SkinMenu>()?.AllSkins;
            OpenMenu(source);
        }

        public static async Task CustomCompanionByName()
        {
            var model = await Common.GetUserInput("Spawn companion by model name", "", 30);
            if (string.IsNullOrEmpty(model))
                return;

            var hash = (uint)API.GetHashKey(model);
            var ped = await Companions.SpawnHuman(hash);
            API.SetPedAsNoLongerNeeded(ref ped);
        }

        public static async Task PoccePassengers()
        {
            if (!Common.EnsurePlayerIsInVehicle(out int player, out int vehicle))
                return;

            while (Vehicles.GetFreeSeat(vehicle, out int seat))
            {
                var pocce = Config.PocceList[API.GetRandomIntInRange(0, Config.PocceList.Length)];
                await Common.RequestModel(pocce);
                var ped = API.CreatePedInsideVehicle(vehicle, 26, pocce, seat, true, false);
                API.SetModelAsNoLongerNeeded(pocce);
                API.SetEntityAsNoLongerNeeded(ref ped);
            }
        }
    }
}
