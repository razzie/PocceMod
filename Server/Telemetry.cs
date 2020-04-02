using CitizenFX.Core;
using PocceMod.Shared;
using System;

namespace PocceMod.Server.Server
{
    public class Telemetry : BaseScript
    {
        public Telemetry()
        {
            EventHandlers["PocceMod:RequestTelemetry"] += new Action<Player, int>(RequestTelemetry);
            EventHandlers["PocceMod:Telemetry"] += new Action<Player, int, dynamic>(SendTelemetry);
        }

        private void RequestTelemetry([FromSource] Player source, int timeoutSec)
        {
            if (Permission.CanDo(source, Ability.ReceiveTelemetry))
                TriggerEvent("PocceMod:RequestTelemetry", source.Handle, timeoutSec);
        }

        private void SendTelemetry([FromSource] Player source, int targetPlayer, dynamic data)
        {
            Players[targetPlayer].TriggerEvent("PocceMod:Telemetry", source.Handle, data);
        }
    }
}
