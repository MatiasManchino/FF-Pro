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

