using CitizenFX.Core;
using System;

namespace PocceMod.Server.Server
{
    public class Peds : BaseScript
    {
        public Peds()
        {
            EventHandlers["PocceMod:RequestMPSkin"] += new Action<Player, int, int>(RequestMPSkin);
            EventHandlers["PocceMod:SetMPSkin"] += new Action<Player, int, dynamic, int>(SetMPSkin);
        }

        private void RequestMPSkin([FromSource] Player source, int ped, int ownerPlayer)
        {
            Players[ownerPlayer].TriggerEvent("PocceMod:RequestMPSkin", ped, source.Handle);
        }

        private void SetMPSkin([FromSource] Player source, int ped, dynamic skin, int requestingPlayer)
        {
            Players[requestingPlayer].TriggerEvent("PocceMod:SetMPSkin", ped, skin);
        }
    }
}
