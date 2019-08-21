using CitizenFX.Core;
using PocceMod.Shared;
using System;

namespace PocceMod.Server
{
    public class Main : BaseScript
    {
        private const bool _debug = false;
        private readonly RopeSet _ropes = new RopeSet();

        public Main()
        {
            EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDropped);
            EventHandlers["PocceMod:EMP"] += new Action<Player, int>(EMP);
            EventHandlers["PocceMod:CompressVehicle"] += new Action<Player, int>(CompressVehicle);
            EventHandlers["PocceMod:SetIndicator"] += new Action<Player, int, int>(SetIndicator);
            EventHandlers["PocceMod:ToggleHorn"] += new Action<Player, int, bool>(ToggleHorn);
            EventHandlers["PocceMod:AddRope"] += new Action<Player, int, int, Vector3, Vector3, int>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<Player>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<Player>(ClearLastRope);
            EventHandlers["PocceMod:ClearEntityRopes"] += new Action<Player, int>(ClearEntityRopes);
            EventHandlers["PocceMod:RequestRopes"] += new Action<Player>(RequestRopes);
            EventHandlers["PocceMod:AntiGravityAdd"] += new Action<Player, int, float>(AntiGravityAdd);
            EventHandlers["PocceMod:AntiGravityRemove"] += new Action<Player, int>(AntiGravityRemove);
            EventHandlers["PocceMod:RequestMPSkin"] += new Action<Player, int, int>(RequestMPSkin);
            EventHandlers["PocceMod:SetMPSkin"] += new Action<Player, int, dynamic, int>(SetMPSkin);
            EventHandlers["PocceMod:ToggleTurboBoost"] += new Action<Player, int, bool>(ToggleTurboBoost);
            EventHandlers["PocceMod:Telemetry"] += new Action<Player, dynamic>(Telemetry);
        }

        private static void Debug(Player source, string text)
        {
#pragma warning disable CS0162
            if (_debug)
                CitizenFX.Core.Debug.WriteLine(string.Format("[player#{0}] {1}", source.Handle, text));
#pragma warning restore CS0162
        }

        private void PlayerDropped([FromSource] Player source, string reason)
        {
            Debug(source, "PlayerDropped");

            ClearRopes(source);
        }

        private void EMP([FromSource] Player source, int vehicle)
        {
            Debug(source, "EMP");

            if (Permission.CanDo(source, Ability.EMPOtherPlayer))
                TriggerClientEvent("PocceMod:EMP", vehicle);
        }

        private void CompressVehicle([FromSource] Player source, int vehicle)
        {
            Debug(source, "CompressVehicle");

            if (Permission.CanDo(source, Ability.CompressVehicle))
                TriggerClientEvent("PocceMod:CompressVehicle", vehicle);
        }

        private void SetIndicator([FromSource] Player source, int vehicle, int state)
        {
            Debug(source, "SetIndicator");

            TriggerClientEvent("PocceMod:SetIndicator", vehicle, state);
        }

        private void ToggleHorn([FromSource] Player source, int vehicle, bool state)
        {
            Debug(source, "ToggleHorn");

            if (Permission.CanDo(source, Ability.AircraftHorn))
                TriggerClientEvent("PocceMod:ToggleHorn", vehicle, state);
        }

        private void AddRope([FromSource] Player source, int entity1, int entity2, Vector3 offset1, Vector3 offset2, int mode)
        {
            Debug(source, "AddRope");

            if (Permission.CanDo(source, Ability.Rope) || Permission.CanDo(source, Ability.RopeGun) || Permission.CanDo(source, Ability.Balloons))
            {
                TriggerClientEvent("PocceMod:AddRope", source.Handle, entity1, entity2, offset1, offset2, (int)mode);

                var rope = new Rope(source, entity1, entity2, offset1, offset2, (Rope.ModeFlag)mode);
                _ropes.AddRope(rope);
            }
        }

        private void ClearRopes([FromSource] Player source)
        {
            Debug(source, "ClearRopes");

            TriggerClientEvent("PocceMod:ClearRopes", source.Handle);
            _ropes.ClearRopes(source);
        }

        private void ClearLastRope([FromSource] Player source)
        {
            Debug(source, "ClearLastRope");

            TriggerClientEvent("PocceMod:ClearLastRope", source.Handle);
            _ropes.ClearLastRope(source);
        }

        private void ClearEntityRopes([FromSource] Player source, int entity)
        {
            Debug(source, "ClearEntityRopes");

            TriggerClientEvent("PocceMod:ClearEntityRopes", entity);
            _ropes.ClearEntityRopes(entity);
        }

        private void RequestRopes([FromSource] Player source)
        {
            Debug(source, "RequestRopes");

            foreach (var rope in _ropes.GetRopes())
            {
                rope.Player.TriggerEvent("PocceMod:AddRope", rope.Player.Handle, rope.Entity1, rope.Entity2, rope.Offset1, rope.Offset2, rope.Mode);
            }
        }

        private void AntiGravityAdd([FromSource] Player source, int entity, float force)
        {
            Debug(source, "AntiGravityAdd");

            if (Permission.CanDo(source, Ability.AntiGravity) || Permission.CanDo(source, Ability.Balloons))
                TriggerClientEvent("PocceMod:AntiGravityAdd", entity, force);
        }

        private void AntiGravityRemove([FromSource] Player source, int entity)
        {
            Debug(source, "AntiGravityRemove");

            if (Permission.CanDo(source, Ability.AntiGravity) || Permission.CanDo(source, Ability.Balloons))
                TriggerClientEvent("PocceMod:AntiGravityRemove", entity);
        }

        private void RequestMPSkin([FromSource] Player source, int ped, int ownerPlayer)
        {
            Debug(source, "RequestMPSkin");

            Players[ownerPlayer].TriggerEvent("PocceMod:RequestMPSkin", ped, source.Handle);
        }

        private void SetMPSkin([FromSource] Player source, int ped, dynamic skin, int requestingPlayer)
        {
            Debug(source, "SetMPSkin");

            Players[requestingPlayer].TriggerEvent("PocceMod:SetMPSkin", ped, skin);
        }

        private void ToggleTurboBoost([FromSource] Player source, int vehicle, bool state)
        {
            Debug(source, "TurboBoost");

            if (Permission.CanDo(source, Ability.TurboBoost))
                TriggerClientEvent("PocceMod:ToggleTurboBoost", vehicle, state);
        }

        private void Telemetry([FromSource] Player source, dynamic data)
        {
            Debug(source, "Telemetry");

            foreach (var player in Players)
            {
                if (player == source || Permission.CanDo(player, Ability.ReceiveTelemetry))
                    player.TriggerEvent("PocceMod:Telemetry", source.Handle, data);
            }
        }
    }
}
