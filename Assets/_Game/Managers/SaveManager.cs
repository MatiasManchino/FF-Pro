using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// SaveManager gestiona el guardado y carga del estado del juego.
/// Serializa/deserializa el estado completo del juego para persistencia.
/// </summary>
public class SaveManager : Singleton<SaveManager>
{
    [Header("Configuración de Guardado")]
    [SerializeField] private string saveFileName = "freight_forwarder_save.dat";
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private float autoSaveIntervalMinutes = 5f;

    private string saveFilePath;
    private float lastAutoSaveTime;

    // Eventos
    public System.Action OnGameSaved;
    public System.Action OnGameLoaded;
    public System.Action OnSaveError;

    // Propiedades públicas
    public string SaveFilePath => saveFilePath;
    public bool HasSaveFile => File.Exists(saveFilePath);

    /// <summary>
    /// Inicializa el SaveManager.
    /// </summary>
    public void Initialize()
    {
        // Crear directorio de guardado si no existe
        string saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        saveFilePath = Path.Combine(saveDirectory, saveFileName);
        lastAutoSaveTime = 0f;

        Debug.Log($"SaveManager inicializado. Archivo de guardado: {saveFilePath}");
    }

    /// <summary>
    /// Actualización por frame del SaveManager.
    /// </summary>
    private void Update()
    {
        // Auto-guardado periódico
        if (autoSaveEnabled && Time.time - lastAutoSaveTime >= autoSaveIntervalMinutes * 60f)
        {
            AutoSave();
        }
    }

