using UnityEngine;
using System;

/// <summary>
/// Representa una ciudad del mundo real en el juego.
/// Contiene información geográfica, infraestructura de transporte y datos económicos.
/// </summary>
[Serializable]
[CreateAssetMenu(fileName = "NewWorldCity", menuName = "FreightForwarder/WorldCity")]
public class WorldCity : ScriptableObject
{
    [Header("Información Básica")]
    [SerializeField] private string cityName;
    [SerializeField] private string country;
    [SerializeField] private string region;

    [Header("Coordenadas Geográficas")]
    [SerializeField] private float latitude;
    [SerializeField] private float longitude;

    [Header("Infraestructura de Transporte")]
    [SerializeField] private bool hasAirport;
    [SerializeField] private bool hasPort;
    [SerializeField] private bool hasRail;
    [SerializeField] private bool isLandHub;

    [Header("Datos Económicos")]
    [SerializeField] private float economicMultiplier = 1f;
    [SerializeField] private float riskMultiplier = 1f;

    [Header("Estado de Desbloqueo")]
    [SerializeField] private bool isUnlocked = true;
    [SerializeField] private int unlockLevel = 1;

    // Propiedades públicas
    public string Name => cityName;
    public string Country => country;
    public string Region => region;
    public float Latitude => latitude;
    public float Longitude => longitude;
    public bool HasAirport => hasAirport;
    public bool HasPort => hasPort;
    public bool HasRail => hasRail;
    public bool IsLandHub => isLandHub;
    public float EconomicMultiplier => economicMultiplier;
    public float RiskMultiplier => riskMultiplier;
    public bool IsUnlocked => isUnlocked;
    public int UnlockLevel => unlockLevel;

    // Propiedades calculadas
    public Vector2 Coordinates => new Vector2(longitude, latitude);
    public bool HasAnyTransport => hasAirport || hasPort || hasRail || isLandHub;

    /// <summary>
    /// Calcula la distancia a otra ciudad usando la fórmula de Haversine.
    /// </summary>
    /// <param name="otherCity">Otra ciudad</param>
    /// <returns>Distancia en kilómetros</returns>
    public float DistanceTo(WorldCity otherCity)
    {
        if (otherCity == null) return 0f;

        float lat1 = latitude * Mathf.Deg2Rad;
        float lon1 = longitude * Mathf.Deg2Rad;
        float lat2 = otherCity.latitude * Mathf.Deg2Rad;
        float lon2 = otherCity.longitude * Mathf.Deg2Rad;

        float dLat = lat2 - lat1;
        float dLon = lon2 - lon1;

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(lat1) * Mathf.Cos(lat2) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return 6371 * c; // Radio de la Tierra en km
    }

    /// <summary>
    /// Verifica si esta ciudad puede conectarse con otra vía un modo de transporte específico.
    /// </summary>
    /// <param name="otherCity">Otra ciudad</param>
    /// <param name="transportMode">Modo de transporte</param>
    /// <returns>True si la conexión es posible</returns>
    public bool CanConnectTo(WorldCity otherCity, Constants.TransportMode transportMode)
    {
        if (otherCity == null || !otherCity.IsUnlocked) return false;

        return transportMode switch
        {
            Constants.TransportMode.Maritime => hasPort && otherCity.hasPort,
            Constants.TransportMode.Air => hasAirport && otherCity.hasAirport,
            Constants.TransportMode.Land => isLandHub && otherCity.isLandHub && IsSameLandZone(otherCity),
            Constants.TransportMode.Rail => hasRail && otherCity.hasRail,
            Constants.TransportMode.Multimodal => true, // Siempre posible con multimodal
            _ => false
        };
    }

    /// <summary>
    /// Verifica si dos ciudades están en la misma zona terrestre.
    /// </summary>
    /// <param name="otherCity">Otra ciudad</param>
    /// <returns>True si están en la misma zona</returns>
    private bool IsSameLandZone(WorldCity otherCity)
    {
        // Simplificación: considerar continentes/regiones
        return region == otherCity.region;
    }

    /// <summary>
    /// Calcula el tiempo de viaje estimado a otra ciudad.
    /// </summary>
    /// <param name="otherCity">Ciudad destino</param>
    /// <param name="transportMode">Modo de transporte</param>
    /// <returns>Días estimados de viaje</returns>
    public int CalculateTravelDays(WorldCity otherCity, Constants.TransportMode transportMode)
    {
        float distance = DistanceTo(otherCity);
        if (distance <= 0) return 1;

        // Velocidades aproximadas en km/día
        float speed = transportMode switch
        {
            Constants.TransportMode.Maritime => 600f, // 20 nudos
            Constants.TransportMode.Air => 19200f,    // 800 km/h * 24h
            Constants.TransportMode.Land => 800f,     // 800 km/día
            Constants.TransportMode.Rail => 1000f,    // 1000 km/día
            Constants.TransportMode.Multimodal => 1200f, // Combinado
            _ => 800f
        };

        return Mathf.Max(1, Mathf.CeilToInt(distance / speed));
    }

    /// <summary>
    /// Desbloquea la ciudad para uso en el juego.
    /// </summary>
    public void Unlock()
    {
        isUnlocked = true;
        Debug.Log($"Ciudad desbloqueada: {cityName}");
    }

    /// <summary>
    /// Obtiene una descripción detallada de la ciudad.
    /// </summary>
    /// <returns>Descripción formateada</returns>
    public string GetDescription()
    {
        string desc = $"{cityName}, {country}\n";
        desc += $"Región: {region}\n";
        desc += $"Coordenadas: {latitude:0.###}°, {longitude:0.###}°\n";

        desc += "Infraestructura: ";
        if (hasAirport) desc += "✈️ ";
        if (hasPort) desc += "🚢 ";
        if (hasRail) desc += "🚂 ";
        if (isLandHub) desc += "🚛 ";
        desc += "\n";

        desc += $"Multiplicador económico: {economicMultiplier}x\n";
        desc += $"Multiplicador de riesgo: {riskMultiplier}x\n";

        if (!isUnlocked)
        {
            desc += $"Requiere nivel: {unlockLevel}\n";
        }

        return desc;
    }

    /// <summary>
    /// Crea una instancia con datos geográficos completos.
    /// </summary>
    public static WorldCity CreateCity(string name, string country, string region,
        float lat, float lon, bool airport = true, bool port = true, bool rail = true, bool landHub = true)
    {
        WorldCity city = CreateInstance<WorldCity>();
        city.cityName   = name;
        city.country    = country;
        city.region     = region;
        city.latitude   = lat;
        city.longitude  = lon;
        city.hasAirport = airport;
        city.hasPort    = port;
        city.hasRail    = rail;
        city.isLandHub  = landHub;
        city.economicMultiplier = 1f;
        city.riskMultiplier     = 1f;
        city.isUnlocked  = true;
        city.unlockLevel = 1;
        return city;
    }

    /// <summary>
    /// Crea una instancia de WorldCity con valores predeterminados para testing.
    /// </summary>
    public static WorldCity CreateTestCity(string name, float lat, float lon, bool airport = true, bool port = true)
    {
        WorldCity city = CreateInstance<WorldCity>();
        city.cityName = name;
        city.country = "Test Country";
        city.region = "Test Region";
        city.latitude = lat;
        city.longitude = lon;
        city.hasAirport = airport;
        city.hasPort = port;
        city.hasRail = true;
        city.isLandHub = true;
        city.economicMultiplier = 1f;
        city.riskMultiplier = 1f;
        city.isUnlocked = true;
        city.unlockLevel = 1;

        return city;
    }
}