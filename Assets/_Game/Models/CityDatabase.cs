using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{

    // Base de datos de ciudades logísticas del juego.
    // Coordenadas alineadas con SpawnCity() en GameBootstrapper
    // (sistema del mapa: lon_mapa = lon_real + 180°, normalizado a ±180°).

    public static class CityDatabase
    {
// Devuelve la all ciudades
        public static Dictionary<string, WorldCity> AllCities { get; private set; }

// Inicializa ialize.
        public static void Initialize()
        {
            if (AllCities != null) return;

            AllCities = new Dictionary<string, WorldCity>();

            // América del Sur
            Add("buenos_aires", "Buenos Aires", "Argentina",       "South America",  -34.61f,  121.62f, true,  true,  true,  true,  true,  0,     0,  60);
            Add("sao_paulo",    "São Paulo",     "Brasil",          "South America",  -23.55f,  133.37f, true,  true,  true,  true,  false, 5000,  1,  70);
            Add("valparaiso",   "Valparaíso",    "Chile",           "South America",  -33.05f,  108.37f, true,  false, true,  false, false, 6000,  2,  40);
            Add("cartagena",    "Cartagena",     "Colombia",        "South America",   10.39f,  104.49f, true,  true,  false, false, false, 8000,  3,  50);

            // América del Norte y Central
            Add("miami",        "Miami",         "Estados Unidos",  "North America",   25.77f,   99.81f, true,  true,  true,  true,  false, 8000,  1,  80);
            Add("los_angeles",  "Los Ángeles",   "Estados Unidos",  "North America",   34.05f,   61.76f, true,  true,  true,  true,  false, 15000, 4,  85);
            Add("new_york",     "New York",      "Estados Unidos",  "North America",   40.71f,  105.99f, true,  true,  true,  true,  false, 12000, 3,  90);
            Add("houston",      "Houston",       "Estados Unidos",  "North America",   29.76f,   84.63f, true,  true,  true,  false, false, 10000, 3,  65);
            Add("panama",       "Panamá",        "Panamá",          "North America",    8.99f,  100.48f, true,  true,  false, true,  false, 7000,  2,  55);

            // Europa Occidental
            Add("rotterdam",    "Rotterdam",     "Países Bajos",    "Europe",          51.92f, -175.52f, true,  true,  true,  true,  false, 10000, 2,  85);
            Add("hamburg",      "Hamburgo",      "Alemania",        "Europe",          53.55f, -170.01f, true,  true,  true,  true,  false, 12000, 3,  80);
            Add("antwerp",      "Amberes",       "Bélgica",         "Europe",          51.22f, -175.60f, true,  true,  true,  false, false, 18000, 5,  82);
            Add("london",       "London",        "Reino Unido",     "Europe",          51.51f,  179.87f, true,  true,  false, true,  false, 13000, 3,  88);
            Add("barcelona",    "Barcelona",     "España",          "Europe",          41.39f, -177.84f, true,  true,  true,  false, false, 11000, 4,  70);
            Add("marseille",    "Marsella",      "Francia",         "Europe",          43.30f, -174.63f, true,  true,  true,  false, false, 11000, 4,  65);

            // África y Mediterráneo
            Add("port_said",    "Port Said",     "Egipto",          "Africa",          31.26f, -147.72f, true,  false, false, true,  false, 9000,  3,  60);
            Add("casablanca",   "Casablanca",    "Marruecos",       "Africa",          33.59f,  172.38f, true,  true,  false, false, false, 9000,  4,  55);

            // Medio Oriente
            Add("dubai",        "Dubái",         "Emiratos Árabes", "Middle East",     25.20f, -124.73f, true,  true,  false, true,  false, 13000, 3,  75);
            Add("jeddah",       "Jeddah",        "Arabia Saudita",  "Middle East",     21.49f, -140.83f, true,  true,  false, false, false, 11000, 4,  60);

            // Asia del Sur
            Add("mumbai",       "Mumbai",        "India",           "Asia",            19.08f, -107.12f, true,  true,  true,  true,  false, 10000, 3,  80);
            Add("singapore",    "Singapur",      "Singapur",        "Asia",             1.35f,  -76.18f, true,  true,  false, true,  false, 10000, 2,  90);

            // Asia del Este
            Add("shanghai",     "Shanghái",      "China",           "Asia",            31.23f,  -58.53f, true,  true,  true,  true,  false, 10000, 2,  95);
            Add("hong_kong",    "Hong Kong",     "China",           "Asia",            22.32f,  -65.83f, true,  true,  false, true,  false, 12000, 3,  88);
            Add("busan",        "Busan",         "Corea del Sur",   "Asia",            35.10f,  -50.96f, true,  true,  false, false, false, 13000, 4,  70);
            Add("tokyo",        "Tokio",         "Japón",           "Asia",            35.68f,  -40.31f, true,  true,  false, true,  false, 15000, 5,  85);

            // Oceanía
            Add("sydney",       "Sídney",        "Australia",       "Oceania",        -33.87f,  -28.79f, true,  true,  true,  true,  false, 18000, 6,  75);
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

// Obtiene ciudad
        public static WorldCity GetCity(string id)
        {
            if (AllCities == null) Initialize();
            AllCities.TryGetValue(id, out WorldCity city);
            return city;
        }

// Obtiene distancia
        public static float GetDistance(string cityId1, string cityId2)
        {
            WorldCity a = GetCity(cityId1);
            WorldCity b = GetCity(cityId2);
            if (a == null || b == null)
            {
                // Ciudad sin datos en CityDatabase: devolver 0 sin warning (el marítimo no usa esta distancia).
                return 0f;
            }
            return a.DistanceTo(b);
        }

// Obtiene unlocked ciudades
        public static List<WorldCity> GetUnlockedCities()
        {
            var result = new List<WorldCity>();
            if (AllCities == null) Initialize();
// Foreach
            foreach (var city in AllCities.Values)
                if (city.IsUnlocked) result.Add(city);
            return result;
        }

// Obtiene ciudad by muestra nombre
        public static WorldCity GetCityByDisplayName(string displayName)
        {
            if (AllCities == null) Initialize();
// Foreach
            foreach (var city in AllCities.Values)
                if (city.DisplayName.Equals(displayName, System.StringComparison.OrdinalIgnoreCase))
                    return city;
            return null;
        }


        // Nombre legible y capitalizado de una ciudad/puerto a partir de su id.
        // Usa el DisplayName canónico (con acentos) si existe; si no, capitaliza el id.

        public static string DisplayNameOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            var city = GetCity(id);
            if (city != null && !string.IsNullOrEmpty(city.DisplayName)) return city.DisplayName;
            return ToTitleCase(id.Replace('_', ' '));
        }

// Gestiona to title case.
        public static string ToTitleCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var parts = s.Split(' ');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }
    }
}