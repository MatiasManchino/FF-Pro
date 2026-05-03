using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ClientManager gestiona todos los clientes del juego: creación, seguimiento de satisfacción,
/// negociación de contratos y evolución de relaciones.
/// </summary>
public class ClientManager : Singleton<ClientManager>
{
    private bool isInitialized;

    [Header("Configuración de Clientes")]
    [SerializeField] private int maxActiveClients = 20;
    [SerializeField] private float clientSatisfactionDecayRate = 0.01f; // Por día

    [Header("Listas de Clientes")]
    [SerializeField] private List<Client> allClients = new List<Client>();
    [SerializeField] private List<Client> activeClients = new List<Client>();

    [Header("Probabilidades de Aparición")]
    [SerializeField] private Dictionary<Constants.ClientType, float> clientTypeProbabilities;

    // Eventos
    public System.Action<Client> OnClientAdded;
    public System.Action<Client> OnClientRemoved;
    public System.Action<Client, float> OnClientSatisfactionChanged;

    // Propiedades públicas
    public List<Client> AllClients => allClients;
    public List<Client> ActiveClients => activeClients;
    public int TotalClients => allClients.Count;
    public int ActiveClientCount => activeClients.Count;

    /// <summary>
    /// Inicializa el ClientManager.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        allClients = new List<Client>();
        activeClients = new List<Client>();

        // Inicializar probabilidades si no están asignadas
        if (clientTypeProbabilities == null || clientTypeProbabilities.Count == 0)
        {
            clientTypeProbabilities = new Dictionary<Constants.ClientType, float>(Constants.CLIENT_PROBABILITIES);
        }

        Debug.Log("ClientManager inicializado");

