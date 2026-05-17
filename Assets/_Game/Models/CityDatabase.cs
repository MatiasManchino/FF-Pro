using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    /// <summary>
    /// Base de datos de ciudades logísticas del juego.
    /// Los IDs y coordenadas están alineados con los CityMarker spawneados en GameBootstrapper.
    /// </summary>
    public static class CityDatabase
    {
        public static Dictionary<string, WorldCity> AllCities { get; private set; }

        public static void Initialize()
        {
            if (AllCities != null) return; // Ya inicializado

            AllCities = new Dictionary<string, WorldCity>();

            // Coordenadas idénticas a las de GameBootstrapper.SpawnCities()
            // Tier 0 = desbloqueada al inicio

            // América del Sur
            Add("buenos_aires", "Buenos Aires", "Argentina",       "South America", -38.46f, -58.38f, true,  true,  true,  true,  true,  0,     0,  60);
            Add("sao_paulo",    "São Paulo",     "Brasil",          "South America", -24.90f, -46.63f, true,  true,  true,  true,  false, 5000,  1,  70);
            Add("valparaiso",   "Valparaíso",    "Chile",           "South America", -36.64f, -71.63f, true,  false, true,  false, false, 6000,  2,  40);
            Add("cartagena",    "Cartagena",     "Colombia",        "South America",  10.36f, -74.51f, true,  true,  false, false, false, 8000,  3,  50);

            // América del Norte y Central
            Add("miami",        "Miami",         "Estados Unidos",  "North America",  29.71f, -79.98f, true,  true,  true,  true,  false, 8000,  1,  80);
            Add("los_angeles",  "Los Ángeles",   "Estados Unidos",  "North America",  39.09f,-118.24f, true,  true,  true,  true,  false, 15000, 4,  85);
            Add("new_york",     "New York",      "Estados Unidos",  "North America",  46.57f, -74.00f, true,  true,  true,  true,  false, 12000, 3,  90);
            Add("houston",      "Houston",       "Estados Unidos",  "North America",  34.22f, -95.37f, true,  true,  true,  false, false, 10000, 3,  65);
            Add("panama",       "Panamá",        "Panamá",          "North America",  10.78f, -79.50f, true,  true,  false, true,  false, 7000,  2,  55);

            // Europa Occidental
            Add("rotterdam",    "Rotterdam",     "Países Bajos",    "Europe",         59.23f,   4.48f, true,  true,  true,  true,  false, 10000, 2,  85);
            Add("hamburg",      "Hamburgo",      "Alemania",        "Europe",         61.07f,   9.99f, true,  true,  true,  true,  false, 12000, 3,  80);
            Add("antwerp",      "Amberes",       "Bélgica",         "Europe",         58.44f,   4.40f, true,  true,  true,  false, false, 18000, 5,  82);
            Add("london",       "London",        "Reino Unido",     "Europe",         58.77f,  -0.13f, true,  true,  false, true,  false, 13000, 3,  88);
            Add("barcelona",    "Barcelona",     "España",          "Europe",         47.34f,   2.16f, true,  true,  true,  false, false, 11000, 4,  70);
            Add("marseille",    "Marsella",      "Francia",         "Europe",         49.50f,   5.37f, true,  true,  true,  false, false, 11000, 4,  65);

            // África y Mediterráneo
            Add("port_said",    "Port Said",     "Egipto",          "Africa",         35.91f,  32.28f, true,  false, false, true,  false, 9000,  3,  60);
            Add("casablanca",   "Casablanca",    "Marruecos",       "Africa",         38.55f,  -7.59f, true,  true,  false, false, false, 9000,  4,  55);

            // Medio Oriente
            Add("dubai",        "Dubái",         "Emiratos Árabes", "Middle East",    29.17f,  55.30f, true,  true,  false, true,  false, 13000, 3,  75);
            Add("jeddah",       "Jeddah",        "Arabia Saudita",  "Middle East",    24.93f,  39.17f, true,  true,  false, false, false, 11000, 4,  60);

            // Asia del Sur
            Add("mumbai",       "Mumbai",        "India",           "Asia",           22.17f,  72.88f, true,  true,  true,  true,  false, 10000, 3,  80);
            Add("singapore",    "Singapur",      "Singapur",        "Asia",            2.14f, 103.82f, true,  true,  false, true,  false, 10000, 2,  90);

            // Asia del Este
            Add("shanghai",     "Shanghái",      "China",           "Asia",           35.88f, 121.47f, true,  true,  true,  true,  false, 10000, 2,  95);
            Add("hong_kong",    "Hong Kong",     "China",           "Asia",           25.82f, 114.17f, true,  true,  false, true,  false, 12000, 3,  88);
            Add("busan",        "Busan",         "Corea del Sur",   "Asia",           40.24f, 129.04f, true,  true,  false, false, false, 13000, 4,  70);
            Add("tokyo",        "Tokio",         "Japón",           "Asia",           40.90f, 139.69f, true,  true,  false, true,  false, 15000, 5,  85);

            // Oceanía
            Add("sydney",       "Sídney",        "Australia",       "Oceania",       -37.62f, 151.21f, true,  true,  true,  true,  false, 18000, 6,  75);
        }

        private static void Add(string id, string name, string country, string continent,
                                float lat, float lon,
                                bool port, bool airport, bool land, bool hub,
                                bool unlocked, int unlockCost, int tier, int popularity)
        {
            AllCities[id] = new WorldCity(id, name, country, continent, lat, lon,
                                          port, airport, land, hub,
                                          unlocked, unlockCost, tier, popularity);
        }

        public static WorldCity GetCity(string id)
        {
            if (AllCities == null) Initialize();
            AllCities.TryGetValue(id, out WorldCity city);
            return city;
        }

        public static float GetDistance(string cityId1, string cityId2)
        {
            WorldCity a = GetCity(cityId1);
            WorldCity b = GetCity(cityId2);
            if (a == null || b == null)
            {
                Debug.LogWarning($"[CityDatabase] Ciudad no encontrada: '{cityId1}' o '{cityId2}'");
                return 0f;
            }
            return a.DistanceTo(b);
        }

        public static List<WorldCity> GetUnlockedCities()
        {
            var result = new List<WorldCity>();
            if (AllCities == null) Initialize();
            foreach (var city in AllCities.Values)
                if (city.IsUnlocked) result.Add(city);
            return result;
        }

        /// <summary>
        /// Busca una ciudad por su nombre de display (para conectar con CityMarker.cityName).
        /// </summary>
        public static WorldCity GetCityByDisplayName(string displayName)
        {
            if (AllCities == null) Initialize();
            foreach (var city in AllCities.Values)
                if (city.DisplayName.Equals(displayName, System.StringComparison.OrdinalIgnoreCase))
                    return city;
            return null;
        }
    }
}