    /// <summary>
    /// Guarda el estado completo del juego.
    /// </summary>
    /// <param name="fileName">Nombre opcional del archivo (usa el predeterminado si es null)</param>
    /// <returns>True si el guardado fue exitoso</returns>
    public bool SaveGame(string fileName = null)
    {
        string targetPath = fileName != null ? Path.Combine(Path.GetDirectoryName(saveFilePath), fileName) : saveFilePath;

        try
        {
            // Crear objeto de datos de guardado
            SaveData saveData = CreateSaveData();

            // Serializar y guardar
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(targetPath, FileMode.Create))
            {
                formatter.Serialize(stream, saveData);
            }

            lastAutoSaveTime = Time.time;
            OnGameSaved?.Invoke();

            Debug.Log($"Juego guardado exitosamente: {targetPath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al guardar el juego: {e.Message}");
            OnSaveError?.Invoke();
            return false;
        }
    }

    /// <summary>
    /// Carga el estado del juego desde archivo.
    /// </summary>
    /// <param name="fileName">Nombre opcional del archivo (usa el predeterminado si es null)</param>
    /// <returns>True si la carga fue exitosa</returns>
    public bool LoadGame(string fileName = null)
    {
        string targetPath = fileName != null ? Path.Combine(Path.GetDirectoryName(saveFilePath), fileName) : saveFilePath;

        if (!File.Exists(targetPath))
        {
            Debug.LogWarning($"Archivo de guardado no encontrado: {targetPath}");
            return false;
        }

        try
        {
            // Deserializar datos de guardado
            BinaryFormatter formatter = new BinaryFormatter();
            SaveData saveData;

            using (FileStream stream = new FileStream(targetPath, FileMode.Open))
            {
                saveData = formatter.Deserialize(stream) as SaveData;
            }

            if (saveData == null)
            {
                Debug.LogError("Datos de guardado corruptos o inválidos");
                return false;
            }

            // Restaurar estado del juego
            RestoreGameState(saveData);

            OnGameLoaded?.Invoke();
            Debug.Log($"Juego cargado exitosamente: {targetPath}");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al cargar el juego: {e.Message}");
            OnSaveError?.Invoke();
            return false;
        }
    }

    /// <summary>
    /// Realiza un auto-guardado si está habilitado.
    /// </summary>
    private void AutoSave()
    {
        if (autoSaveEnabled)
        {
            SaveGame("autosave.dat");
            Debug.Log("Auto-guardado realizado");
        }
    }

    /// <summary>
    /// Crea un objeto SaveData con el estado actual del juego.
    /// </summary>
    /// <returns>Objeto SaveData con todos los datos del juego</returns>
    private SaveData CreateSaveData()
    {
        SaveData saveData = new SaveData();

        // Datos de tiempo
        if (TimeManager.Instance != null)
        {
            saveData.currentYear = TimeManager.Instance.CurrentYear;
            saveData.currentMonth = TimeManager.Instance.CurrentMonth;
            saveData.currentDay = TimeManager.Instance.CurrentDay;
            saveData.currentHour = TimeManager.Instance.CurrentHour;
        }

        // Datos económicos
        if (EconomyManager.Instance != null)
        {
            saveData.currentMoney = EconomyManager.Instance.CurrentMoney;
            saveData.currentReputation = EconomyManager.Instance.CurrentReputation;
            saveData.totalEarned = EconomyManager.Instance.TotalEarned;
            saveData.totalSpent = EconomyManager.Instance.TotalSpent;
            saveData.completedCargos = EconomyManager.Instance.CompletedCargos;
            saveData.failedCargos = EconomyManager.Instance.FailedCargos;
        }

        // Datos de cargas (simplificado - en implementación real sería más complejo)
        if (CargoManager.Instance != null)
        {
            saveData.marketCargoCount = CargoManager.Instance.MarketCargos.Count;
            saveData.activeCargoCount = CargoManager.Instance.ActiveCargos.Count;
            saveData.completedCargoCount = CargoManager.Instance.CompletedCargos.Count;
            saveData.failedCargoCount = CargoManager.Instance.FailedCargos.Count;
        }

        // Datos de clientes
        if (ClientManager.Instance != null)
        {
            saveData.totalClients = ClientManager.Instance.TotalClients;
            saveData.activeClientCount = ClientManager.Instance.ActiveClientCount;
        }

        // Datos de agentes
        if (AgentManager.Instance != null)
        {
            saveData.totalAgents = AgentManager.Instance.TotalAgents;
            saveData.availableAgentCount = AgentManager.Instance.AvailableAgentCount;
            saveData.busyAgentCount = AgentManager.Instance.BusyAgentCount;
        }

        // Metadata del guardado
        saveData.saveTime = System.DateTime.Now;
        saveData.gameVersion = Application.version;

        return saveData;
    }

    /// <summary>
    /// Restaura el estado del juego desde SaveData.
    /// </summary>
    /// <param name="saveData">Datos de guardado a restaurar</param>
    private void RestoreGameState(SaveData saveData)
    {
        // Restaurar tiempo
        if (TimeManager.Instance != null)
        {
            // Nota: En implementación real, ajustar TimeManager para restaurar fecha específica
            Debug.Log($"Restaurando tiempo: {saveData.currentDay}/{saveData.currentMonth}/{saveData.currentYear} {saveData.currentHour}:00");
        }

        // Restaurar economía
        if (EconomyManager.Instance != null)
        {
            // Nota: En implementación real, restaurar valores económicos
            Debug.Log($"Restaurando economía: ${saveData.currentMoney}, Reputación: {saveData.currentReputation}");
        }

        // Nota: Restaurar cargas, clientes y agentes sería más complejo
        // y requeriría serialización más detallada de cada objeto

        Debug.Log("Estado del juego restaurado desde guardado");
    }

    /// <summary>
    /// Elimina el archivo de guardado.
    /// </summary>
    /// <param name="fileName">Nombre opcional del archivo</param>
    /// <returns>True si se eliminó exitosamente</returns>
    public bool DeleteSaveFile(string fileName = null)
    {
        string targetPath = fileName != null ? Path.Combine(Path.GetDirectoryName(saveFilePath), fileName) : saveFilePath;

        try
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
                Debug.Log($"Archivo de guardado eliminado: {targetPath}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Archivo de guardado no encontrado para eliminar: {targetPath}");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al eliminar archivo de guardado: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene información del archivo de guardado.
    /// </summary>
    /// <param name="fileName">Nombre opcional del archivo</param>
    /// <returns>Información del guardado o null si no existe</returns>
    public SaveFileInfo GetSaveFileInfo(string fileName = null)
    {
        string targetPath = fileName != null ? Path.Combine(Path.GetDirectoryName(saveFilePath), fileName) : saveFilePath;

        if (!File.Exists(targetPath))
        {
            return null;
        }

        try
        {
            FileInfo fileInfo = new FileInfo(targetPath);
            return new SaveFileInfo
            {
                fileName = Path.GetFileName(targetPath),
                filePath = targetPath,
                lastModified = fileInfo.LastWriteTime,
                fileSize = fileInfo.Length
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al obtener información del archivo: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Lista todos los archivos de guardado disponibles.
    /// </summary>
    /// <returns>Lista de información de archivos de guardado</returns>
    public System.Collections.Generic.List<SaveFileInfo> ListSaveFiles()
    {
        System.Collections.Generic.List<SaveFileInfo> saveFiles = new System.Collections.Generic.List<SaveFileInfo>();

        try
        {
            string saveDirectory = Path.GetDirectoryName(saveFilePath);
            if (Directory.Exists(saveDirectory))
            {
                string[] files = Directory.GetFiles(saveDirectory, "*.dat");
                foreach (string file in files)
                {
                    SaveFileInfo info = GetSaveFileInfo(Path.GetFileName(file));
                    if (info != null)
                    {
                        saveFiles.Add(info);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al listar archivos de guardado: {e.Message}");
        }

        return saveFiles;
    }

    /// <summary>
    /// Fuerza un guardado inmediato (útil para puntos de guardado importantes).
    /// </summary>
    public void ForceSave()
    {
        SaveGame();
        Debug.Log("Guardado forzado realizado");
    }
}

/// <summary>
/// Contiene todos los datos serializables del estado del juego.
/// </summary>
[System.Serializable]
public class SaveData
{
    // Tiempo
    public int currentYear;
    public int currentMonth;
    public int currentDay;
    public int currentHour;

    // Economía
    public float currentMoney;
    public float currentReputation;
    public float totalEarned;
    public float totalSpent;
    public int completedCargos;
    public int failedCargos;

    // Estadísticas simplificadas (en implementación real sería más detallado)
    public int marketCargoCount;
    public int activeCargoCount;
    public int completedCargoCount;
    public int failedCargoCount;
    public int totalClients;
    public int activeClientCount;
    public int totalAgents;
    public int availableAgentCount;
    public int busyAgentCount;

    // Metadata
    public System.DateTime saveTime;
    public string gameVersion;
}

/// <summary>
/// Información sobre un archivo de guardado.
/// </summary>
public class SaveFileInfo
{
    public string fileName;
    public string filePath;
    public System.DateTime lastModified;
    public long fileSize;

    public string GetFormattedSize()
    {
        if (fileSize < 1024) return $"{fileSize} B";
        if (fileSize < 1024 * 1024) return $"{fileSize / 1024f:0.##} KB";
        return $"{fileSize / (1024f * 1024f):0.##} MB";
    }

    public string GetFormattedDate()
    {
        return lastModified.ToString("dd/MM/yyyy HH:mm");
    }
}