using CitizenFX.Core;
using System;

namespace PocceMod.Server
{
    public class Server : BaseScript
    {
        public Server()
        {
            EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDropped);

            EventHandlers["PocceMod:Burn"] += new Action<int>(entity =>
            {
                TriggerClientEvent("PocceMod:Burn", entity);
            });

            EventHandlers["PocceMod:EMP"] += new Action<int>(entity =>
            {
                TriggerClientEvent("PocceMod:EMP", entity);
            });

            EventHandlers["PocceMod:AddRope"] += new Action<int, int, int>((player, entity1, entity2) =>
            {
                TriggerClientEvent("PocceMod:AddRope", player, entity1, entity2);
            });

            EventHandlers["PocceMod:ClearRopes"] += new Action<int>(player =>
            {
                TriggerClientEvent("PocceMod:ClearRopes", player);
            });
        }

        private void PlayerDropped([FromSource] Player source, string reason)
        {
            TriggerClientEvent("PocceMod:ClearRopes", source.Handle);
        }
    }
}
