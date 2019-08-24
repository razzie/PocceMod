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
        private class Data
        {
            public int Calls { get; private set; }
            public double Min { get; private set; }
            public double Max { get; private set; }
            public double Sum { get; private set; }
            public double Avg { get { return Sum / Calls; } }

            public Data(TimeSpan time)
            {
                Add(time);
            }

            public void Add(TimeSpan time)
            {
                var ms = time.TotalMilliseconds;

                Sum += ms;
                Calls++;

                if (Calls == 1)
                {
                    Min = ms;
                    Max = ms;
                }
                else
                {
                    if (ms < Min)
                        Min = ms;

                    if (ms > Max)
                        Max = ms;
                }
            }

            public void Reset()
            {
                Calls = 0;
                Min = 0;
                Max = 0;
                Sum = 0;
            }
        }

        private static readonly bool Enabled;
        private static readonly Dictionary<string, Data> _localData = new Dictionary<string, Data>();
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

        public static IEnumerable<KeyValuePair<int, PlayerTelemetry>> PlayerData
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

            if (_localData.TryGetValue(feature, out Data data))
                data.Add(timespan);
            else
                _localData.Add(feature, new Data(timespan));
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
                var data = pair.Value;
                var values = new string[] {
                    "calls: " + data.Calls,
                    "min: " + Math.Round(data.Min, 2) + "ms",
                    "max: " + Math.Round(data.Max, 2) + "ms",
                    "avg: " + Math.Round(data.Avg, 2) + "ms",
                    "sum: " + Math.Round(data.Sum, 2) + "ms"
                };
                result.Add(pair.Key, values);

                data.Reset();
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
