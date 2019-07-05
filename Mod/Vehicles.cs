using CitizenFX.Core;
using CitizenFX.Core.Native;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Mod
{
    public static class Vehicles
    {
        public static List<int> Get(bool includeWithDriver = true, float rangeSquared = 900.0f)
        {
            var vehicles = new List<int>();
            int vehicle = 0;
            int handle = API.FindFirstVehicle(ref vehicle);
            var player = Game.Player.Character.Handle;
            var playerVehicle = API.GetVehiclePedIsIn(player, false);
            var coords = Game.Player.Character.Position;

            if (handle == -1)
                return vehicles;

            do
            {
                var pos = API.GetEntityCoords(vehicle, false);

                if (vehicle == playerVehicle)
                    continue;

                if (!includeWithDriver && !API.IsVehicleSeatFree(vehicle, -1))
                    continue;

                if (coords.DistanceToSquared(pos) > rangeSquared)
                    continue;

                vehicles.Add(vehicle);

            } while (API.FindNextVehicle(handle, ref vehicle));

            API.EndFindVehicle(handle);
            return vehicles;
        }

        public static async Task<int> Spawn(string model)
        {
            var pos = Game.Player.Character.Position;
            var hash = (uint)API.GetHashKey(model);

            if (!API.IsModelValid(hash))
            {
                Hud.Notification(string.Format("Invalid model hash: 0x{0:X8} ({1})", hash, model));
                return -1;
            }

            if (API.IsPedInAnyVehicle(Game.Player.Character.Handle, false))
            {
                Hud.Notification("Player is in a vehicle");
                return -1;
            }

            await Common.RequestModel(hash);
            var vehicle = API.CreateVehicle(hash, pos.X, pos.Y, pos.Z + 1.0f, Game.Player.Character.Heading, true, false);
            Game.Player.Character.SetIntoVehicle(new Vehicle(vehicle), VehicleSeat.Driver);
            return vehicle;
        }
    }
}
