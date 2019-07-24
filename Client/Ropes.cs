using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static PocceMod.Shared.Rope;

namespace PocceMod.Client
{
    public class Ropes : BaseScript
    {
        private const uint Ropegun = 0x44AE7910; // WEAPON_POCCE_ROPEGUN
        private static readonly int RopegunWindKey;
        private static readonly int RopegunUndoKey;
        private static readonly RopeSet _ropes = new RopeSet();
        private static readonly RopegunState _ropegunState = new RopegunState();

        static Ropes()
        {
            RopegunWindKey = Config.GetConfigInt("RopegunWindKey");
            RopegunUndoKey = Config.GetConfigInt("RopegunUndoKey");
        }

        public Ropes()
        {
            EventHandlers["PocceMod:AddRope"] += new Func<int, int, int, Vector3, Vector3, int, Task>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<int>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<int>(ClearLastRope);

            TriggerServerEvent("PocceMod:RequestRopes");

            API.AddTextEntryByHash(0x6FCC4E8A, "Pocce Ropegun"); // WT_POCCE_ROPEGUN

            Tick += UpdateRopegun;
            Tick += UpdateRopes;
        }

        private static void AdjustOffsetForTowing(int entity, ref Vector3 offset, float towOffset)
        {
            if (!API.IsEntityAVehicle(entity))
                return;

            var right = Vector3.Zero;
            var forward = Vector3.Zero;
            var up = Vector3.Zero;
            var pos = Vector3.Zero;
            API.GetEntityMatrix(entity, ref right, ref forward, ref up, ref pos);

            var model = (uint)API.GetEntityModel(entity);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            if (towOffset > 0)
                right *= (max.X * towOffset);
            else
                right *= (-min.X * towOffset);
            
            offset = right;
        }

        private static bool IsOtherPlayerEntity(int entity)
        {
            var playerID = API.PlayerId();

            if (API.IsEntityAPed(entity))
            {
                return API.IsPedAPlayer(entity) && API.NetworkGetPlayerIndexFromPed(entity) != playerID;
            }
            else if (API.IsEntityAVehicle(entity))
            {
                var players = Vehicles.GetPlayers(entity);
                return players.Any(player => player != playerID);
            }

            return false;
        }

        private static async Task AddRope(int player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, int mode)
        {
            if (entity1 == entity2 && entity1 > 0)
                return;

            entity1 = await Common.WaitForNetEntity(entity1);
            entity2 = await Common.WaitForNetEntity(entity2);

            var rope = new ClientRope(new Player(player), entity1, entity2, offset1, offset2, (ModeFlag)mode);
            _ropes.AddRope(rope);

            if (!API.RopeAreTexturesLoaded())
                API.RopeLoadTextures();
        }

        private static void ClearRopes(int player)
        {
            _ropes.ClearRopes(new Player(player));
        }

        private static void ClearLastRope(int player)
        {
            _ropes.ClearLastRope(new Player(player));
        }

        public static void PlayerAttach(int entity, Vector3 offset, ModeFlag mode = ModeFlag.Normal)
        {
            Attach(Common.GetPlayerPedOrVehicle(), entity, Vector3.Zero, offset, mode);
        }

        public static void AttachToClosest(IEnumerable<int> entities, bool tow = false)
        {
            if (Common.GetClosestEntity(entities, out int closest))
                PlayerAttach(closest, Vector3.Zero, tow ? ModeFlag.Tow : ModeFlag.Normal);
            else
                Common.Notification("Nothing in range");
        }

        public static void Attach(int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode = ModeFlag.Normal)
        {
            if (!Permission.CanDo(Ability.RopeOtherPlayer) && (IsOtherPlayerEntity(entity1) || IsOtherPlayerEntity(entity2)))
            {
                Common.Notification("You are not allowed to attach rope to another player");
                return;
            }

            if ((mode & ModeFlag.Tow) == ModeFlag.Tow)
            {
                AdjustOffsetForTowing(entity1, ref offset1, -0.75f);
                AdjustOffsetForTowing(entity2, ref offset2, 0.75f);
            }

            if ((mode & ModeFlag.Ropegun) == ModeFlag.Ropegun)
            {
                _ropegunState.Update(ref entity1, ref entity2, ref offset1, ref offset2, out bool clearLast);
                if (clearLast)
                    ClearLast();
            }
            else
            {
                _ropegunState.Clear();
            }

            if (entity1 == entity2 && entity1 > 0)
                return;

            int ObjToNet(int entity)
            {
                if (entity == 0)
                    return 0;

                return API.ObjToNet(entity);
            }

            TriggerServerEvent("PocceMod:AddRope", ObjToNet(entity1), ObjToNet(entity2), offset1, offset2, (int)mode);
        }

