﻿using CitizenFX.Core;
using CitizenFX.Core.Native;
using PocceMod.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Events : BaseScript
    {
        private const string SpeakerRadioDecor = "POCCE_SPEAKER_RADIO_DECOR";
        private static readonly List<int> _speakers = new List<int>();

        public Events()
        {
            API.DecorRegister(SpeakerRadioDecor, 3);

            Tick += Telemetry.Wrap("speakers", UpdateSpeakers);
        }

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
                API.DecorSetInt(prop, SpeakerRadioDecor, station);
                // cleanup later in update
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

        public static Task PoccePartyRandom()
        {
            float radius = API.GetRandomFloatInRange(5f, 10f);
            int speakers = API.GetRandomIntInRange(2, 7);
            int peds = API.GetRandomIntInRange(10, 20);
            int balloons = API.GetRandomIntInRange(1, 10);
            int booze = API.GetRandomIntInRange(10, 20);
            return PocceParty(radius, speakers, peds, balloons, booze);
        }

        public static async Task PocceRiot(bool useWeapons)
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

        public static async Task PedRiot(bool useWeapons)
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
                }

                await Delay(1000); // let them get out of vehicles
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

        private static Task UpdateSpeakers()
        {
            foreach (var speaker in _speakers.ToArray())
            {
                if (!API.DoesEntityExist(speaker))
                    _speakers.Remove(speaker);
            }

            var newSpeakers = Props.Get().Where(prop => API.DecorExistOn(prop, SpeakerRadioDecor) && !_speakers.Contains(prop));
            foreach (var speaker in newSpeakers)
            {
                var station = API.DecorGetInt(speaker, SpeakerRadioDecor);
                //API.N_0x651d3228960d08af("SE_Script_Placed_Prop_Emitter_Boombox", prop);
                Function.Call((Hash)0x651d3228960d08af, "SE_Script_Placed_Prop_Emitter_Boombox", speaker);
                API.SetEmitterRadioStation("SE_Script_Placed_Prop_Emitter_Boombox", API.GetRadioStationName(station));
                API.SetStaticEmitterEnabled("SE_Script_Placed_Prop_Emitter_Boombox", true);
                _speakers.Add(speaker);

                var tmp_prop = speaker;
                API.SetEntityAsNoLongerNeeded(ref tmp_prop);
            }

            return Delay(1000);
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
