using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DaylightData", menuName = "EarthVis/Daylight Database")]
public class DaylightData : ScriptableObject
{
    [System.Serializable]
    public class CityDaylightInfo
    {
        public string cityName;
        public float latitude;
        public float longitude;
        public float[] monthlyDaylightHours = new float[12]; // Ene-Dic
    }
    
    public List<CityDaylightInfo> cities = new List<CityDaylightInfo>();
    
    // Helper para obtener horas de luz esperadas
    public float GetExpectedDaylight(string cityName, int month)
    {
        var city = cities.Find(c => c.cityName == cityName);
        if (city != null && month >= 1 && month <= 12)
            return city.monthlyDaylightHours[month - 1];
        return -1f;
    }
}
