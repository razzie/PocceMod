using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Ropes : BaseScript
    {
        public static readonly float MaxLength = Config.GetConfigFloat("MaxRopeLength");
        private const uint Ropegun = 0x44AE7910; // WEAPON_POCCE_ROPEGUN
        private static readonly int RopegunWindKey;
        private static readonly int RopeClearKey;
        private static int _nextRopeID;
        private static readonly RopeSet _ropes = new RopeSet();
        private static readonly RopegunState _ropegunState = new RopegunState();
        private static readonly Dictionary<int, DateTime> _expirations = new Dictionary<int, DateTime>();

        [Flags]
        public enum ModeFlag
        {
            Normal = 0,
            Tow = 1,
            Ropegun = 2,
            Grapple = 4
        }

        static Ropes()
        {
            RopegunWindKey = Config.GetConfigInt("RopegunWindKey");
            RopeClearKey = Config.GetConfigInt("RopeClearKey");

            API.AddTextEntryByHash(0x6FCC4E8A, "Pocce Ropegun"); // WT_POCCE_ROPEGUN
        }

        public Ropes()
        {
            EventHandlers["PocceMod:AddRope"] += new Func<string, int, int, int, Vector3, Vector3, float, Task>(NetAddRope);
            EventHandlers["PocceMod:SetRopeLength"] += new Action<string, int, float>(NetSetRopeLength);
            EventHandlers["PocceMod:RemoveRope"] += new Action<string, int>(NetRemoveRope);

            TriggerServerEvent("PocceMod:RequestRopes");

            Tick += Telemetry.Wrap("ropegun", UpdateRopegun);
            Tick += Telemetry.Wrap("ropes", UpdateRopes);
            Tick += Telemetry.Wrap("rope_hotkeys", UpdateRopeHotkeys);
        }

        private static void AdjustOffsetForTowing(int entity, ref Vector3 offset, float towOffset)
        {
            if (!API.IsEntityAVehicle(entity))
                return;

            var model = (uint)API.GetEntityModel(entity);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            offset = new Vector3(0f, 1f, 0f);

            if (towOffset > 0)
                offset *= (max.X * towOffset);
            else
                offset *= (-min.X * towOffset);
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
                if (API.GetPedInVehicleSeat(entity, -1) == API.GetPlayerPed(-1))
                    return false;

                var players = Vehicles.GetPlayers(entity);
                return players.Any(player => player != playerID);
            }

            return false;
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

        public static int? Attach(int entity1, int entity2, Vector3 offset1, Vector3 offset2, ModeFlag mode = ModeFlag.Normal)
        {
            if (!Permission.CanDo(Ability.RopeOtherPlayer) && (IsOtherPlayerEntity(entity1) || IsOtherPlayerEntity(entity2)))
            {
                Common.Notification("You are not allowed to attach rope to another player");
                return null;
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
                return null;

            int ObjToNet(int entity)
            {
                if (entity == 0)
                    return 0;

                return API.ObjToNet(entity);
            }

            Vector3 GetPos(int entity, Vector3 offset)
            {
                if (entity == 0)
                    return offset;

                return API.GetOffsetFromEntityInWorldCoords(entity, offset.X, offset.Y, offset.Z);
            }

            var id = ++_nextRopeID;
            var length = (GetPos(entity1, offset1) - GetPos(entity2, offset2)).Length();

            TriggerServerEvent("PocceMod:AddRope", id, ObjToNet(entity1), ObjToNet(entity2), offset1, offset2, length);

            if ((mode & ModeFlag.Grapple) == ModeFlag.Grapple)
                TriggerServerEvent("PocceMod:SetRopeLength", id, 1f);

            if (entity1 == 0 && entity2 == 0)
                _expirations.Add(id, DateTime.Now + TimeSpan.FromMinutes(1));

            return id;
        }

        public static void ClearAll()
        {
            foreach (var rope in _ropes.GetPlayerRopes(Common.PlayerID.ToString()).ToArray())
                TriggerServerEvent("PocceMod:RemoveRope", rope.ID);
        }

        public static void ClearLast()
        {
            var ropes = _ropes.GetPlayerRopes(Common.PlayerID.ToString()).ToArray();
            if (ropes.Length > 0)
            {
                var lastRopeID = ropes.Max(rope => rope.ID);
                TriggerServerEvent("PocceMod:RemoveRope", lastRopeID);
            }
        }

        public static void ClearPlayer()
        {
            var player = API.GetPlayerPed(-1);
            if (_ropes.IsAnyRopeAttachedToEntity(player))
                TriggerServerEvent("PocceMod:RemoveEntityRopes", API.PedToNet(player));

            if (API.IsPedInAnyVehicle(player, false))
            {
                int vehicle = API.GetVehiclePedIsIn(player, false);
                if (_ropes.IsAnyRopeAttachedToEntity(vehicle))
                    TriggerServerEvent("PocceMod:RemoveEntityRopes", API.VehToNet(vehicle));
            }
        }

        private static async Task NetAddRope(string player, int id, int netEntity1, int netEntity2, Vector3 offset1, Vector3 offset2, float length)
        {
            var rope = await RopeWrapper.Create(player, id, netEntity1, netEntity2, offset1, offset2, length);
            if (player == Common.PlayerID.ToString())
                rope.OnDespawn += _ => TriggerServerEvent("PocceMod:RemoveRope", id);

            _ropes.AddRope(rope);

            if (!API.RopeAreTexturesLoaded())
                API.RopeLoadTextures();
        }

        private static void NetSetRopeLength(string player, int id, float length)
        {
            var rope = _ropes.GetRope(player, id);
            if (rope != null)
                rope.Length = length;
        }

        private static void NetRemoveRope(string player, int id)
        {
            _ropes.RemoveRope(player, id);
        }

        public static void EquipRopeGun()
        {
            var player = API.GetPlayerPed(-1);
            Weapons.Give(player, Ropegun);
            API.SetCurrentPedVehicleWeapon(player, Ropegun);
        }

        private static bool TryShootRopegun(float distance, out int target, out Vector3 offset)
        {
            var player = Common.GetPlayerPedOrVehicle();
            Common.GetAimCoords(out Vector3 rayBegin, out Vector3 rayEnd, distance);
            var ray = API.StartShapeTestRay(rayBegin.X, rayBegin.Y, rayBegin.Z, rayEnd.X, rayEnd.Y, rayEnd.Z, 1 | 2 | 4 | 8 | 16 | 32, player, 0);

            target = 0;
            offset = Vector3.Zero;

            bool hit = false;
            var coords = Vector3.Zero;
            var normal = Vector3.Zero;
            API.GetShapeTestResult(ray, ref hit, ref coords, ref normal, ref target);

            if (hit)
            {
                switch (API.GetEntityType(target))
                {
                    case 1:
                        return true;

                    case 2:
                        if (coords == Vector3.Zero)
                            offset = coords;
                        else
                            offset = API.GetOffsetFromEntityGivenWorldCoords(target, coords.X, coords.Y, coords.Z);
                        return true;

                    case 3:
                        if (API.NetworkGetEntityIsNetworked(target))
                        {
                            if (coords == Vector3.Zero)
                                offset = coords;
                            else
                                offset = API.GetOffsetFromEntityGivenWorldCoords(target, coords.X, coords.Y, coords.Z);
                            return true;
                        }
                        break;
                }

                if (Permission.CanDo(Ability.RopeGunStaticObjects) && coords != Vector3.Zero)
                {
                    offset = coords;
                    target = 0;
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

            bool isAiming = false;
            if (API.IsPlayerFreeAiming(playerID))
            {
                isAiming = true;
            }
            else if (API.IsControlPressed(0, 25)) // INPUT_AIM
            {
                API.ShowHudComponentThisFrame(14); // crosshair
                isAiming = true;
            }

            if (!isAiming)
                return;

            var attackControl = API.IsPedInAnyVehicle(player, false) ? 69 : 24;  // INPUT_VEH_ATTACK; INPUT_ATTACK
            var grapple = (RopegunWindKey > 0 && API.IsControlPressed(0, RopegunWindKey));

            if (API.IsControlJustPressed(0, attackControl) && TryShootRopegun(MaxLength, out int target, out Vector3 offset))
            {
                PlayerAttach(target, offset, grapple ? ModeFlag.Ropegun | ModeFlag.Grapple : ModeFlag.Ropegun);

                if (!API.IsPlayerFreeAiming(playerID)) // force shoot effect
                {
                    var coords = (target == 0) ? offset : API.GetOffsetFromEntityInWorldCoords(target, offset.X, offset.Y, offset.Z);

                    if (API.IsPedInAnyVehicle(player, false))
                        API.SetVehicleShootAtTarget(player, 0, coords.X, coords.Y, coords.Z);
                    else
                        API.SetPedShootsAtCoord(player, coords.X, coords.Y, coords.Z, false);
                }
            }
        }

        private static async Task UpdateRopes()
        {
            await Delay(10);

            foreach (var rope in _ropes.Ropes.ToArray())
            {
                rope.Update();
            }

            var now = DateTime.Now;
            foreach (var rope in _expirations.ToArray())
            {
                if (rope.Value < now)
                {
                    TriggerServerEvent("PocceMod:RemoveRope", rope.Key);
                    _expirations.Remove(rope.Key);
                }
            }
        }

        private static Task UpdateRopeHotkeys()
        {
            if (RopeClearKey > 0 && API.IsControlJustPressed(0, RopeClearKey))
            {
                ClearPlayer();
            }

            if (RopegunWindKey > 0 && API.IsControlJustPressed(0, RopegunWindKey) && !API.IsControlPressed(0, 25)) // INPUT_AIM
            {
                var player = Common.PlayerID.ToString();
                var ropes = _ropes.GetEntityRopes(Common.GetPlayerPedOrVehicle()).Where(rope => rope.Player == player).ToArray();
                foreach (var rope in ropes)
                {
                    TriggerServerEvent("PocceMod:SetRopeLength", rope.ID, 1f);
                }
            }

            return Task.FromResult(0);
        }
    }
}
