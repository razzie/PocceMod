using CitizenFX.Core;
using PocceMod.Shared;
using System;

namespace PocceMod.Server
{
    public class Main : BaseScript
    {
        public Main()
        {
            EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDropped);
            EventHandlers["PocceMod:Burn"] += new Action<Player, int>(Burn);
            EventHandlers["PocceMod:EMP"] += new Action<Player, int>(EMP);
            EventHandlers["PocceMod:SetIndicator"] += new Action<Player, int, int>(SetIndicator);
        }

        private void PlayerDropped([FromSource] Player source, string reason)
        {
            Ropes.ClearRopes(source);
        }

        private void Burn([FromSource] Player source, int entity)
        {
            if (Permission.CanDo(source, Ability.SpawnTrashPed))
                TriggerClientEvent("PocceMod:Burn", entity);
        }

        private void EMP([FromSource] Player source, int vehicle)
        {
            if (Permission.CanDo(source, Ability.EMPOtherPlayer))
                TriggerClientEvent("PocceMod:EMP", vehicle);
        }

        private void SetIndicator([FromSource] Player source, int vehicle, int state)
        {
            TriggerClientEvent("PocceMod:SetIndicator", vehicle, state);
        }
    }
}
