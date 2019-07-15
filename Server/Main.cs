using CitizenFX.Core;
using System;

namespace PocceMod.Server
{
    public class Main : BaseScript
    {
        public Main()
        {
            EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDropped);
            EventHandlers["PocceMod:Burn"] += new Action<int>(Burn);
            EventHandlers["PocceMod:EMP"] += new Action<int>(EMP);
            EventHandlers["PocceMod:AddRope"] += new Action<Player, int, int, bool>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<Player>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<Player>(ClearLastRope);
        }

        private void PlayerDropped([FromSource] Player source, string reason)
        {
            ClearRopes(source);
        }

        private void Burn(int entity)
        {
            TriggerClientEvent("PocceMod:Burn", entity);
        }

        private void EMP(int entity)
        {
            TriggerClientEvent("PocceMod:EMP", entity);
        }

        private void AddRope([FromSource] Player source, int entity1, int entity2, bool tow)
        {
            TriggerClientEvent("PocceMod:AddRope", source.Handle, entity1, entity2, tow);
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
