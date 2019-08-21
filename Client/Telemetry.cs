using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    using PlayerTelemetry = Dictionary<string, List<string>>;

    public class Telemetry : BaseScript
    {
        private static readonly Dictionary<string, List<TimeSpan>> _localData = new Dictionary<string, List<TimeSpan>>();
        private static readonly Dictionary<int, PlayerTelemetry> _playerTelemetries = new Dictionary<int, PlayerTelemetry>();

        public Telemetry()
        {
            EventHandlers["PocceMod:Telemetry"] += new Action<int, dynamic>(ReceiveTelemetry);

            Tick += Update;
        }

        public static IEnumerable<KeyValuePair<int, PlayerTelemetry>> Entries
        {
            get { return _playerTelemetries; }
        }

        public static Func<Task> Wrap(string feature, Func<Task> func)
        {
            return () =>
            {
                var start = DateTime.Now;
                return func().ContinueWith(_ => AddData(feature, start));
            };
        }

        private static void AddData(string feature, DateTime start) => AddData(feature, DateTime.Now - start);

        public static void AddData(string feature, TimeSpan timespan)
        {
            if (_localData.TryGetValue(feature, out List<TimeSpan> times))
                times.Add(timespan);
            else
                _localData.Add(feature, new List<TimeSpan> { timespan });
        }

        private static void MinMaxAvg(List<TimeSpan> times, out int min, out int max, out int avg)
        {
            if (times.Count == 0)
            {
                min = 0;
                max = 0;
                avg = 0;
                return;
            }

            min = int.MaxValue;
            max = int.MinValue;
            int sum = 0;
            
            foreach (var time in times)
            {
                int ms = (int)time.TotalMilliseconds;
                sum += ms;

                if (ms < min)
                    min = ms;
                else if (ms > max)
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

        private static dynamic ComposeAndClear()
        {
            IDictionary<string, object> result = new ExpandoObject();

            foreach (var pair in _localData)
            {
                MinMaxAvg(pair.Value, out int min, out int max, out int avg);

                var values = new string[] { "calls: " + pair.Value.Count, "min: " + min, "max: " + max, "avg: " + avg };
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
