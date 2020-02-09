using System;
using System.Collections.Generic;
using System.Linq;
using IoTRynningeasenFACore.Models;
using Newtonsoft.Json.Linq;

namespace IoTRynningeasenFACore
{
    public class SensorsFunctions
    {
        public Dictionary<string, Sensor<State>[]> GetSensors(string rawJson)
        {
            var sensors = JToken.Parse(rawJson)
                .Children().Values().Where(x => x["type"].ToString().StartsWith("ZHA"))
                .GroupBy(x => x["type"].ToString())
                .ToDictionary(
                    x => x.Key,
                    x => x.Key switch
                    {
                        "ZHAPressure" => x.Select(o => CreateSensor(o, "pressure")).ToArray(),
                        "ZHATemperature" => x.Select(o => CreateSensor(o, "temperature")).ToArray(),
                        "ZHAHumidity" => x.Select(o => CreateSensor(o, "humidity")).ToArray(),
                        _ => null
                    });

            return sensors;
        }

        private static Sensor<State> CreateSensor(JToken o, string key)
        {
            o["state"]["value"] = o["state"][key];
            return o.ToObject<Sensor<State>>();
        }

        public WeightedSensor CreateWeightedSensor(Sensor<State> sensor, DateTime baseLine)
        {
            return new WeightedSensor(
                sensor.type,
                sensor.state.value,
                Math.Max(0, (int)Math.Truncate(60 - (baseLine - sensor.state.lastupdated).TotalMinutes)),
                sensor.state.lastupdated);
        }

        public decimal CalculateWeightedAvg(IEnumerable<WeightedSensor> weightedSensors)
        {
            decimal totalWeight = weightedSensors.Sum(x => x.Weight);
            decimal total = weightedSensors.Sum(x => x.Weight * x.Value);

            var weightedAvg = totalWeight == 0 ? Decimal.MinValue : Math.Round(total / totalWeight, 3);
            return weightedAvg;
        }
    }
}