        public static void ClearAll()
        {
            TriggerServerEvent("PocceMod:ClearRopes");
        }

        public static void ClearLast()
        {
            TriggerServerEvent("PocceMod:ClearLastRope");
        }

        public static void EquipRopeGun()
        {
            var player = API.GetPlayerPed(-1);
            Peds.GiveWeapon(player, Ropegun);
            API.SetCurrentPedVehicleWeapon(player, Ropegun);
        }

        private static bool TryShootRopegun(float distance, out Task<int> target, out Vector3 offset)
        {
            var player = Common.GetPlayerPedOrVehicle();
            Common.GetAimCoords(out Vector3 rayBegin, out Vector3 rayEnd, distance);
            var ray = API.CastRayPointToPoint(rayBegin.X, rayBegin.Y, rayBegin.Z, rayEnd.X, rayEnd.Y, rayEnd.Z, 1 | 2 | 4 | 8 | 16 | 32, player, 0);

            target = Task.FromResult(-1);
            offset = Vector3.Zero;

            bool hit = false;
            var coords = Vector3.Zero;
            var normal = Vector3.Zero;
            var entity = -1;
            API.GetRaycastResult(ray, ref hit, ref coords, ref normal, ref entity);

            if (hit)
            {
                switch (API.GetEntityType(entity))
                {
                    case 1:
                        target = Task.FromResult(entity);
                        break;

                    case 2:
                        target = Task.FromResult(entity);
                        offset = API.GetOffsetFromEntityGivenWorldCoords(entity, coords.X, coords.Y, coords.Z);
                        return true;

                    case 3:
                        if (Props.IsPocceProp(entity))
                        {
                            target = Task.FromResult(entity);
                            offset = API.GetOffsetFromEntityGivenWorldCoords(entity, coords.X, coords.Y, coords.Z);
                            return true;
                        }
                        break;
                }

                if (Permission.CanDo(Ability.RopeGunStaticObjects))
                {
                    target = Props.SpawnAtCoords("prop_devin_rope_01", coords, normal, true, false);
                    return true;
                }
            }

            return false;
        }

        private static async Task UpdateRopegun()
        {
            var playerID = API.PlayerId();
            var player = API.GetPlayerPed(-1);
            if (API.GetSelectedPedWeapon(player) != (int)Ropegun)
            {
                await Delay(100);
                return;
            }

            if (RopegunUndoKey > 0 && API.IsControlJustPressed(0, RopegunUndoKey))
            {
                _ropegunState.Undo(out bool clearLast);
                if (clearLast) ClearLast();
            }

            if (!API.IsPlayerFreeAiming(playerID))
                return;

            var attackControl = API.IsPedInAnyVehicle(player, false) ? 69 : 24;  // INPUT_VEH_ATTACK; INPUT_ATTACK
            var grapple = (RopegunWindKey > 0 && API.IsControlPressed(0, RopegunWindKey));

            if (API.IsControlJustPressed(0, attackControl) && TryShootRopegun(48f, out Task<int> target, out Vector3 offset))
            {
                var entity = await target;
                PlayerAttach(entity, offset, grapple ? ModeFlag.Ropegun | ModeFlag.Grapple : ModeFlag.Ropegun);
                //API.SetEntityAsNoLongerNeeded(ref entity);
            }
        }

        private static async Task UpdateRopes()
        {
            await Delay(10);

            foreach (var rope in _ropes.GetRopes().Cast<ClientRope>())
            {
                rope.Update();
            }
        }
    }

    internal class ClientRope : Shared.Rope
    {
        private int _handle;
        private float _length;
        private Scenario _scenario;

        private enum Scenario
        {
            EntityToEntity,
            EntityToGround,
            GroundToEntity,
            GroundToGround
        }

        public ClientRope(Player player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode) : base(player, entity1, entity2, offset1, offset2, mode)
        {
            _scenario = GetScenario(entity1, entity2);

            GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
            _length = (pos1 - pos2).Length();
            
            int unkPtr = 0;
            var ropePos = (_scenario == Scenario.EntityToGround) ? pos2 : pos1;
            _handle = API.AddRope(ropePos.X, ropePos.Y, ropePos.Z, 0f, 0f, 0f, _length, 1, _length, 1f, 0f, false, false, false, 5f, true, ref unkPtr);
            Attach(pos1, pos2);

            if ((Mode & ModeFlag.Grapple) == ModeFlag.Grapple)
                API.StartRopeWinding(_handle);
        }

        private static Scenario GetScenario(int entity1, int entity2)
        {
            if (entity1 > 0 && entity2 > 0)
                return Scenario.EntityToEntity;
            else if (entity1 > 0)
                return Scenario.EntityToGround;
            else if (entity2 > 0)
                return Scenario.GroundToEntity;
            else
                return Scenario.GroundToGround;
        }

