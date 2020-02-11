using System;
using System.Collections.Generic;
using System.Linq;
using IoTRynningeasenFACore.Models;
using Newtonsoft.Json.Linq;

namespace IoTRynningeasenFACore
{
    public class SensorsFunctions
    {
        public static Dictionary<string, Sensor<State>[]> GetSensors(string rawJson)
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

        public static WeightedSensor CreateWeightedSensor(Sensor<State> sensor, DateTime baseLine)
        {
            return new WeightedSensor(
                sensor.type,
                sensor.state.value,
                Math.Max(0, (int)Math.Truncate(60 - (baseLine - sensor.state.lastupdated).TotalMinutes)),
                sensor.state.lastupdated);
        }

        public static decimal CalculateWeightedAvg(IEnumerable<WeightedSensor> weightedSensors)
        {
            decimal totalWeight = weightedSensors.Sum(x => x.Weight);
            decimal total = weightedSensors.Sum(x => x.Weight * x.Value);

            var weightedAvg = totalWeight == 0 ? Decimal.MinValue : Math.Round(total / totalWeight, 3);
            return weightedAvg;
        }

        /// https://www.smhi.se/kunskapsbanken/hur-mats-lufttryck-1.23830
        /// https://dotnetfiddle.net/6P3tS8
        public static double CalculateQFF(
            double sensorPressure /*sensor pressure*/,
            double temperature /*temperture*/,
            double degLatitude /*latitude in deg*/,
            double heightAboveSeaLevel /*height above sea level*/)
        {
            //var heightAboveSeaLevel = 30;
            //var temperature = 3.1;
            //var degLatitude = 59.284611;
            //var sensorPressure = 970.3;

            var t1 = temperature < -7 ? 0.500 * temperature + 275.0 : (temperature < 2 ? 0.535 * temperature + 275.6 : 1.07 * temperature + 274.5);
            var b = 0.034163 * (1 - 0.0026373 * Math.Cos(2 * degLatitude));

            return sensorPressure * Math.Exp(heightAboveSeaLevel * b / t1);
        }
    }
}