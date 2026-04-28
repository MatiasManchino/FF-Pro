using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    /// <summary>
    /// WorldCity.cs — Modelo de una ciudad en el mundo.
    /// 
    /// QUÉ ES UNA PROPERTY EN C#?
    /// { get; set; } es una "property" (propiedad). Es como un campo pero con control.
    /// Puedes leer (get) y escribir (set) desde fuera. Si pones "private set", solo
    /// se puede modificar desde dentro de la clase.
    /// 
    /// QUÉ ES [Serializable]?
    /// Permite que esta clase se guarde en archivos JSON (para el sistema de guardado).
    /// </summary>
    [Serializable]
    public class WorldCity
    {
        // =========================================================================
        // IDENTIFICACIÓN BÁSICA
        // =========================================================================
        
        /// <summary>
        /// ID único de la ciudad (ej: "buenos_aires", "sao_paulo")
        /// Usamos snake_case para consistencia con el prototipo Godot.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Nombre mostrado al jugador (ej: "Buenos Aires")
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// País al que pertenece la ciudad
        /// </summary>
        public string Country { get; set; }
        
        /// <summary>
        /// Continente (ej: "South America", "Europe", "Asia")
        /// </summary>
        public string Continent { get; set; }
        
        // =========================================================================
        // COORDENADAS GEOGRÁFICAS (para el mapa 3D)
        // =========================================================================
        
        /// <summary>
        /// Latitud en grados decimales (ej: -34.6 para Buenos Aires)
        /// Negativo = Sur, Positivo = Norte
        /// </summary>
        public float Latitude { get; set; }
        
        /// <summary>
        /// Longitud en grados decimales (ej: -58.4 para Buenos Aires)
        /// Negativo = Oeste, Positivo = Este
        /// </summary>
        public float Longitude { get; set; }
        
        // =========================================================================
        // INFRAESTRUCTURA
        // =========================================================================
        
        /// <summary>
        /// ¿Tiene puerto marítimo?
        /// </summary>
        public bool HasPort { get; set; }
        
        /// <summary>
        /// ¿Tiene aeropuerto?
        /// </summary>
        public bool HasAirport { get; set; }
        
        /// <summary>
        /// ¿Es un hub terrestre (puede recibir/enviar carga por camión)?
        /// </summary>
        public bool IsLandHub { get; set; }
        
        /// <summary>
        /// ¿Es un hub logístico importante? (afecta rutas y costos)
        /// </summary>
        public bool IsMajorHub { get; set; }
        
        // =========================================================================
        // PROGRESIÓN Y DESBLOQUEO
        // =========================================================================
        
        /// <summary>
        /// ¿Está desbloqueada para el jugador?
        /// Buenos Aires comienza en true, las demás en false.
        /// </summary>
        public bool IsUnlocked { get; set; }
        
        /// <summary>
        /// Costo para abrir una oficina aquí (base = 10000)
        /// Las ciudades principales cuestan más.
        /// </summary>
        public int UnlockCost { get; set; }
        
        /// <summary>
        /// Tier de desbloqueo (0 = inicio, 1 = 1 oficina, 2 = 3 oficinas, etc.)
        /// </summary>
        public int UnlockTier { get; set; }
        
        /// <summary>
        /// Popularidad de la ciudad (0-100). Afecta cuántas cargas se generan.
        /// </summary>
        public int Popularity { get; set; }
        
        // =========================================================================
        // ZONA TERRESTRE (para saber si hay conexión por tierra con otra ciudad)
        // =========================================================================
        
        /// <summary>
        /// Zona terrestre para transporte por camión.
        /// Dos ciudades pueden conectarse por tierra SOLO si están en la misma zona.
        /// Ejemplos: "south_america", "north_america", "europe", "east_asia"
        /// </summary>
        public string LandZone { get; set; }
        
        // =========================================================================
        // CONSTRUCTORES
        // =========================================================================
        
        /// <summary>
        /// Constructor por defecto (necesario para serialización JSON)
        /// </summary>
        public WorldCity()
        {
            Id = string.Empty;
            DisplayName = string.Empty;
            Country = string.Empty;
            Continent = string.Empty;
            LandZone = string.Empty;
            IsUnlocked = false;
            HasPort = false;
            HasAirport = false;
            IsLandHub = false;
            IsMajorHub = false;
            UnlockCost = 10000;
            UnlockTier = 0;
            Popularity = 50;
        }
        
        /// <summary>
        /// Constructor completo para crear ciudades desde código.
        /// </summary>
        public WorldCity(string id, string displayName, string country, string continent,
                         float latitude, float longitude, bool hasPort, bool hasAirport,
                         bool isLandHub, bool isMajorHub, int unlockTier, int popularity = 50)
        {
            Id = id;
            DisplayName = displayName;
            Country = country;
            Continent = continent;
            Latitude = latitude;
            Longitude = longitude;
            HasPort = hasPort;
            HasAirport = hasAirport;
            IsLandHub = isLandHub;
            IsMajorHub = isMajorHub;
            UnlockTier = unlockTier;
            Popularity = popularity;
            IsUnlocked = false;
            UnlockCost = 10000;
            LandZone = DetermineLandZone(continent, country);
        }
        
        // =========================================================================
        // MÉTODOS AUXILIARES
        // =========================================================================
        
        /// <summary>
        /// Determina la zona terrestre según continente y país.
        /// </summary>
        private string DetermineLandZone(string continent, string country)
        {
            // Sudamérica está conectada por tierra (excepto el Tapón del Darién)
            if (continent == "South America")
                return "south_america";
            
            // Norteamérica (excepto Panamá que es puente)
            if (continent == "North America" && country != "Panamá")
                return "north_america";
            
            // Centroamérica tiene el Tapón del Darién que la desconecta de Sudamérica
            if (country == "Panamá")
                return "central_america";
            
            // Europa continental está conectada
            if (continent == "Europe" && country != "Reino Unido")
                return "europe";
            
            // Asia (parte continental)
            if (continent == "Asia")
            {
                // India, Pakistán, China, Rusia, etc. están conectados
                if (country != "Japón" && country != "Filipinas" && country != "Indonesia" &&
                    country != "Sri Lanka" && country != "Taiwán")
                    return "asia_continental";
            }
            
            // África continental
            if (continent == "Africa")
                return "africa_continental";
            
            // Por defecto, sin conexión terrestre (islas o aisladas)
            return string.Empty;
        }
        
        /// <summary>
        /// Verifica si se puede transportar carga por tierra desde esta ciudad a otra.
        /// </summary>
        public bool CanLandTransportTo(WorldCity other)
        {
            // Ambas deben tener zona terrestre definida
            if (string.IsNullOrEmpty(LandZone) || string.IsNullOrEmpty(other.LandZone))
                return false;
            
            // Deben estar en la MISMA zona
            return LandZone == other.LandZone;
        }
        
        /// <summary>
        /// Convierte latitud/longitud a posición en esfera 3D (para el mapa).
        /// </summary>
        /// <param name="radius">Radio de la esfera (ej: 10 unidades)</param>
        /// <returns>Vector3 con coordenadas X, Y, Z</returns>
        public UnityEngine.Vector3 ToSpherePosition(float radius)
        {
            // Convertir grados a radianes
            float latRad = Latitude * Mathf.Deg2Rad;
            float lonRad = Longitude * Mathf.Deg2Rad;
            
            // Fórmula de conversión:
            // x = R * cos(lat) * cos(lon)
            // y = R * sin(lat)
            // z = R * cos(lat) * sin(lon)
            float x = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);
            float y = radius * Mathf.Sin(latRad);
            float z = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
            
            return new UnityEngine.Vector3(x, y, z);
        }
        
        /// <summary>
        /// Calcula la distancia Haversine (km) entre dos ciudades.
        /// </summary>
        public float DistanceTo(WorldCity other)
        {
            const float EarthRadiusKm = 6371f;
            
            float lat1 = Latitude * Mathf.Deg2Rad;
            float lat2 = other.Latitude * Mathf.Deg2Rad;
            float deltaLat = (other.Latitude - Latitude) * Mathf.Deg2Rad;
            float deltaLon = (other.Longitude - Longitude) * Mathf.Deg2Rad;
            
            float a = Mathf.Sin(deltaLat / 2) * Mathf.Sin(deltaLat / 2) +
                      Mathf.Cos(lat1) * Mathf.Cos(lat2) *
                      Mathf.Sin(deltaLon / 2) * Mathf.Sin(deltaLon / 2);
            
            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
            
            return EarthRadiusKm * c;
        }
        
        // =========================================================================
        // SOBRESCRITURA PARA DEPURACIÓN
        // =========================================================================
        
        public override string ToString()
        {
            return $"[WorldCity] {DisplayName}, {Country} ({(IsUnlocked ? "🔓" : "🔒")})";
        }
    }
    
    /// <summary>
    /// Clase estática con datos predefinidos de todas las ciudades.
    /// Esto reemplaza la necesidad de un ScriptableObject por ahora.
    /// </summary>
    public static class CityDatabase
    {
        private static Dictionary<string, WorldCity> _cities;
        
        /// <summary>
        /// Diccionario de todas las ciudades del juego.
        /// </summary>
        public static Dictionary<string, WorldCity> AllCities
        {
            get
            {
                if (_cities == null)
                    LoadCities();
                return _cities;
            }
        }
        
        /// <summary>
        /// Obtiene una ciudad por su ID.
        /// </summary>
        public static WorldCity GetCity(string id)
        {
            if (AllCities.TryGetValue(id, out WorldCity city))
                return city;
            return null;
        }
        
        /// <summary>
        /// Carga todas las ciudades (hardcodeadas por ahora).
        /// </summary>
        private static void LoadCities()
        {
            _cities = new Dictionary<string, WorldCity>();
            
            // ======================================================================
            // TIER 0 - INICIO (oficinas = 0)
            // ======================================================================
            AddCity(new WorldCity("buenos_aires", "Buenos Aires", "Argentina", "South America",
                                  -34.6f, -58.4f, true, true, true, true, 0, 60));
            
            // ======================================================================
            // TIER 1 (1 oficina)
            // ======================================================================
            AddCity(new WorldCity("sao_paulo", "São Paulo", "Brasil", "South America",
                                  -23.5f, -46.6f, true, true, true, true, 1, 70));
            
            AddCity(new WorldCity("miami", "Miami", "Estados Unidos", "North America",
                                  25.8f, -80.2f, true, true, true, true, 1, 80));
            
            AddCity(new WorldCity("lima", "Lima", "Perú", "South America",
                                  -12.0f, -77.0f, true, true, true, false, 1, 58));
            
            // ======================================================================
            // TIER 2 (3 oficinas)
            // ======================================================================
            AddCity(new WorldCity("shanghai", "Shanghai", "China", "Asia",
                                  31.2f, 121.5f, true, true, true, true, 2, 95));
            
            AddCity(new WorldCity("rotterdam", "Rotterdam", "Países Bajos", "Europe",
                                  51.9f, 4.5f, true, true, true, true, 2, 85));
            
            AddCity(new WorldCity("tokyo", "Tokyo", "Japón", "Asia",
                                  35.7f, 139.7f, true, true, false, true, 2, 82));
            
            AddCity(new WorldCity("houston", "Houston", "Estados Unidos", "North America",
                                  29.8f, -95.4f, true, true, true, false, 2, 78));
            
            AddCity(new WorldCity("panama", "Ciudad de Panamá", "Panamá", "Central America",
                                  9.0f, -79.5f, true, true, false, true, 2, 70));
            
            // ======================================================================
            // TIER 3 (5 oficinas)
            // ======================================================================
            AddCity(new WorldCity("new_york", "New York", "Estados Unidos", "North America",
                                  40.7f, -74.0f, true, true, true, true, 3, 90));
            
            AddCity(new WorldCity("london", "London", "Reino Unido", "Europe",
                                  51.5f, -0.1f, true, true, false, true, 3, 88));
            
            AddCity(new WorldCity("dubai", "Dubai", "Emiratos Árabes", "Middle East",
                                  25.2f, 55.3f, true, true, false, true, 3, 75));
            
            AddCity(new WorldCity("singapore", "Singapur", "Singapur", "Asia",
                                  1.3f, 103.8f, true, true, true, true, 3, 90));
            
            AddCity(new WorldCity("hamburg", "Hamburg", "Alemania", "Europe",
                                  53.6f, 10.0f, true, true, true, false, 3, 80));
            
            AddCity(new WorldCity("cape_town", "Cape Town", "Sudáfrica", "Africa",
                                  -33.9f, 18.4f, true, true, true, false, 3, 62));
            
            AddCity(new WorldCity("madrid", "Madrid", "España", "Europe",
                                  40.4f, -3.7f, false, true, true, true, 3, 82));
            
            AddCity(new WorldCity("bogota", "Bogotá", "Colombia", "South America",
                                  4.7f, -74.1f, false, true, true, false, 3, 68));
            
            AddCity(new WorldCity("valparaiso", "Valparaíso", "Chile", "South America",
                                  -33.0f, -71.6f, true, false, false, false, 3, 62));
            
            // ======================================================================
            // TIER 4 (8 oficinas)
            // ======================================================================
            AddCity(new WorldCity("los_angeles", "Los Ángeles", "Estados Unidos", "North America",
                                  34.0f, -118.2f, true, true, true, true, 4, 85));
            
            AddCity(new WorldCity("hong_kong", "Hong Kong", "China", "Asia",
                                  22.3f, 114.2f, true, true, true, true, 4, 92));
            
            AddCity(new WorldCity("mumbai", "Mumbai", "India", "Asia",
                                  19.1f, 72.9f, true, true, true, false, 4, 78));
            
            AddCity(new WorldCity("istanbul", "Estambul", "Turquía", "Europe",
                                  41.0f, 28.9f, true, true, true, false, 4, 80));
            
            AddCity(new WorldCity("barcelona", "Barcelona", "España", "Europe",
                                  41.4f, 2.2f, true, true, true, false, 4, 78));
            
            AddCity(new WorldCity("bangkok", "Bangkok", "Tailandia", "Asia",
                                  13.8f, 100.5f, true, true, true, false, 4, 75));
            
            AddCity(new WorldCity("vancouver", "Vancouver", "Canadá", "North America",
                                  49.3f, -123.1f, true, true, true, false, 4, 75));
            
            // ======================================================================
            // TIER 5 (12 oficinas)
            // ======================================================================
            AddCity(new WorldCity("antwerp", "Amberes", "Bélgica", "Europe",
                                  51.2f, 4.4f, true, true, true, false, 5, 82));
            
            AddCity(new WorldCity("busan", "Busan", "Corea del Sur", "Asia",
                                  35.1f, 129.0f, true, true, false, false, 5, 75));
            
            AddCity(new WorldCity("johannesburg", "Johannesburgo", "Sudáfrica", "Africa",
                                  -26.2f, 28.0f, false, true, true, false, 5, 68));
            
            AddCity(new WorldCity("sydney", "Sydney", "Australia", "Oceania",
                                  -33.9f, 151.2f, true, true, false, true, 5, 65));
            
            AddCity(new WorldCity("marseille", "Marsella", "Francia", "Europe",
                                  43.3f, 5.4f, true, true, true, false, 5, 72));
            
            AddCity(new WorldCity("frankfurt", "Fráncfort", "Alemania", "Europe",
                                  50.1f, 8.7f, false, true, true, true, 5, 80));
            
            AddCity(new WorldCity("santos", "Santos", "Brasil", "South America",
                                  -23.9f, -46.3f, true, false, true, false, 5, 55));
            
            // ======================================================================
            // TIER 6 (18 oficinas) - Dominio absoluto
            // ======================================================================
            AddCity(new WorldCity("port_said", "Port Said", "Egipto", "Africa",
                                  31.3f, 32.3f, true, true, true, false, 6, 55));
            
            AddCity(new WorldCity("mombasa", "Mombasa", "Kenia", "Africa",
                                  -4.1f, 39.7f, true, true, true, false, 6, 50));
            
            AddCity(new WorldCity("ho_chi_minh", "Ho Chi Minh", "Vietnam", "Asia",
                                  10.8f, 106.7f, true, true, true, false, 6, 68));
            
            AddCity(new WorldCity("manila", "Manila", "Filipinas", "Asia",
                                  14.6f, 120.9f, true, true, false, false, 6, 65));
            
            AddCity(new WorldCity("taipei", "Taipéi", "Taiwán", "Asia",
                                  25.0f, 121.6f, true, true, false, false, 6, 72));
            
            AddCity(new WorldCity("auckland", "Auckland", "Nueva Zelanda", "Oceania",
                                  -36.9f, 174.8f, true, true, false, false, 6, 52));
        }
        
        private static void AddCity(WorldCity city)
        {
            _cities[city.Id] = city;
        }
    }
}