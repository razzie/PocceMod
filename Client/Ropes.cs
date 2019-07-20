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

        private class RopegunState
        {
            private bool _ropegunFirstUse;
            private int _lastRopegunEntity1;
            private int _lastRopegunEntity2;
            private DateTime _lastRopegunFire;

            public RopegunState()
            {
                _ropegunFirstUse = true;
                Clear();
            }

            public void Update(ref int entity1, ref int entity2, out bool clearLast)
            {
                clearLast = false;

                var player = GetPlayerEntity();
                if (entity1 != player && entity2 != player)
                    return;

                if (_ropegunFirstUse)
                {
                    Common.Notification("First time using ropegun, yay! You can connect 2 entities in 2 seconds");
                    _ropegunFirstUse = false;
                }

                var timestamp = DateTime.Now;
                if (_lastRopegunEntity1 != -1 && _lastRopegunEntity2 != -1 && (timestamp - _lastRopegunFire) < TimeSpan.FromSeconds(2f))
                {
                    clearLast = (_lastRopegunEntity1 == player || _lastRopegunEntity2 == player);

                    if (entity1 == player)
                        entity1 = _lastRopegunEntity2;
                    else
                        entity2 = _lastRopegunEntity2;
                }

                _lastRopegunEntity1 = entity1;
                _lastRopegunEntity2 = entity2;
                _lastRopegunFire = timestamp;

            }

            public void Clear()
            {
                _lastRopegunEntity1 = -1;
                _lastRopegunEntity2 = -1;
                _lastRopegunFire = DateTime.MinValue;
            }
        }

        private const uint Ropegun = 0x44AE7910; // WEAPON_POCCE_ROPEGUN
        private static readonly int RopegunWindKey;
        private static readonly Dictionary<int, List<int>> _ropes = new Dictionary<int, List<int>>();
        private static readonly List<int> _windingRopes = new List<int>();
        private static readonly RopegunState _ropegunState = new RopegunState();

        static Ropes()
        {
            RopegunWindKey = Config.GetConfigInt("RopegunWindKey");
        }

        public Ropes()
        {
            EventHandlers["PocceMod:AddRope"] += new Func<int, int, int, int, Task>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<int>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<int>(ClearLastRope);

            API.AddTextEntryByHash(0x6FCC4E8A, "Pocce Ropegun"); // WT_POCCE_ROPEGUN

            Tick += Update;
            Tick += RopeWindUpdate;
        }

        private static int GetPlayerEntity()
        {
            var player = API.GetPlayerPed(-1);
            return API.IsPedInAnyVehicle(player, false) ? API.GetVehiclePedIsIn(player, false) : player;
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

        private static async Task AddRope(int player, int entity1, int entity2, int mode)
        {
            if (entity1 == entity2)
                return;

            entity1 = await Common.WaitForNetEntity(entity1);
            entity2 = await Common.WaitForNetEntity(entity2);

            bool tow = ((Mode)mode & Mode.Tow) == Mode.Tow;
            var pos1 = tow ? GetAdjustedPosition(entity1, -0.75f) : API.GetEntityCoords(entity1, API.IsEntityAPed(entity1));
            var pos2 = tow ? GetAdjustedPosition(entity2, 0.75f) : API.GetEntityCoords(entity2, API.IsEntityAPed(entity2));
            var length = (float)Math.Sqrt(pos1.DistanceToSquared(pos2));

            int unkPtr = 0;
            var rope = API.AddRope(pos1.X, pos1.Y, pos1.Z, 0f, 0f, 0f, length, 1, length, 1f, 0f, false, false, false, 5f, true, ref unkPtr);
            API.AttachEntitiesToRope(rope, entity1, entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, length, false, false, null, null);

            if (_ropes.TryGetValue(player, out List<int> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(player, new List<int> { rope });

            if (((Mode)mode & Mode.Grapple) == Mode.Grapple)
            {
                API.StartRopeWinding(rope);
                _windingRopes.Add(rope);
            }

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

        public static void PlayerAttach(int entity, Mode mode = Mode.Normal)
        {
            Attach(GetPlayerEntity(), entity, mode);
        }

        public static void Attach(int entity1, int entity2, Mode mode = Mode.Normal)
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
                _ropegunState.Update(ref entity1, ref entity2, out bool clearLast);
                if (clearLast)
                    ClearLast();
            }
            else
            {
                _ropegunState.Clear();
            }

            if (entity1 == entity2)
                return;

            TriggerServerEvent("PocceMod:AddRope", API.ObjToNet(entity1), API.ObjToNet(entity2), (int)mode);
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
            Peds.GiveWeapon(API.GetPlayerPed(-1), Ropegun);
        }

        private static async Task Update()
        {
            var playerID = API.PlayerId();
            var player = API.GetPlayerPed(-1);
            if (API.GetSelectedPedWeapon(player) != (int)Ropegun)
            {
                await Delay(100);
                return;
            }

            if (!API.IsPlayerFreeAiming(playerID))
                return;

            var attackControl = API.IsPedInAnyVehicle(player, false) ? 69 : 24;  // INPUT_VEH_ATTACK; INPUT_ATTACK
            if (API.IsControlJustPressed(0, attackControl))
            {
                Mode mode = (RopegunWindKey > 0 && API.IsControlPressed(0, RopegunWindKey)) ? Mode.Ropegun | Mode.Grapple : Mode.Ropegun;

                int target = 0;
                if (API.GetEntityPlayerIsFreeAimingAt(playerID, ref target) &&
                    (API.IsEntityAPed(target) || API.IsEntityAVehicle(target) || API.DoesEntityBelongToThisScript(target, true)))
                {
                    if (API.IsEntityAPed(target) && API.IsPedInAnyVehicle(target, false))
                        target = API.GetVehiclePedIsIn(target, false);

                    PlayerAttach(target, mode);
                }
                else if (Permission.CanDo(Ability.RopeGunStaticObjects))
                {
                    player = GetPlayerEntity();
                    Common.GetAimCoords(out Vector3 rayBegin, out Vector3 rayEnd, 30f);
                    var ray = API.CastRayPointToPoint(rayBegin.X, rayBegin.Y, rayBegin.Z, rayEnd.X, rayEnd.Y, rayEnd.Z, -1, player, 0);

                    bool hit = false;
                    var endCoords = Vector3.Zero;
                    var surfaceNormal = Vector3.Zero;
                    int entityHit = -1;
                    API.GetRaycastResult(ray, ref hit, ref endCoords, ref surfaceNormal, ref entityHit);

                    if (hit)
                    {
                        var prop = await Props.SpawnAtCoords("prop_devin_rope_01", endCoords, surfaceNormal);
                        API.FreezeEntityPosition(prop, true);
                        PlayerAttach(prop, mode);
                        API.SetEntityAsNoLongerNeeded(ref prop);
                    }
                }
            }
        }

        private static async Task RopeWindUpdate()
        {
            await Delay(10);

            if (_windingRopes.Count == 0)
                return;

            foreach (var rope in _windingRopes.ToArray())
            {
                int tmp_rope = rope;
                if (!API.DoesRopeExist(ref tmp_rope))
                {
                    _windingRopes.Remove(rope);
                    continue;
                }

                var length = API.GetRopeLength(rope);
                if (length > 1f)
                    API.RopeForceLength(rope, length - 0.2f);
            }
        }
    }
}
