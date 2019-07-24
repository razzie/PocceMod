﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public static class Common
    {
        public static void Notification(string message, bool blink = false, bool saveToBrief = false)
        {
            API.SetNotificationTextEntry("CELL_EMAIL_BCON");
            foreach (string s in CitizenFX.Core.UI.Screen.StringToArray(message))
            {
                API.AddTextComponentSubstringPlayerName(s);
            }
            API.DrawNotification(blink, saveToBrief);
        }

        // I'm really sorry for stealing this
        // source: https://github.com/TomGrobbe/vMenu/blob/master/vMenu/CommonFunctions.cs
        public static async Task<string> GetUserInput(string windowTitle, string defaultText, int maxInputLength)
        {
            // Create the window title string.
            var spacer = "\t";
            API.AddTextEntry($"{API.GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", $"{windowTitle ?? "Enter"}:{spacer}(MAX {maxInputLength.ToString()} Characters)");

            // Display the input box.
            API.DisplayOnscreenKeyboard(1, $"{API.GetCurrentResourceName().ToUpper()}_WINDOW_TITLE", "", defaultText ?? "", "", "", "", maxInputLength);
            await BaseScript.Delay(0);
            // Wait for a result.
            while (true)
            {
                int keyboardStatus = API.UpdateOnscreenKeyboard();

                switch (keyboardStatus)
                {
                    case 3: // not displaying input field anymore somehow
                    case 2: // cancelled
                        return null;
                    case 1: // finished editing
                        return API.GetOnscreenKeyboardResult();
                    default:
                        await BaseScript.Delay(0);
                        break;
                }
            }
        }

        public static async Task RequestModel(uint model)
        {
            while (!API.HasModelLoaded(model))
            {
                API.RequestModel(model);
                await BaseScript.Delay(10);
            }
        }

        public static async Task RequestCollision(uint model)
        {
            while (!API.HasCollisionForModelLoaded(model))
            {
                API.RequestCollisionForModel(model);
                await BaseScript.Delay(10);
            }
        }

        public static async Task NetworkRequestControl(int entity)
        {
            while (!API.NetworkHasControlOfEntity(entity))
            {
                API.NetworkRequestControlOfEntity(entity);
                await BaseScript.Delay(10);
            }
        }

        public static async Task<int> WaitForNetEntity(int netEntity)
        {
            if (netEntity == 0)
                return 0;

            var timeout = DateTime.Now + TimeSpan.FromSeconds(10);
            var entity = API.NetToEnt(netEntity);

            while (!API.DoesEntityExist(entity) && DateTime.Now < timeout)
            {
                await BaseScript.Delay(100);
                entity = API.NetToEnt(netEntity);
            }

            return entity;
        }

        public static int GetPlayerPedOrVehicle()
        {
            var player = API.GetPlayerPed(-1);
            return API.IsPedInAnyVehicle(player, false) ? API.GetVehiclePedIsIn(player, false) : player;
        }

        public static bool GetClosestEntity(IEnumerable<int> entities, out int closest, int originEntity = -1)
        {
            closest = -1;
            bool found = false;
            float minDist = float.MaxValue;
            var coords = API.GetEntityCoords((originEntity != -1) ? originEntity : API.GetPlayerPed(-1), false);

            foreach (var entity in entities)
            {
                var pos = API.GetEntityCoords(entity, API.IsEntityAPed(entity));
                var dist = coords.DistanceToSquared(pos);

                if (dist < minDist)
                {
                    closest = entity;
                    minDist = dist;
                    found = true;
                }
            }

            return found;
        }

        public static bool IsEntityInRangeSquared(int entity, float rangeSquared)
        {
            var playerPos = API.GetEntityCoords(API.GetPlayerPed(-1), true);
            var entityPos = API.GetEntityCoords(entity, false);
            return playerPos.DistanceToSquared(entityPos) <= rangeSquared;
        }

        public static bool GetWaypoint(out Vector3 wp, bool adjust = true)
        {
            wp = Vector3.Zero;

            if (!API.IsWaypointActive())
                return false;

            wp = API.GetBlipInfoIdCoord(API.GetFirstBlipInfoId(8));

            if (adjust)
            {
                var adjustedWp = Vector3.Zero;
                if (API.GetClosestVehicleNode(wp.X, wp.Y, wp.Z, ref adjustedWp, 1, 100f, 2.5f))
                    wp = adjustedWp;
            }

            return true;
        }

        public static List<int> GetObjects()
        {
            var objs = new List<int>();
            int obj = 0;
            int handle = API.FindFirstObject(ref obj);
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), true);

            if (handle == -1)
                return objs;

            do
            {
                objs.Add(obj);

            } while (API.FindNextObject(handle, ref obj));

            API.EndFindObject(handle);
            return objs;
        }

        public static void GetAimCoords(out Vector3 position, out Vector3 target, float distance)
        {
            position = API.GetGameplayCamCoords();
            var rot = API.GetGameplayCamRot(2);
            var forward = RotationToDirection(rot) * distance;
            target = position + forward;
        }

        public static void GetCamHorizontalForwardAndRightVectors(out Vector3 forward, out Vector3 right)
        {
            var heading = API.GetGameplayCamRot(2).Z;
            var headingRad = MathUtil.DegreesToRadians(heading);
            forward = new Vector3(-(float)Math.Sin(headingRad), (float)Math.Cos(headingRad), 0f);
            right = new Vector3(forward.Y, -forward.X, 0f);
        }

        public static Vector3 RotationToDirection(Vector3 rot)
        {
            float radiansZ = rot.Z * 0.0174532924f;
            float radiansX = rot.X * 0.0174532924f;
            float num = Math.Abs((float)Math.Cos(radiansX));
            return new Vector3
            {
                X = -(float)Math.Sin(radiansZ) * num,
                Y = (float)Math.Cos(radiansZ) * num,
                Z = (float)Math.Sin(radiansX)
            };
        }
    }
}