        private void GetWorldCoords(out Vector3 pos1, out Vector3 pos2)
        {
            if (_scenario == Scenario.EntityToEntity || _scenario == Scenario.EntityToGround)
                pos1 = API.GetOffsetFromEntityInWorldCoords(Entity1, Offset1.X, Offset1.Y, Offset1.Z);
            else
                pos1 = Offset1;

            if (_scenario == Scenario.EntityToEntity || _scenario == Scenario.GroundToEntity)
                pos2 = API.GetOffsetFromEntityInWorldCoords(Entity2, Offset2.X, Offset2.Y, Offset2.Z);
            else
                pos2 = Offset2;
        }

        private void Attach(Vector3 pos1, Vector3 pos2)
        {
            switch (_scenario)
            {
                case Scenario.EntityToEntity:
                    API.AttachEntitiesToRope(_handle, Entity1, Entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, _length, false, false, null, null);
                    break;

                case Scenario.EntityToGround:
                    API.AttachRopeToEntity(_handle, Entity1, pos1.X, pos1.Y, pos1.Z, true);
                    API.PinRopeVertex(_handle, API.GetRopeVertexCount(_handle) - 1, pos2.X, pos2.Y, pos2.Z);
                    break;

                case Scenario.GroundToEntity:
                    API.AttachRopeToEntity(_handle, Entity2, pos2.X, pos2.Y, pos2.Z, true);
                    API.PinRopeVertex(_handle, 0, pos1.X, pos1.Y, pos1.Z);
                    break;

                case Scenario.GroundToGround:
                    API.PinRopeVertex(_handle, 0, pos1.X, pos1.Y, pos1.Z);
                    API.PinRopeVertex(_handle, API.GetRopeVertexCount(_handle) - 1, pos2.X, pos2.Y, pos2.Z);
                    break;
            }
        }

        public void Update()
        {
            // if a rope is shot, it ceases to exist
            var rope = _handle;
            if (!API.DoesRopeExist(ref rope))
                return;

            if (_scenario != Scenario.GroundToGround)
            {
                // if length is negative, rope is detached
                if (API.GetRopeLength(_handle) < 0f)
                {
                    GetWorldCoords(out Vector3 pos1, out Vector3 pos2);
                    Attach(pos1, pos2);
                }

                if ((Mode & ModeFlag.Grapple) == ModeFlag.Grapple && _length > 1f)
                {
                    _length -= 0.2f;
                }

                API.RopeSetUpdatePinverts(_handle);
                API.RopeForceLength(_handle, _length);
            }
        }

        public override void Clear()
        {
            API.DeleteRope(ref _handle);
        }
    }

    internal class RopegunState
    {
        private bool _firstUse;
        private int _lastEntity1;
        private int _lastEntity2;
        private Vector3 _lastOffset1;
        private Vector3 _lastOffset2;
        private DateTime _lastFire;

        public RopegunState()
        {
            _firstUse = true;
            Clear();
        }

        public void Update(ref int entity1, ref int entity2, ref Vector3 offset1, ref Vector3 offset2, out bool clearLast)
        {
            clearLast = false;

            var player = Common.GetPlayerPedOrVehicle();
            if (entity1 != player && entity2 != player)
                return;

            if (_firstUse)
            {
                Common.Notification("First time using ropegun, yay! You can connect 2 entities in 2 seconds");
                _firstUse = false;
            }

            var timestamp = DateTime.Now;
            if (_lastEntity1 != -1 && _lastEntity2 != -1 && (timestamp - _lastFire) < TimeSpan.FromSeconds(2f))
            {
                clearLast = (_lastEntity1 == player || _lastEntity2 == player);

                if (entity1 == player)
                {
                    entity1 = _lastEntity2;
                    offset1 = _lastOffset2;
                }
                else
                {
                    entity2 = _lastEntity2;
                    offset2 = _lastOffset2;
                }
            }

            _lastEntity1 = entity1;
            _lastEntity2 = entity2;
            _lastOffset1 = offset1;
            _lastOffset2 = offset2;
            _lastFire = timestamp;
        }

        public void Undo(out bool clearLast)
        {
            var player = Common.GetPlayerPedOrVehicle();
            clearLast = (_lastEntity1 == player || _lastEntity2 == player);
            Clear();
        }

        public void Clear()
        {
            _lastEntity1 = -1;
            _lastEntity2 = -1;
            _lastOffset1 = Vector3.Zero;
            _lastOffset2 = Vector3.Zero;
            _lastFire = DateTime.MinValue;
        }
    }
}
