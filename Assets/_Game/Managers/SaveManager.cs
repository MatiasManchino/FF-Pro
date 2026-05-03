using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FreightForwarder.Models;
using FreightForwarder.Managers;
using FreightForwarder.Utils;

namespace FreightForwarder.Managers
{
    /// <summary>
    /// SaveManager.cs — Gestiona la persistencia de la partida.
    /// 
    /// RESPONSABILIDADES:
    /// - Serializar/Deserializar datos con JsonUtility
    /// - Guardar en Application.persistentDataPath
    /// - Recolectar datos de todos los Managers al guardar
    /// - Restaurar estado completo al cargar
    /// </summary>
    public class SaveManager : Singleton<SaveManager>
    {
        private const string SAVE_FILENAME = "savegame.json";
        
        public string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILENAME);
        
        public bool IsSaveAvailable => File.Exists(SavePath);
        
        // =========================================================================
        // EVENTOS
        // =========================================================================
        
        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;
        public event Action<string> OnSaveFailed;
        public event Action<string> OnLoadFailed;
        
        // =========================================================================
        // MÉTODOS PÚBLICOS
        // =========================================================================
        
        /// <summary>
        /// Guarda la partida actual recolectando datos de todos los managers.
        /// </summary>
        public void SaveGame(string companyName = "")
        {
            try
            {
                var saveData = new SaveData
                {
                    SaveVersion = 1,
                    SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    CompanyName = companyName
                };
                
                // Recolectar datos de EconomyManager
                if (EconomyManager.Instance != null)
                {
                    saveData.Money = EconomyManager.Instance.Money;
                    saveData.Reputation = EconomyManager.Instance.Reputation;
                    saveData.Level = EconomyManager.Instance.Level;
                    saveData.CurrentXP = EconomyManager.Instance.CurrentXP;
                    saveData.TotalCargosCompleted = EconomyManager.Instance.TotalCargosCompleted;
                    saveData.TotalCargosFailed = EconomyManager.Instance.TotalCargosFailed;
                    saveData.TotalRevenue = EconomyManager.Instance.TotalRevenue;
                    saveData.TotalCosts = EconomyManager.Instance.TotalCosts;
                    saveData.TotalCargosAbandoned = EconomyManager.Instance.TotalCargosAbandoned;
                }
                
                // Recolectar datos de TimeManager
                if (TimeManager.Instance != null)
                {
                    saveData.CurrentDay = TimeManager.Instance.CurrentDay;
                    saveData.CurrentDate = TimeManager.Instance.CurrentDate;
                    saveData.ContinuousDays = TimeManager.Instance.ContinuousDays;
                }
                
                // Recolectar datos de CargoManager
                if (CargoManager.Instance != null)
                {
                    saveData.MarketCargos = new List<Cargo>(CargoManager.Instance.MarketCargos);
                    saveData.ActiveCargos = new List<Cargo>(CargoManager.Instance.ActiveCargos);
                    saveData.CompletedCargos = new List<Cargo>(CargoManager.Instance.CompletedCargos);
                    saveData.FailedCargos = new List<Cargo>(CargoManager.Instance.FailedCargos);
                }
                
                // Recolectar datos de ClientManager
                if (ClientManager.Instance != null)
                {
                    saveData.Clients = new List<Client>(ClientManager.Instance.Clients.Values);
                    saveData.ClientRelationships = new Dictionary<string, float>(ClientManager.Instance.RelationshipWithClients);
                }
                
                // Recolectar datos de AgentManager
                if (AgentManager.Instance != null)
                {
                    saveData.Agents = new List<Agent>(AgentManager.Instance.GetAllAgents());
                    saveData.AgentActiveCargos = new Dictionary<string, List<string>>(AgentManager.Instance.GetAgentActiveCargos());
                }
                
                // Recolectar ciudades desbloqueadas
                if (CargoManager.Instance != null)
                {
                    saveData.UnlockedCityIds = new List<string>(CargoManager.Instance.GetUnlockedCityIds());
                }
                
                // Recolectar cotizaciones pendientes directamente de ClientManager
                if (ClientManager.Instance != null)
                {
                    saveData.PendingQuotes = new List<Quote>();
                    foreach (var kvp in ClientManager.Instance.PendingQuotes)
                    {
                        saveData.PendingQuotes.AddRange(kvp.Value);
                    }
                }
                
                // Serializar y guardar
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(SavePath, json);
                
                Debug.Log($"[SaveManager] Partida guardada en: {SavePath}");
                OnSaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error al guardar: {ex.Message}");
                OnSaveFailed?.Invoke(ex.Message);
            }
        }
        
        /// <summary>
        /// Carga la partida guardada y restaura el estado.
        /// </summary>
        public bool LoadGame()
        {
            try
            {
                if (!IsSaveAvailable)
                {
                    Debug.LogWarning("[SaveManager] No hay partida guardada.");
                    OnLoadFailed?.Invoke("No save file found.");
                    return false;
                }
                
                string json = File.ReadAllText(SavePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);
                
                if (saveData == null)
                {
                    Debug.LogError("[SaveManager] Error: save data is null or corrupt.");
                    OnLoadFailed?.Invoke("Corrupt save file.");
                    return false;
                }
                
                // Restaurar EconomyManager
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.RestoreState(
                        saveData.Money,
                        saveData.Reputation,
                        saveData.Level,
                        saveData.CurrentXP,
                        saveData.TotalCargosCompleted,
                        saveData.TotalCargosFailed,
                        saveData.TotalRevenue,
                        saveData.TotalCosts,
                        saveData.TotalCargosAbandoned
                    );
                }
                
                // Restaurar TimeManager
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.RestoreState(
                        saveData.CurrentDay,
                        saveData.CurrentDate,
                        saveData.ContinuousDays
                    );
                }
                
                // Restaurar CargoManager
                if (CargoManager.Instance != null)
                {
                    CargoManager.Instance.RestoreState(
                        saveData.MarketCargos ?? new List<Cargo>(),
                        saveData.ActiveCargos ?? new List<Cargo>(),
                        saveData.CompletedCargos ?? new List<Cargo>(),
                        saveData.FailedCargos ?? new List<Cargo>(),
                        saveData.UnlockedCityIds ?? new List<string>(),
                        saveData.PendingQuotes ?? new List<Quote>()
                    );
                }
                
                // Restaurar ClientManager
                if (ClientManager.Instance != null)
                {
                    ClientManager.Instance.RestoreState(
                        saveData.Clients ?? new List<Client>(),
                        saveData.ClientRelationships ?? new Dictionary<string, float>()
                    );
                }
                
                // Restaurar AgentManager
                if (AgentManager.Instance != null)
                {
                    AgentManager.Instance.RestoreState(
                        saveData.Agents ?? new List<Agent>(),
                        saveData.AgentActiveCargos ?? new Dictionary<string, List<string>>()
                    );
                }
                
                Debug.Log($"[SaveManager] Partida cargada (v{saveData.SaveVersion}) - {saveData.SaveDate}");
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error al cargar: {ex.Message}");
                OnLoadFailed?.Invoke(ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Elimina la partida guardada.
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (IsSaveAvailable)
                {
                    File.Delete(SavePath);
                    Debug.Log("[SaveManager] Partida eliminada.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error al eliminar: {ex.Message}");
            }
        }
    }
}
