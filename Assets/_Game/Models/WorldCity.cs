using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    [Serializable]
    public class WorldCity
    {
        // Identificación
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Country { get; set; }
        public string Continent { get; set; }

        // Coordenadas
        public float Latitude { get; set; }
        public float Longitude { get; set; }

        // Infraestructura
        public bool HasPort { get; set; }
        public bool HasAirport { get; set; }
        public bool IsLandHub { get; set; }
        public bool IsMajorHub { get; set; }

        // Progresión
        public bool IsUnlocked { get; set; }
        public int UnlockCost { get; set; }
        public int UnlockTier { get; set; }
        public int Popularity { get; set; }

        // Zona terrestre
        public string LandZone { get; set; }

        public WorldCity() { }

        public WorldCity(string id, string displayName, string country, string continent,
                         float latitude, float longitude,
                         bool hasPort, bool hasAirport, bool isLandHub, bool isMajorHub,
                         bool isUnlocked, int unlockCost, int unlockTier, int popularity)
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
            IsUnlocked = isUnlocked;
            UnlockCost = unlockCost;
            UnlockTier = unlockTier;
            Popularity = popularity;
            LandZone = DetermineLandZone(continent, country);
        }

        private string DetermineLandZone(string continent, string country)
        {
            switch (continent)
            {
                case "South America": return "south_america";
                case "North America":
                    return country == "Panama" ? "central_america" : "north_america";
                case "Europe":
                    return country == "United Kingdom" ? "" : "europe";
                case "Asia":
                case "Middle East":
                {
                    string[] islands = { "Japan", "Philippines", "Indonesia", "Sri Lanka", "Taiwan" };
                    foreach (var island in islands)
                        if (country == island) return "";
                    return "asia_continental";
                }
                case "Africa": return "africa_continental";
                default:       return "";
            }
        }

        public bool CanLandTransportTo(WorldCity other)
        {
            if (string.IsNullOrEmpty(LandZone) || string.IsNullOrEmpty(other.LandZone))
                return false;
            return LandZone == other.LandZone;
        }

        public Vector3 ToSpherePosition(float radius)
        {
            float latRad = Latitude * Mathf.Deg2Rad;
            float lonRad = Longitude * Mathf.Deg2Rad;
            return new Vector3(
                radius * Mathf.Cos(latRad) * Mathf.Cos(lonRad),
                radius * Mathf.Sin(latRad),
                radius * Mathf.Cos(latRad) * Mathf.Sin(lonRad)
            );
        }

        public Vector2 ToVector2() => new Vector2(Latitude, Longitude);

        public float DistanceTo(WorldCity other)
        {
            const float R = 6371f;
            float lat1 = Latitude * Mathf.Deg2Rad;
            float lat2 = other.Latitude * Mathf.Deg2Rad;
            float dLat = (other.Latitude - Latitude) * Mathf.Deg2Rad;
            float dLon = (other.Longitude - Longitude) * Mathf.Deg2Rad;

            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                      Mathf.Cos(lat1) * Mathf.Cos(lat2) *
                      Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
            return R * c;
        }

        public override string ToString() => $"{DisplayName}, {Country} ({Id})";
    }
}
