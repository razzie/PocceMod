using CitizenFX.Core;
using PocceMod.Shared;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace PocceMod.Client
{
    public class Telemetry : BaseScript
    {
        private static readonly bool Enabled = Config.GetConfigBool("EnableTelemetry");
        private static readonly List<Measurement> _measurements = new List<Measurement>();

        public delegate void TelemetryReceivedDelegate(int sourcePlayer, Measurement measurement);
        public static event TelemetryReceivedDelegate TelemetryReceived;

        public Telemetry()
        {
            EventHandlers["PocceMod:RequestTelemetry"] += new Action<int, int>(NetRequestTelemetry);
            EventHandlers["PocceMod:Telemetry"] += new Action<int, dynamic>(NetTelemetry);

            if (Enabled)
                Tick += Wrap("telemetry", Update);
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

            foreach (var measurement in _measurements)
            {
                measurement.AddData(feature, timespan);
            }
        }

        public static void Request(int timeoutSec)
        {
            TriggerServerEvent("PocceMod:RequestTelemetry", timeoutSec);
        }

        private static void NetRequestTelemetry(int requestingPlayer, int timeoutSec)
        {
            _measurements.Add(new Measurement(requestingPlayer, timeoutSec));
        }

        private static void NetTelemetry(int sourcePlayer, dynamic data)
        {
            var measurement = Measurement.Deserialize(data);
            TelemetryReceived?.Invoke(sourcePlayer, measurement);
        }

        private static Task Update()
        {
            foreach (var measurement in _measurements.ToArray())
            {
                if (measurement.Timeout < DateTime.Now)
                {
                    TriggerServerEvent("PocceMod:Telemetry", measurement.RequestingPlayer, measurement.Serialize());
                    _measurements.Remove(measurement);
                }
            }

            return Delay(100);
        }

        public class Data
        {
            public string Name { get; }
            public int Calls { get; private set; }
            public double Min { get; private set; }
            public double Max { get; private set; }
            public double Sum { get; private set; }
            public double Avg { get { return Sum / Calls; } }

            public Data(string name)
            {
                Name = name;
            }

            public Data(string name, TimeSpan time)
            {
                Name = name;
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
                    else if (ms > Max)
                        Max = ms;
                }
            }

            public List<string> Items
            {
                get
                {
                    return new List<string> {
                        "calls: " + Calls,
                        "min: " + Math.Round(Min, 2) + "ms",
                        "max: " + Math.Round(Max, 2) + "ms",
                        "avg: " + Math.Round(Avg, 2) + "ms",
                        "sum: " + Math.Round(Sum, 2) + "ms"
                    };
                }
            }

            public dynamic Serialize()
            {
                dynamic result = new ExpandoObject();
                result.Name = Name;
                result.Calls = Calls;
                result.Min = Min;
                result.Max = Max;
                result.Sum = Sum;
                return result;
            }

            public static Data Deserialize(dynamic data)
            {
                return new Data(data.Name)
                {
                    Calls = data.Calls,
                    Min = data.Min,
                    Max = data.Max,
                    Sum = data.Sum
                };
            }
        }

        public class Measurement
        {
            public Data Total { get; }
            public Dictionary<string, Data> FeatureData { get; }
            public DateTime Timeout { get; }
            public int RequestingPlayer { get; }

            private Measurement(Data total, IEnumerable<Data> featureData)
            {
                Total = total;
                FeatureData = featureData.ToDictionary(data => data.Name, data => data);
            }

            public Measurement(int requestingPlayer, int timeoutSec)
            {
                Total = new Data("Total");
                FeatureData = new Dictionary<string, Data>();
                RequestingPlayer = requestingPlayer;
                Timeout = DateTime.Now + TimeSpan.FromSeconds(timeoutSec);
            }

            public void AddData(string feature, DateTime start) => AddData(feature, DateTime.Now - start);

            public void AddData(string feature, TimeSpan timespan)
            {
                Total.Add(timespan);

                if (FeatureData.TryGetValue(feature, out Data data))
                    data.Add(timespan);
                else
                    FeatureData.Add(feature, new Data(feature, timespan));
            }

            public dynamic Serialize()
            {
                dynamic result = new ExpandoObject();
                result.Total = Total.Serialize();
                result.FeatureData = FeatureData.Values.Select(data => data.Serialize());
                return result;
            }

            public static Measurement Deserialize(dynamic data)
            {
                List<dynamic> featureData = data.FeatureData;
                return new Measurement(Data.Deserialize(data.Total), featureData.Select<dynamic, Data>(x => Data.Deserialize(x)));
            }
        }
    }
}
