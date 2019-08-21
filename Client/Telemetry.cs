using CitizenFX.Core;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    using PlayerTelemetry = Dictionary<string, List<string>>;

    public class Telemetry : BaseScript
    {
        private static readonly bool Enabled;
        private static readonly Dictionary<string, List<TimeSpan>> _localData = new Dictionary<string, List<TimeSpan>>();
        private static readonly Dictionary<int, PlayerTelemetry> _playerTelemetries = new Dictionary<int, PlayerTelemetry>();

        static Telemetry()
        {
            Enabled = Config.GetConfigBool("EnableTelemetry");
        }

        public Telemetry()
        {
            EventHandlers["PocceMod:Telemetry"] += new Action<int, dynamic>(ReceiveTelemetry);

            if (Enabled)
                Tick += Wrap("telemetry", Update);
        }

        public static IEnumerable<KeyValuePair<int, PlayerTelemetry>> Entries
        {
            get { return _playerTelemetries; }
        }

        public static Func<Task> Wrap(string feature, Func<Task> func)
        {
            if (!Enabled)
                return func;

            return () =>
            {
                var start = DateTime.Now;
                var result = func();
                AddData(feature, start);
                return result;
            };
        }

        public static void AddData(string feature, DateTime start) => AddData(feature, DateTime.Now - start);

        public static void AddData(string feature, TimeSpan timespan)
        {
            if (!Enabled)
                return;

            if (_localData.TryGetValue(feature, out List<TimeSpan> times))
                times.Add(timespan);
            else
                _localData.Add(feature, new List<TimeSpan> { timespan });
        }

        private static void MinMaxAvgSum(List<TimeSpan> times, out float min, out float max, out float avg, out float sum)
        {
            if (times.Count == 0)
            {
                min = 0;
                max = 0;
                avg = 0;
                sum = 0;
                return;
            }

            min = float.MaxValue;
            max = float.MinValue;
            sum = 0;
            
            foreach (var time in times)
            {
                var ms = (float)time.TotalMilliseconds;
                sum += ms;

                if (ms < min)
                    min = ms;

                if (ms > max)
                    max = ms;
            }

            avg = sum / times.Count;
        }

        private static void ReceiveTelemetry(int sourcePlayer, dynamic data)
        {
            var playerTelemetry = new PlayerTelemetry();

            foreach (KeyValuePair<string, dynamic> pair in data)
            {
                List<object> values = pair.Value;
                playerTelemetry.Add(pair.Key, values.Cast<string>().ToList());
            }

            _playerTelemetries[sourcePlayer] = playerTelemetry;
        }

        private static IDictionary<string, object> ComposeAndClear()
        {
            IDictionary<string, object> result = new Dictionary<string, object>();

            foreach (var pair in _localData)
            {
                MinMaxAvgSum(pair.Value, out float min, out float max, out float avg, out float sum);

                var values = new string[] {
                    "calls: " + pair.Value.Count,
                    "min: " + min + "ms",
                    "max: " + max + "ms",
                    "avg: " + avg + "ms",
                    "sum: " + sum + "ms"
                };
                result.Add(pair.Key, values);

                pair.Value.Clear();
            }

            return result;
        }

        private static async Task Update()
        {
            await Delay(60000); // once per minute

            TriggerServerEvent("PocceMod:Telemetry", ComposeAndClear());
        }
    }
}
