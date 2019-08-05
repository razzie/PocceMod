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
        internal static int RootObject { get; private set; }
        private const uint Ropegun = 0x44AE7910; // WEAPON_POCCE_ROPEGUN
        private static readonly int RopegunWindKey;
        private static readonly int RopeClearKey;
        private static readonly RopeSet _ropes = new RopeSet();
        private static readonly RopegunState _ropegunState = new RopegunState();
        private static readonly List<RopeWrapper> _ropeCleanupList = new List<RopeWrapper>();

        static Ropes()
        {
            RopegunWindKey = Config.GetConfigInt("RopegunWindKey");
            RopeClearKey = Config.GetConfigInt("RopeClearKey");
        }

        public Ropes()
        {
            EventHandlers["PocceMod:AddRope"] += new Func<int, int, int, Vector3, Vector3, int, Task>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<int>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<int>(ClearLastRope);
            EventHandlers["PocceMod:ClearEntityRopes"] += new Func<int, Task>(ClearEntityRopes);

            TriggerServerEvent("PocceMod:RequestRopes");

            API.AddTextEntryByHash(0x6FCC4E8A, "Pocce Ropegun"); // WT_POCCE_ROPEGUN

            Tick += UpdateRopegun;
            Tick += UpdateRopes;

            if (RopeClearKey > 0)
            {
                Tick += UpdateRopeClear;
            }
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

        private static async Task AddRope(int player, int entity1, int entity2, Vector3 offset1, Vector3 offset2, int mode)
        {
            if (entity1 == entity2 && entity1 > 0)
                return;

            if (RootObject == 0)
            {
                var model = (uint)API.GetHashKey("prop_devin_rope_01");
                await Common.RequestModel(model);
                RootObject = API.CreateObject((int)model, 0f, 0f, 0f, false, false, false);
                API.SetModelAsNoLongerNeeded(model);
                API.FreezeEntityPosition(RootObject, true);
            }

            entity1 = await Common.WaitForNetEntity(entity1);
            entity2 = await Common.WaitForNetEntity(entity2);

            if (entity1 == 0)
                entity1 = RootObject;

            if (entity2 == 0)
                entity2 = RootObject;

            if (!API.DoesEntityExist(entity1) || !API.DoesEntityExist(entity2))
                return;

            var rope = new RopeWrapper(new Player(player), entity1, entity2, offset1, offset2, (ModeFlag)mode);
            _ropes.AddRope(rope);

            if (entity1 == RootObject && entity2 == RootObject)
                _ropeCleanupList.Add(rope);

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

        private static async Task ClearEntityRopes(int entity)
        {
            _ropes.ClearEntityRopes(await Common.WaitForNetEntity(entity));
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

        public static void ClearPlayer()
        {
            TriggerServerEvent("PocceMod:ClearEntityRopes", API.ObjToNet(Common.GetPlayerPedOrVehicle()));
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
            //var ray = API.CastRayPointToPoint(rayBegin.X, rayBegin.Y, rayBegin.Z, rayEnd.X, rayEnd.Y, rayEnd.Z, 1 | 2 | 4 | 8 | 16 | 32, player, 0);
            var ray = API.StartShapeTestRay(rayBegin.X, rayBegin.Y, rayBegin.Z, rayEnd.X, rayEnd.Y, rayEnd.Z, 1 | 2 | 4 | 8 | 16 | 32, player, 0);

            target = 0;
            offset = Vector3.Zero;

            bool hit = false;
            var coords = Vector3.Zero;
            var normal = Vector3.Zero;
            //API.GetRaycastResult(ray, ref hit, ref coords, ref normal, ref target);
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
                        if (API.NetworkDoesEntityExistWithNetworkId(target))
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

            if (API.IsControlJustPressed(0, attackControl) && TryShootRopegun(48f, out int target, out Vector3 offset))
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

            foreach (var rope in _ropes.GetRopes())
            {
                rope.Update();
            }

            var now = DateTime.Now;
            foreach (var rope in _ropeCleanupList.ToArray())
            {
                if (rope.Created + TimeSpan.FromMinutes(1) < now)
                {
                    rope.Clear();
                    _ropeCleanupList.Remove(rope);
                }
            }
        }

        private static Task UpdateRopeClear()
        {
            if (API.IsControlJustPressed(0, RopeClearKey))
            {
                ClearPlayer();
            }

            return Task.FromResult(0);
        }
    }
}
