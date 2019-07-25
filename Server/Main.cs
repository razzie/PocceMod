using CitizenFX.Core;
using PocceMod.Shared;
using System;

namespace PocceMod.Server
{
    public class Main : BaseScript
    {
        private readonly RopeSet _ropes = new RopeSet();

        public Main()
        {
            EventHandlers["playerDropped"] += new Action<Player, string>(PlayerDropped);
            EventHandlers["PocceMod:Burn"] += new Action<Player, int>(Burn);
            EventHandlers["PocceMod:EMP"] += new Action<Player, int>(EMP);
            EventHandlers["PocceMod:SetIndicator"] += new Action<Player, int, int>(SetIndicator);
            EventHandlers["PocceMod:AddRope"] += new Action<Player, int, int, Vector3, Vector3, int>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<Player>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<Player>(ClearLastRope);
            EventHandlers["PocceMod:ClearEntityRopes"] += new Action<Player, int>(ClearEntityRopes);
            EventHandlers["PocceMod:RequestRopes"] += new Action<Player>(RequestRopes);
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

        private void SetIndicator([FromSource] Player source, int vehicle, int state)
        {
            TriggerClientEvent("PocceMod:SetIndicator", vehicle, state);
        }

        private void AddRope([FromSource] Player source, int entity1, int entity2, Vector3 offset1, Vector3 offset2, int mode)
        {
            if (Permission.CanDo(source, Ability.Rope) || Permission.CanDo(source, Ability.RopeGun))
            {
                TriggerClientEvent("PocceMod:AddRope", source.Handle, entity1, entity2, offset1, offset2, (int)mode);

                var rope = new Rope(source, entity1, entity2, offset1, offset2, (Rope.ModeFlag)mode);
                _ropes.AddRope(rope);
            }
        }

        private void ClearRopes([FromSource] Player source)
        {
            TriggerClientEvent("PocceMod:ClearRopes", source.Handle);
            _ropes.ClearRopes(source);
        }

        private void ClearLastRope([FromSource] Player source)
        {
            TriggerClientEvent("PocceMod:ClearLastRope", source.Handle);
            _ropes.ClearLastRope(source);
        }

        private void ClearEntityRopes([FromSource] Player source, int entity)
        {
            TriggerClientEvent("PocceMod:ClearEntityRopes", entity);
            _ropes.ClearEntityRopes(entity);
        }

        private void RequestRopes([FromSource] Player source)
        {
            foreach (var rope in _ropes.GetRopes())
            {
                rope.Player.TriggerEvent("PocceMod:AddRope", rope.Player.Handle, rope.Entity1, rope.Entity2, rope.Offset1, rope.Offset2, rope.Mode);
            }
        }
    }
}
