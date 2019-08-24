using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocceMod.Client.Menus
{
    public static class EventMenu
    {
        public static async Task PocceParty(float radius, int speakers, int peds, int balloons, int booze)
        {
            var center = API.GetEntityCoords(API.GetPlayerPed(-1), true);
            var station = API.GetPlayerRadioStationIndex();

            if (station == 255) // OFF
                station = 19; // RADIO_19_USER

            for (int i = 0; i < speakers; ++i)
            {
                var model = SpeakerList[API.GetRandomIntInRange(0, SpeakerList.Length)];
                var prop = await Props.SpawnInRange(center, model, 1f, radius, false);
                Props.SetSpeaker(prop, station);
            }

            for (int i = 0; i < peds; ++i)
            {
                var ped = await Peds.SpawnInRange(Config.PocceList, center, 1f, radius);
                API.TaskStartScenarioInPlace(ped, "WORLD_HUMAN_PARTYING", 0, true);
                API.SetPedAsNoLongerNeeded(ref ped);
            }

            if (Permission.CanDo(Ability.Balloons))
            {
                for (int i = 0; i < balloons; ++i)
                {
                    var coords = Common.GetRandomSpawnCoordsInRange(center, 1f, radius, out float heading);
                    var balloon = await Props.SpawnBalloons(coords);
                    API.FreezeEntityPosition(balloon, true);
                    API.SetEntityAsNoLongerNeeded(ref balloon);
                }
            }

            for (int i = 0; i < booze; ++i)
            {
                var model = BoozeList[API.GetRandomIntInRange(0, BoozeList.Length)];
                var prop = await Props.SpawnInRange(center, model, 1f, radius, false);
                API.SetEntityAsNoLongerNeeded(ref prop);
            }
        }

        public static Task PocceParty()
        {
            float radius = API.GetRandomFloatInRange(5f, 10f);
            int speakers = API.GetRandomIntInRange(2, 7);
            int peds = API.GetRandomIntInRange(10, 20);
            int balloons = API.GetRandomIntInRange(1, 10);
            int booze = API.GetRandomIntInRange(10, 20);
            return PocceParty(radius, speakers, peds, balloons, booze);
        }

        private static async Task PocceRiot(bool useWeapons)
        {
            var peds = new List<int>();
            var weapons = useWeapons ? Config.WeaponList : null;

            for (int i = 0; i < 4; ++i)
            {
                int ped1 = await Peds.Spawn(Config.PocceList);
                int ped2 = await Peds.Spawn(Config.PocceList);

                peds.Add(ped1);
                peds.Add(ped2);

                await Peds.Arm(ped1, weapons);
                await Peds.Arm(ped2, weapons);

                API.TaskCombatPed(ped1, ped2, 0, 16);
                API.TaskCombatPed(ped2, ped1, 0, 16);
            }

            for (int i = 0; i < 4; ++i)
            {
                int ped = await Peds.Spawn(Config.PocceList);
                peds.Add(ped);
                await Peds.Arm(ped, weapons);
                API.TaskCombatPed(ped, API.GetPlayerPed(-1), 0, 16);
            }

            foreach (int ped in peds)
            {
                int tmp_ped = ped;
                API.SetPedAsNoLongerNeeded(ref tmp_ped);
            }
        }

        public static Task PocceRiot()
        {
            return PocceRiot(false);
        }

        public static Task PocceRiotArmed()
        {
            return PocceRiot(true);
        }

        private static async Task PedRiot(bool useWeapons)
        {
            int i = 0;
            var peds = Peds.Get(Peds.Filter.Dead | Peds.Filter.Players | (useWeapons ? Peds.Filter.Animals : Peds.Filter.None)); // do not include animals when using weapons
            var weapons = useWeapons ? Config.WeaponList : null;

            if (peds.Count < 2)
                return;

            foreach (int ped in peds)
            {
                if (API.IsPedInAnyVehicle(ped, false))
                {
                    var vehicle = API.GetVehiclePedIsIn(ped, false);
                    API.TaskLeaveVehicle(ped, vehicle, 1);

                    while (API.IsPedInVehicle(ped, vehicle, false))
                    {
                        await BaseScript.Delay(100);
                    }
                }

                API.ClearPedTasks(ped);

                await Peds.Arm(ped, weapons);

                int enemyPed;
                if (i % 2 == 0)
                    enemyPed = peds[(i + 1) % peds.Count];
                else if (i == peds.Count - 1)
                    enemyPed = peds[0];
                else
                    enemyPed = peds[i - 1];
                API.TaskCombatPed(ped, enemyPed, 0, 16);

                int tmp_ped = ped; API.SetEntityAsNoLongerNeeded(ref tmp_ped);

                ++i;
            }
        }

        public static Task PedRiot()
        {
            return PedRiot(false);
        }

        public static Task PedRiotArmed()
        {
            return PedRiot(true);
        }

        private static readonly string[] SpeakerList = new string[]
        {
            "prop_speaker_01",
            "prop_speaker_02",
            "prop_speaker_03",
            "prop_speaker_05",
            "prop_speaker_06",
            "prop_speaker_07",
            "prop_speaker_08"
        };
        private static readonly string[] BoozeList = new string[]
        {
            "hei_heist_cs_beer_box",
            "prop_cs_beer_bot_01",
            "prop_cs_beer_bot_02",
            "prop_cs_beer_bot_03",
            "prop_cs_beer_bot_40oz",
            "prop_cs_beer_bot_40oz_02",
            "prop_cs_beer_bot_40oz_03",
            "prop_cs_beer_box",
            "prop_wine_bot_01",
            "prop_wine_bot_02",
            "prop_vodka_bottle",
            "proc_litter_01",
            "proc_litter_02",
            "v_ret_ml_chips1",
            "v_ret_ml_chips2",
            "v_ret_ml_chips3",
            "v_ret_ml_chips4",
            "winerow",
            "vodkarow",
            "spiritsrow",
            "beerrow_local"
        };
    }
}
