using CitizenFX.Core.Native;
using PocceMod.Shared;

namespace PocceMod.Client.Menus
{
    [MainMenuInclude]
    public class SkinMenu : SkinSubmenu
    {
        public SkinMenu() : base(Skin.ChangePlayerSkin)
        {
            foreach (var pocce in Config.PocceList)
            {
                AllSkins.Add(Skin.ModelToName(pocce));
            }
        }

        public SkinSet AllSkins { get; } = new SkinSet();
        public SkinSet LastSkins { get; } = new SkinSet();

        public void ShowAllSkins()
        {
            if (Permission.CanDo(Ability.ChangeSkin))
                OpenMenu(AllSkins);
        }

        public void ShowLastSkins()
        {
            if (Permission.CanDo(Ability.ChangeSkin))
                OpenMenu(LastSkins);
        }

        public void DetectSkins()
        {
            LastSkins.Clear();
            var peds = Peds.Get(Peds.DefaultFilters, 4f);

            foreach (var ped in peds)
            {
                var skin = new Skin(ped);
                LastSkins.Add(skin);
                AllSkins.Add(skin);
                ShowNotification(skin);
            }

            if (LastSkins.Count > 0)
                ShowLastSkins();
        }

        public void DetectPlayerSkin()
        {
            LastSkins.Clear();

            var skin = new Skin(API.GetPlayerPed(-1));
            LastSkins.Add(skin);
            AllSkins.Add(skin);
            ShowNotification(skin);

            if (LastSkins.Count > 0)
                ShowLastSkins();
        }

        private static void ShowNotification(Skin skin)
        {
            if (skin.IsMultiplayer)
            {
                var mpSkin = skin.MultiplayerSkin;
                Common.Notification(string.Format("model: {0} (father: {1}, mother: {2})", skin.Name, mpSkin.Father, mpSkin.Mother));
            }
            else
            {
                Common.Notification("model: " + skin.Name);
            }
        }
    }
}
