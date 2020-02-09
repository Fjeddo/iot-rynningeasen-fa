using System;

namespace IoTRynningeasenFACore.Models
{
    public class WeightedSensor
    {
        public int Value { get;  }
        public int Weight { get;  }
        public string Type { get;  }
        public DateTime LastUpdated { get; }


        public WeightedSensor(string type, int value, int weight, DateTime lastUpdated)
        {
            Type = type;
            Value = value;
            Weight = weight;
            LastUpdated = lastUpdated;
        }
    }
}
