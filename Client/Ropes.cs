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
        private static readonly uint _ropegun = 0x44AE7910; // WEAPON_POCCE_ROPEGUN
        private static Dictionary<int, List<int>> _ropes = new Dictionary<int, List<int>>();

        public Ropes()
        {
            EventHandlers["PocceMod:AddRope"] += new Action<int, int, int, bool>(AddRope);
            EventHandlers["PocceMod:ClearRopes"] += new Action<int>(ClearRopes);
            EventHandlers["PocceMod:ClearLastRope"] += new Action<int>(ClearLastRope);

            API.AddTextEntryByHash(0x6FCC4E8A, "Pocce Ropegun"); // WT_POCCE_ROPEGUN

            Tick += Update;
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

        private static void AddRope(int player, int entity1, int entity2, bool tow)
        {
            entity1 = API.NetToEnt(entity1);
            entity2 = API.NetToEnt(entity2);

            var pos1 = tow ? GetAdjustedPosition(entity1, -0.75f) : API.GetEntityCoords(entity1, API.IsEntityAPed(entity1));
            var pos2 = tow ? GetAdjustedPosition(entity2, 0.75f) : API.GetEntityCoords(entity2, API.IsEntityAPed(entity2));
            var length = (float)Math.Sqrt(pos1.DistanceToSquared(pos2));

            int unkPtr = 0;
            var rope = API.AddRope(pos1.X, pos1.Y, pos1.Z, 0.0f, 0.0f, 0.0f, length, 1, length, 1.0f, 0.0f, false, false, false, 5.0f, true, ref unkPtr);
            API.AttachEntitiesToRope(rope, entity1, entity2, pos1.X, pos1.Y, pos1.Z, pos2.X, pos2.Y, pos2.Z, length, false, false, null, null);

            if (_ropes.TryGetValue(player, out List<int> playerRopes))
                playerRopes.Add(rope);
            else
                _ropes.Add(player, new List<int> { rope });

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

        public static void PlayerAttach(int entity, bool tow = false)
        {
            var player = API.GetPlayerPed(-1);
            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                Attach(vehicle, entity, tow);
            }
            else
            {
                Attach(player, entity, tow);
            }
        }

        public static void Attach(int entity1, int entity2, bool tow = false)
        {
            if (!Permission.CanDo(Ability.RopeOtherPlayer))
            {
                var player = API.GetPlayerPed(-1);
                if ((API.IsEntityAPed(entity1) && API.IsPedAPlayer(entity1) && entity1 != player) ||
                    (API.IsEntityAPed(entity2) && API.IsPedAPlayer(entity2) && entity2 != player))
                {
                    Hud.Notification("You are not allowed to attach rope to another player");
                    return;
                }
            }

            TriggerServerEvent("PocceMod:AddRope", API.ObjToNet(entity1), API.ObjToNet(entity2), tow);
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
            Peds.GiveWeapon(API.GetPlayerPed(-1), _ropegun);
            //Game.PlayerPed.Weapons.Give((WeaponHash)_ropegun, int.MaxValue, true, true);
        }

        private static Task Update()
        {
            var playerID = API.PlayerId();
            var player = API.GetPlayerPed(-1);
            if (API.GetSelectedPedWeapon(player) != (int)_ropegun)
                return Delay(100);

            if (!API.IsPlayerFreeAiming(playerID))
                return Delay(10);

            int target = 0;
            if (!API.GetEntityPlayerIsFreeAimingAt(playerID, ref target))
                return Delay(0);

            if (API.IsPedInAnyVehicle(player, false))
            {
                var vehicle = API.GetVehiclePedIsIn(player, false);
                if (API.IsControlJustPressed(0, 69))
                    Attach(vehicle, target);
            }
            else
            {
                if (API.IsControlJustPressed(0, 24))
                    Attach(player, target);
            }

            return Delay(0);
        }
    }
}
