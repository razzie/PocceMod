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
            EventHandlers["PocceMod:AddRope"] += new Action<Player, int, int, int>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<Player>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<Player>(ClearLastRope);
        }

        private void PlayerDropped([FromSource] Player source, string reason)
        {
            ClearRopes(source);
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

        private void AddRope([FromSource] Player source, int entity1, int entity2, int mode)
        {
            if (Permission.CanDo(source, Ability.Rope) || Permission.CanDo(source, Ability.RopeGun))
                TriggerClientEvent("PocceMod:AddRope", source.Handle, entity1, entity2, mode);
        }

        private void ClearRopes([FromSource] Player source)
        {
            TriggerClientEvent("PocceMod:ClearRopes", source.Handle);
        }

        private void ClearLastRope([FromSource] Player source)
        {
            TriggerClientEvent("PocceMod:ClearLastRope", source.Handle);
        }
    }
}
