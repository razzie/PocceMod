using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Ropes : BaseScript
    {
        [Flags]
        public enum Mode
        {
            Normal = 0,
            Tow = 1,
            Ropegun = 2,
            Grapple = 4
        }

        private const uint Ropegun = 0x44AE7910; // WEAPON_POCCE_ROPEGUN
        private static readonly int RopegunWindKey;
        private static readonly int RopegunUndoKey;
        private static readonly Dictionary<int, List<int>> _ropes = new Dictionary<int, List<int>>();
        private static readonly List<RopeWindState> _windingRopes = new List<RopeWindState>();
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
            Tick += RopeWindUpdate;
        }

        private static Vector3 GetAdjustedPosition(int entity, float front)
        {
            var right = Vector3.Zero;
            var forward = Vector3.Zero;
            var up = Vector3.Zero;
            var pos = Vector3.Zero;
            API.GetEntityMatrix(entity, ref right, ref forward, ref up, ref pos);

            if (!API.IsEntityAVehicle(entity))
                return pos;

            var model = (uint)API.GetEntityModel(entity);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            if (front > 0)
                right *= (max.X * front);
            else
                right *= (-min.X * front);

            pos += right;
            return pos;
        }

        private static async Task AddRope(int player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, int mode)
        {
            if (entity1 == entity2)
                return;

            entity1 = await Common.WaitForNetEntity(entity1);
            entity2 = await Common.WaitForNetEntity(entity2);

            bool tow = ((Mode)mode & Mode.Tow) == Mode.Tow;
            var pos1 = tow ? GetAdjustedPosition(entity1, -0.75f) : API.GetOffsetFromEntityInWorldCoords(entity1, offset1.X, offset1.Y, offset1.Z);
            var pos2 = tow ? GetAdjustedPosition(entity2, 0.75f) : API.GetOffsetFromEntityInWorldCoords(entity2, offset2.X, offset2.Y, offset2.Z);
            var length = (float)Math.Sqrt(pos1.DistanceToSquared(pos2));

            int unkPtr = 0;
            var rope = API.AddRope(pos1.X, pos1.Y, pos1.Z, 0f, 0f, 0f, length, 1, length, 1f, 0f, false, false, false, 5f, true, ref unkPtr);
            API.AttachEntitiesToRope(rope, entity1, entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, length, false, false, null, null);

            if (_ropes.TryGetValue(player, out List<int> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(player, new List<int> { rope });

            if (((Mode)mode & Mode.Grapple) == Mode.Grapple)
                _windingRopes.Add(new RopeWindState(rope, length));

            if (!API.RopeAreTexturesLoaded())
                API.RopeLoadTextures();
        }

        private static void ClearRopes(int player)
        {
            if (_ropes.TryGetValue(player, out List<int> playerRopes))
            {
                foreach (var rope in playerRopes)
                {
                    var tmp_rope = rope;
                    API.DeleteRope(ref tmp_rope);
                }

                playerRopes.Clear();
            }
        }

        private static void ClearLastRope(int player)
        {
            if (_ropes.TryGetValue(player, out List<int> playerRopes))
            {
                if (playerRopes.Count == 0)
                    return;

                var rope = playerRopes[playerRopes.Count - 1];
                API.DeleteRope(ref rope);
                playerRopes.RemoveAt(playerRopes.Count - 1);
            }
        }

        public static void PlayerAttach(int entity, Vector3 offset, Mode mode = Mode.Normal)
        {
            Attach(Common.GetPlayerPedOrVehicle(), entity, Vector3.Zero, offset, mode);
        }

        public static void AttachToClosest(IEnumerable<int> entities, bool tow = false)
        {
            if (Common.GetClosestEntity(entities, out int closest))
                PlayerAttach(closest, Vector3.Zero, tow ? Mode.Tow : Mode.Normal);
            else
                Common.Notification("Nothing in range");
        }

        public static void Attach(int entity1, int entity2, Vector3 offset1, Vector3 offset2, Mode mode = Mode.Normal)
        {
            if (!Permission.CanDo(Ability.RopeOtherPlayer))
            {
                var player = API.GetPlayerPed(-1);
                if ((API.IsEntityAPed(entity1) && API.IsPedAPlayer(entity1) && entity1 != player) ||
                    (API.IsEntityAPed(entity2) && API.IsPedAPlayer(entity2) && entity2 != player))
                {
                    Common.Notification("You are not allowed to attach rope to another player");
                    return;
                }
            }

            if ((mode & Mode.Ropegun) == Mode.Ropegun)
            {
                _ropegunState.Update(ref entity1, ref entity2, ref offset1, ref offset2, out bool clearLast);
                if (clearLast)
                    ClearLast();
            }
            else
            {
                _ropegunState.Clear();
            }

            if (entity1 == entity2)
                return;

            TriggerServerEvent("PocceMod:AddRope", API.ObjToNet(entity1), API.ObjToNet(entity2), offset1, offset2, (int)mode);
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
                    target = Props.SpawnAtCoords("prop_devin_rope_01", coords, normal, true);
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
                PlayerAttach(entity, offset, grapple ? Mode.Ropegun | Mode.Grapple : Mode.Ropegun);
                API.SetEntityAsNoLongerNeeded(ref entity);
            }
        }

        private static async Task RopeWindUpdate()
        {
            await Delay(10);

            if (_windingRopes.Count == 0)
                return;

            foreach (var rope in _windingRopes.ToArray())
            {
                if (!rope.Valid)
                {
                    _windingRopes.Remove(rope);
                    continue;
                }

                rope.Update();
            }
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

    internal class RopeWindState
    {
        private readonly int _rope;
        private float _length;

        public RopeWindState(int rope, float length)
        {
            API.StartRopeWinding(rope);
            _rope = rope;
            _length = length;
        }

        public bool Valid
        {
            get
            {
                int rope = _rope;
                return API.DoesRopeExist(ref rope) && _length > 1f;
            }
        }

        public void Update()
        {
            _length -= 0.2f;
            API.RopeForceLength(_rope, _length);
        }
    }
}
