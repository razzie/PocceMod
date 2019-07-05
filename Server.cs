using CitizenFX.Core;
using System;

namespace PocceMod.Server
{
    public class Server : BaseScript
    {
        public Server()
        {
            EventHandlers["PocceMod:AddRope"] += new Action<int, int, int>((player, entity1, entity2) =>
            {
                TriggerClientEvent("PocceMod:AddRope", player, entity1, entity2);
            });

            EventHandlers["PocceMod:ClearRopes"] += new Action<int>((player) =>
            {
                TriggerClientEvent("PocceMod:ClearRopes", player);
            });
        }
    }
}