        // Generar clientes iniciales
        GenerateInitialClients();
    }

    /// <summary>
    /// Actualización por frame del ClientManager.
    /// </summary>
    private void Update()
    {
        // Actualizar satisfacción de clientes
        UpdateClientSatisfaction();

        // Verificar si necesitamos generar nuevos clientes
        MaintainClientPool();
    }

    /// <summary>
    /// Genera el pool inicial de clientes.
    /// </summary>
    private void GenerateInitialClients()
    {
        for (int i = 0; i < maxActiveClients; i++)
        {
            Client newClient = GenerateRandomClient();
            if (newClient != null)
            {
                allClients.Add(newClient);
                activeClients.Add(newClient);
                OnClientAdded?.Invoke(newClient);
            }
        }

        Debug.Log($"Generados {activeClients.Count} clientes iniciales");
    }

    /// <summary>
    /// Genera un cliente aleatorio basado en probabilidades.
    /// </summary>
    private Client GenerateRandomClient()
    {
        Constants.ClientType clientType = SelectRandomClientType();

        string[] firstNames = { "Juan", "María", "Carlos", "Ana", "Pedro", "Laura", "Diego", "Carmen", "Miguel", "Isabel" };
        string[] lastNames = { "García", "Rodríguez", "González", "Fernández", "López", "Martínez", "Sánchez", "Pérez", "Martín", "Ruiz" };
        string[] companyTypes = { "S.A.", "Ltda.", "Corp.", "Inc.", "LLC", "GmbH", "Co.", "Group" };

        string firstName = firstNames[Random.Range(0, firstNames.Length)];
        string lastName = lastNames[Random.Range(0, lastNames.Length)];
        string companyType = companyTypes[Random.Range(0, companyTypes.Length)];

        string clientName = $"{firstName} {lastName}";
        string companyName = $"{lastName} {companyType}";

        Client newClient = Client.CreateTestClient(clientName, clientType);
        newClient.CompanyName = companyName;

        // Asignar cargas preferidas aleatoriamente
        int preferredCount = Random.Range(0, 3);
        for (int i = 0; i < preferredCount; i++)
        {
            Constants.CargoType randomType = (Constants.CargoType)Random.Range(0, System.Enum.GetValues(typeof(Constants.CargoType)).Length);
            if (!newClient.PreferredCargoTypes.Contains(randomType))
            {
                newClient.PreferredCargoTypes.Add(randomType);
            }
        }

        return newClient;
    }

    /// <summary>
    /// Selecciona un tipo de cliente aleatorio basado en probabilidades.
    /// </summary>
    private Constants.ClientType SelectRandomClientType()
    {
        if (clientTypeProbabilities == null || clientTypeProbabilities.Count == 0)
        {
            clientTypeProbabilities = new Dictionary<Constants.ClientType, float>(Constants.CLIENT_PROBABILITIES);
        }

        float roll = Random.value;
        float cumulative = 0f;

        foreach (var kvp in clientTypeProbabilities)
        {
            cumulative += kvp.Value;
            if (roll <= cumulative)
            {
                return kvp.Key;
            }
        }

        return Constants.ClientType.GoodPayer; // Fallback
    }

    /// <summary>
    /// Actualiza la satisfacción de todos los clientes activos.
    /// </summary>
    private void UpdateClientSatisfaction()
    {
        foreach (Client client in activeClients)
        {
            client.UpdateSatisfaction(clientSatisfactionDecayRate);

            // Verificar cambios significativos en satisfacción
            if (client.CurrentSatisfaction <= 0.2f && client.IsActive)
            {
                // Cliente muy insatisfecho - podría irse
                if (Random.value < 0.1f) // 10% chance diaria
                {
                    RemoveClient(client, "Insatisfacción extrema");
                }
            }
        }
    }

    /// <summary>
    /// Mantiene el pool de clientes activos generando nuevos cuando sea necesario.
    /// </summary>
    private void MaintainClientPool()
    {
        if (activeClients.Count < maxActiveClients * 0.8f) // Mantener al menos 80%
        {
            int clientsToGenerate = maxActiveClients - activeClients.Count;
            for (int i = 0; i < clientsToGenerate; i++)
            {
                Client newClient = GenerateRandomClient();
                if (newClient != null)
                {
                    allClients.Add(newClient);
                    activeClients.Add(newClient);
                    OnClientAdded?.Invoke(newClient);
                    Debug.Log($"Nuevo cliente generado: {newClient.Name}");
                }
            }
        }
    }

    /// <summary>
    /// Remueve un cliente del pool activo.
    /// </summary>
    /// <param name="client">Cliente a remover</param>
    /// <param name="reason">Razón de la remoción</param>
    public void RemoveClient(Client client, string reason = "")
    {
        if (client == null || !activeClients.Contains(client)) return;

        activeClients.Remove(client);
        client.IsActive = false;

        OnClientRemoved?.Invoke(client);
        Debug.Log($"Cliente removido: {client.Name} - Razón: {reason}");
    }

    /// <summary>
    /// Busca un cliente por nombre.
    /// </summary>
    /// <param name="clientName">Nombre del cliente</param>
    /// <returns>Cliente encontrado o null</returns>
    public Client FindClientByName(string clientName)
    {
        return allClients.Find(c => c.Name == clientName);
    }

    /// <summary>
    /// Obtiene clientes interesados en un tipo de carga específico.
    /// </summary>
    /// <param name="cargoType">Tipo de carga</param>
    /// <returns>Lista de clientes interesados</returns>
    public List<Client> GetClientsInterestedInCargoType(Constants.CargoType cargoType)
    {
        return activeClients.FindAll(c => c.IsInterestedInCargoType(cargoType));
    }

    /// <summary>
    /// Obtiene clientes ordenados por satisfacción (más satisfechos primero).
    /// </summary>
    /// <returns>Lista ordenada de clientes</returns>
    public List<Client> GetClientsBySatisfaction()
    {
        List<Client> sortedClients = new List<Client>(activeClients);
        sortedClients.Sort((a, b) => b.CurrentSatisfaction.CompareTo(a.CurrentSatisfaction));
        return sortedClients;
    }

    /// <summary>
    /// Obtiene estadísticas de los clientes.
    /// </summary>
    public Dictionary<string, float> GetClientStats()
    {
        if (activeClients.Count == 0) return new Dictionary<string, float>();

        float avgSatisfaction = 0f;
        float avgContracts = 0f;
        float avgSuccessRate = 0f;

        foreach (Client client in activeClients)
        {
            avgSatisfaction += client.CurrentSatisfaction;
            avgContracts += client.TotalContracts;
            avgSuccessRate += client.SuccessRate;
        }

        return new Dictionary<string, float>
        {
            { "AverageSatisfaction", avgSatisfaction / activeClients.Count },
            { "AverageContracts", avgContracts / activeClients.Count },
            { "AverageSuccessRate", avgSuccessRate / activeClients.Count },
            { "TotalActiveClients", activeClients.Count },
            { "TotalAllClients", allClients.Count }
        };
    }

    /// <summary>
    /// Registra el resultado de un contrato con un cliente.
    /// </summary>
    /// <param name="client">Cliente involucrado</param>
    /// <param name="success">Si el contrato fue exitoso</param>
    /// <param name="paymentAmount">Monto pagado</param>
    /// <param name="paymentDelay">Días de retraso en pago</param>
    public void RegisterContractResult(Client client, bool success, float paymentAmount, int paymentDelay = 0)
    {
        if (client == null) return;

        client.RegisterContractResult(success, paymentAmount, paymentDelay);

        // Actualizar satisfacción del cliente
        OnClientSatisfactionChanged?.Invoke(client, client.CurrentSatisfaction);

        Debug.Log($"Resultado de contrato registrado para {client.Name}: {(success ? "Éxito" : "Fallo")}");
    }

    /// <summary>
    /// Limpia todos los clientes (para reinicio del juego).
    /// </summary>
    public void ClearAllClients()
    {
        activeClients.Clear();
        allClients.Clear();
        Debug.Log("Todos los clientes limpiados");
    }

    /// <summary>
    /// Agrega un cliente personalizado al sistema.
    /// </summary>
    /// <param name="client">Cliente a agregar</param>
    public void AddCustomClient(Client client)
    {
        if (client != null && !allClients.Contains(client))
        {
            allClients.Add(client);
            if (client.IsActive && !activeClients.Contains(client))
            {
                activeClients.Add(client);
            }
            OnClientAdded?.Invoke(client);
            Debug.Log($"Cliente personalizado agregado: {client.Name}");
        }
    }
}