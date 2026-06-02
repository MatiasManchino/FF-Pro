using System;

namespace FreightForwarder.Models
{
    // Representa el clima de UNA celda de la grilla del mundo. El planeta se divide en una
    // cuadrícula y cada celda guarda acá su estado meteorológico.
    // [Serializable] permite guardarla en disco y verla en el inspector de Unity.
    [Serializable]
    public class WeatherCell
    {
        // Valores de 0 a 1 (0 = nada, 1 = máximo) que describen el clima de la celda:
        public float cloud;    // nubosidad
        public float storm;    // intensidad de la tormenta
        public float wind;     // intensidad del viento
        public float cyclone;  // intensidad del ciclón / huracán

        public float noiseSeed;   // semilla de ruido: hace que cada celda varíe de forma distinta
        public bool  isStorming;  // true si en esta celda hay una tormenta activa
        public bool  isCyclone;   // true si en esta celda hay un ciclón activo
    }
}
