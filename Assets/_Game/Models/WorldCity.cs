using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Models
{
    // Una ciudad del mundo: sus datos (nombre, país, continente), su posición geográfica
    // (latitud/longitud), qué infraestructura tiene (puerto, aeropuerto, hub terrestre) y
    // si el jugador ya la desbloqueó. [Serializable] permite guardarla y verla en el inspector.
    [Serializable]
    public class WorldCity
    {
        public string Id { get; set; }           // identificador interno (ej. "buenos_aires")
        public string DisplayName { get; set; }  // nombre para mostrar (ej. "Buenos Aires")
        public string Country { get; set; }      // país
        public string Continent { get; set; }    // continente

        public float Latitude { get; set; }      // latitud geográfica
        public float Longitude { get; set; }     // longitud (en coordenadas del mapa del juego)

        public bool HasPort { get; set; }     // tiene puerto marítimo
        public bool HasAirport { get; set; }  // tiene aeropuerto
        public bool IsLandHub { get; set; }   // es un nodo de transporte terrestre
        public bool IsMajorHub { get; set; }  // es un hub importante (mucho tráfico)

        public bool IsUnlocked { get; set; }  // el jugador ya puede operar en esta ciudad
        public int UnlockCost { get; set; }   // cuánto cuesta desbloquearla
        public int UnlockTier { get; set; }   // nivel/categoría de desbloqueo
        public int Popularity { get; set; }   // qué tan demandada es (afecta cuántas cargas aparecen)

        // "Zona terrestre": agrupa ciudades conectadas por tierra (mismo continente/región).
        // Sólo se puede ir por camión/tren entre ciudades de la misma zona.
        public string LandZone { get; set; }

        // Constructor vacío: necesario para guardar/cargar la ciudad desde disco.
        public WorldCity() { }

        // Constructor principal: completa todos los datos y deduce la zona terrestre.
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

        // Decide a qué "zona terrestre" pertenece la ciudad según su continente y país.
        // Las islas (Japón, Filipinas, etc.) y el Reino Unido quedan SIN zona ("") porque
        // no tienen conexión terrestre con el resto: a ellas sólo se llega por mar o aire.
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
                        if (country == island) return "";   // isla: sin conexión terrestre
                    return "asia_continental";
                }
                case "Africa": return "africa_continental";
                default:       return "";
            }
        }

        // ¿Se puede ir por tierra desde esta ciudad hasta "other"?
        // Sólo si ambas tienen zona terrestre y es la MISMA zona.
        public bool CanLandTransportTo(WorldCity other)
        {
            if (string.IsNullOrEmpty(LandZone) || string.IsNullOrEmpty(other.LandZone))
                return false;
            return LandZone == other.LandZone;
        }

        // Convierte la latitud/longitud de la ciudad a un punto 3D sobre una esfera del radio dado
        // (sirve para ubicar el marcador de la ciudad sobre el globo terráqueo).
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

        // Devuelve la posición como un par (latitud, longitud).
        public Vector2 ToVector2() => new Vector2(Latitude, Longitude);

        // Distancia en kilómetros entre esta ciudad y "other", sobre la superficie de la Tierra.
        // Usa la fórmula de Haversine (distancia "de gran círculo"); R = 6371 km es el radio terrestre.
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

        // Texto legible de la ciudad, útil para depurar (ej. "Buenos Aires, Argentina (buenos_aires)").
        public override string ToString() => $"{DisplayName}, {Country} ({Id})";
    }
}
