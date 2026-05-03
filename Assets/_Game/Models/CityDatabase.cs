using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    /// <summary>
    /// CityDatabase.cs — Base de datos estática de ciudades.
    /// 
    /// Contiene el diccionario AllCities accesible globalmente.
    /// </summary>
    public static class CityDatabase
    {
        public static Dictionary<string, WorldCity> AllCities { get; private set; } = new Dictionary<string, WorldCity>();

        /// <summary>
        /// Inicializa la base de datos con las ciudades predefinidas.
        /// </summary>
        public static void Initialize()
        {
            AllCities.Clear();

            // Lista de ciudades principales
            var cities = new List<WorldCity>
            {
                new WorldCity("buenos_aires", "Buenos Aires", "Argentina", "South America", -34.6f, -58.4f, true, true, true, true, 0, 60),
                new WorldCity("miami", "Miami", "Estados Unidos", "North America", 25.8f, -80.2f, true, true, true, true, 1, 80),
                new WorldCity("shanghai", "Shanghai", "China", "Asia", 31.2f, 121.5f, true, true, true, true, 2, 95),
                new WorldCity("rotterdam", "Rotterdam", "Países Bajos", "Europe", 51.9f, 4.5f, true, true, true, true, 2, 85),
                new WorldCity("dubai", "Dubai", "Emiratos Árabes", "Middle East", 25.2f, 55.3f, true, true, false, true, 3, 75),
                new WorldCity("hamburg", "Hamburg", "Alemania", "Europe", 53.6f, 10.0f, true, true, false, true, 3, 80),
                new WorldCity("sao_paulo", "São Paulo", "Brasil", "South America", -23.5f, -46.6f, true, true, true, true, 1, 70),
                new WorldCity("los_angeles", "Los Ángeles", "Estados Unidos", "North America", 34.0f, -118.2f, true, true, true, true, 4, 85),
                new WorldCity("antwerp", "Amberes", "Bélgica", "Europe", 51.2f, 4.4f, true, true, false, false, 5, 82),
                new WorldCity("copenhagen", "Copenhague", "Dinamarca", "Europe", 55.7f, 12.6f, true, false, false, false, 6, 52),
            };

            foreach (var city in cities)
            {
                AllCities[city.Id] = city;
            }

            Debug.Log($"[CityDatabase] Inicializada con {AllCities.Count} ciudades");
        }

        /// <summary>
        /// Obtiene una ciudad por su ID.
        /// </summary>
        public static WorldCity GetCity(string id)
        {
            AllCities.TryGetValue(id, out WorldCity city);
            return city;
        }

        /// <summary>
        /// Calcula la distancia en km entre dos ciudades usando la fórmula haversine.
        /// </summary>
        public static float GetDistance(string cityId1, string cityId2)
        {
            var c1 = GetCity(cityId1);
            var c2 = GetCity(cityId2);
            
            if (c1 == null || c2 == null) return 0f;

            float r = 6371f; // Radio de la Tierra en km
            float dLat = (c2.Latitude - c1.Latitude) * Mathf.PI / 180f;
            float dLon = (c2.Longitude - c1.Longitude) * Mathf.PI / 180f;

            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                      Mathf.Cos(c1.Latitude * Mathf.PI / 180f) * Mathf.Cos(c2.Latitude * Mathf.PI / 180f) *
                      Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

            float c = 2f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
            return r * c;
        }
    }
}
