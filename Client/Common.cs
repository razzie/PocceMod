using CitizenFX.Core;
using CitizenFX.Core.Native;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public static class Common
    {
        private static int? _playerID;
        public static int PlayerID
        {
            get
            {
                if (_playerID == null)
                    _playerID = API.GetPlayerServerId(API.PlayerId());

                return (int)_playerID;
            }
        }

        public static void Notification(string message, bool blink = false, bool saveToBrief = false)
        {
            API.SetNotificationTextEntry("CELL_EMAIL_BCON");
            foreach (string s in CitizenFX.Core.UI.Screen.StringToArray(message))
            {
                API.AddTextComponentSubstringPlayerName(s);
            }
            API.DrawNotification(blink, saveToBrief);
        }

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
            if (!API.IsModelValid(model))
                return;

            var start = DateTime.Now;

            while (!API.HasModelLoaded(model))
            {
                API.RequestModel(model);
                await BaseScript.Delay(10);
            }

            Telemetry.AddData("request_model", start);
        }

        public static async Task RequestCollision(uint model)
        {
            if (!API.IsModelValid(model))
                return;

            var start = DateTime.Now;
            var timeout = start + TimeSpan.FromSeconds(0.5);

            while (!API.HasCollisionForModelLoaded(model) && DateTime.Now < timeout)
            {
                API.RequestCollisionForModel(model);
                await BaseScript.Delay(10);
            }

            Telemetry.AddData("request_collision", start);
        }

        public static async Task RequestPtfxAsset(string name)
        {
            var start = DateTime.Now;

            while (!API.HasNamedPtfxAssetLoaded(name))
            {
                API.RequestNamedPtfxAsset(name);
                await BaseScript.Delay(10);
            }

            Telemetry.AddData("request_ptfx_asset", start);
        }

        public static async Task NetworkRequestControl(int entity, int timeoutSeconds = 1)
        {
            if (!API.DoesEntityExist(entity))
                return;

            var start = DateTime.Now;
            var timeout = start + TimeSpan.FromSeconds(timeoutSeconds);

            while (!API.NetworkHasControlOfEntity(entity) && DateTime.Now < timeout)
            {
                API.NetworkRequestControlOfEntity(entity);
                await BaseScript.Delay(100);
            }

            Telemetry.AddData("network_request_control", start);
        }

        public static int GetPlayerPedOrVehicle()
        {
            var player = API.GetPlayerPed(-1);
            return API.IsPedInAnyVehicle(player, false) ? API.GetVehiclePedIsIn(player, false) : player;
        }

        public static bool GetClosestEntity(IEnumerable<int> entities, out int closest)
        {
            closest = -1;
            bool found = false;
            float minDist = float.MaxValue;
            var coords = API.GetEntityCoords(API.GetPlayerPed(-1), false);

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

        public static void GetEntityMinMaxZ(int entity, out float minZ, out float maxZ)
        {
            var model = (uint)API.GetEntityModel(entity);
            var min = Vector3.Zero;
            var max = Vector3.Zero;
            API.GetModelDimensions(model, ref min, ref max);

            minZ = min.Z;
            maxZ = max.Z;
        }

        public static float GetEntityHeight(int entity)
        {
            GetEntityMinMaxZ(entity, out float minZ, out float maxZ);
            return maxZ - minZ;
        }

        public static float GetEntityHeightAboveGround(int entity)
        {
            var coords = API.GetEntityCoords(entity, false);
            float wheight = 0f;

            if (API.GetWaterHeightNoWaves(coords.X, coords.Y, coords.Z, ref wheight))
                return coords.Z - wheight;
            else
                return API.GetEntityHeightAboveGround(entity);
        }

        public static Vector3 GetEntityTopCoords(int entity)
        {
            var coords = API.GetEntityCoords(entity, API.IsEntityAPed(entity));
            GetEntityMinMaxZ(entity, out float minZ, out float maxZ);
            coords.Z += maxZ;
            return coords;
        }

        public static void ApplyTorque(int entity, float x, float y, bool scaleLeverage = true)
        {
            var min = -Vector3.One;
            var max = Vector3.One;

            if (scaleLeverage)
            {
                var model = (uint)API.GetEntityModel(entity);
                var isHeli = API.IsThisModelAHeli(model);
                API.GetModelDimensions(model, ref min, ref max);
            }

            if (x != 0) // up-down
            {
                API.ApplyForceToEntity(entity, 1, 0f, 0f, -x, 0f, max.Y, 0f, -1, true, true, true, false, false);
                API.ApplyForceToEntity(entity, 1, 0f, 0f, x, 0f, min.Y, 0f, -1, true, true, true, false, false);
            }

            if (y != 0) // left-right
            {
                API.ApplyForceToEntity(entity, 1, 0f, 0f, y / 2, max.X, 0f, 0f, -1, true, true, true, false, false);
                API.ApplyForceToEntity(entity, 1, 0f, 0f, -y / 2, min.X, 0f, 0f, -1, true, true, true, false, false);
            }
        }

        public static bool EnsurePlayerIsInVehicle(out int player, out int vehicle, bool notification = true)
        {
            vehicle = 0;
            player = API.GetPlayerPed(-1);
            if (!API.IsPedInAnyVehicle(player, true))
            {
                if (notification)
                    Notification("Player is not in a vehicle");

                return false;
            }

            vehicle = API.GetVehiclePedIsIn(player, false);
            return true;
        }

        public static bool EnsurePlayerIsVehicleDriver(out int player, out int vehicle, bool notification = true)
        {
            if (!EnsurePlayerIsInVehicle(out player, out vehicle, notification))
                return false;

            var driver = API.GetPedInVehicleSeat(vehicle, -1);

            if (driver != player && !Autopilot.IsOwnedAutopilot(driver))
            {
                if (notification)
                    Notification("Player is not the driver of this vehicle");

                return false;
            }

            return true;
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
            var headingRad = heading * (Math.PI / 180f);
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

        public static Vector3 GetRandomSpawnCoordsInRange(Vector3 center, float minRange, float maxRange, out float heading)
        {
            heading = API.GetRandomFloatInRange(0f, 360f);
            var headingRad = heading * (Math.PI / 180f);
            var distance = API.GetRandomFloatInRange(minRange, maxRange);
            var offset = new Vector3(-(float)Math.Sin(headingRad), (float)Math.Cos(headingRad), 0) * distance;

            float groundZ = 0f;
            if (API.GetGroundZFor_3dCoord(center.X + offset.X, center.Y + offset.Y, center.Z, ref groundZ, false))
                offset.Z = groundZ - center.Z;

            return center + offset;
        }
    }
}
