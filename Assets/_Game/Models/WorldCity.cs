using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    /// <summary>
    /// WorldCity.cs — Modelo de una ciudad en el mundo.
    /// </summary>
    [Serializable]
    public class WorldCity
    {
        // =========================================================================
        // IDENTIFICACIÓN BÁSICA
        // =========================================================================
        
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Country { get; set; }
        public string Continent { get; set; }
        
        // =========================================================================
        // COORDENADAS GEOGRÁFICAS
        // =========================================================================
        
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        
        // =========================================================================
        // INFRAESTRUCTURA
        // =========================================================================
        
        public bool HasPort { get; set; }
        public bool HasAirport { get; set; }
        public bool IsLandHub { get; set; }
        public bool IsMajorHub { get; set; }
        
        // =========================================================================
        // PROGRESIÓN Y DESBLOQUEO
        // =========================================================================
        
        public bool IsUnlocked { get; set; }
        public int UnlockCost { get; set; }
        public int UnlockTier { get; set; }
        public int Popularity { get; set; }
        
        // =========================================================================
        // ZONA TERRESTRE
        // =========================================================================
        
        public string LandZone { get; set; }
        
        // =========================================================================
        // CONSTRUCTORES
        // =========================================================================
        
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
        
        private string DetermineLandZone(string continent, string country)
        {
            if (continent == "South America")
                return "south_america";
            if (continent == "North America" && country != "Panamá")
                return "north_america";
            if (country == "Panamá")
                return "central_america";
            if (continent == "Europe" && country != "Reino Unido")
                return "europe";
            if (continent == "Asia")
            {
                if (country != "Japón" && country != "Filipinas" && country != "Indonesia" &&
                    country != "Sri Lanka" && country != "Taiwán")
                    return "asia_continental";
            }
            if (continent == "Africa")
                return "africa_continental";
            return string.Empty;
        }
        
        public bool CanLandTransportTo(WorldCity other)
        {
            if (string.IsNullOrEmpty(LandZone) || string.IsNullOrEmpty(other.LandZone))
                return false;
            return LandZone == other.LandZone;
        }
        
        public UnityEngine.Vector3 ToSpherePosition(float radius)
        {
            float latRad = Latitude * Mathf.Deg2Rad;
            float lonRad = Longitude * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad);
            float y = radius * Mathf.Sin(latRad);
            float z = radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad);
            return new UnityEngine.Vector3(x, y, z);
        }
        
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
        
        public override string ToString()
        {
            return $"[WorldCity] {DisplayName}, {Country} ({(IsUnlocked ? "🔓" : "🔒")})";
        }
    }
}