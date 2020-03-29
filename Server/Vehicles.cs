using CitizenFX.Core;
using PocceMod.Shared;
using System;

namespace PocceMod.Server.Server
{
    public class Vehicles : BaseScript
    {
        public Vehicles()
        {
            EventHandlers["PocceMod:EMP"] += new Action<Player, int>(EMP);
            EventHandlers["PocceMod:CompressVehicle"] += new Action<Player, int>(CompressVehicle);
            EventHandlers["PocceMod:SetIndicator"] += new Action<Player, int, int>(SetIndicator);
            EventHandlers["PocceMod:ToggleHorn"] += new Action<Player, int, bool>(ToggleHorn);
            EventHandlers["PocceMod:ToggleTurboBoost"] += new Action<Player, int, bool, int>(ToggleTurboBoost);
            EventHandlers["PocceMod:ToggleTurboBrake"] += new Action<Player, int, bool>(ToggleTurboBrake);
        }

        private void EMP([FromSource] Player source, int vehicle)
        {
            if (Permission.CanDo(source, Ability.EMPOtherPlayer))
                TriggerClientEvent("PocceMod:EMP", vehicle);
        }

        private void CompressVehicle([FromSource] Player source, int vehicle)
        {
            if (Permission.CanDo(source, Ability.CompressVehicle))
                TriggerClientEvent("PocceMod:CompressVehicle", vehicle);
        }

        private void SetIndicator([FromSource] Player source, int vehicle, int state)
        {
            TriggerClientEvent("PocceMod:SetIndicator", vehicle, state);
        }

        private void ToggleHorn([FromSource] Player source, int vehicle, bool state)
        {
            if (Permission.CanDo(source, Ability.CustomHorn))
                TriggerClientEvent("PocceMod:ToggleHorn", vehicle, state);
        }

        private void ToggleTurboBoost([FromSource] Player source, int vehicle, bool state, int mode)
        {
            if (Permission.CanDo(source, Ability.TurboBoost))
                TriggerClientEvent("PocceMod:ToggleTurboBoost", vehicle, state, mode);
        }

        private void ToggleTurboBrake([FromSource] Player source, int vehicle, bool state)
        {
            if (Permission.CanDo(source, Ability.TurboBrake))
                TriggerClientEvent("PocceMod:ToggleTurboBrake", vehicle, state);
        }
    }
}
