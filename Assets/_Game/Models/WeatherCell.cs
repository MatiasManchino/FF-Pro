using System;

namespace FreightForwarder.Models
{
    [Serializable]
    public class WeatherCell
    {
        public float cloud;    // 0..1
        public float storm;    // 0..1
        public float wind;     // 0..1
        public float cyclone;  // 0..1

        public float noiseSeed;
        public bool  isStorming;
        public bool  isCyclone;
    }
}
