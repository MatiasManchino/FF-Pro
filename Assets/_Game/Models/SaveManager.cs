using System;
using System.Collections.Generic;
using FreightForwarder.Models;

namespace FreightForwarder.Models
{
    /// <summary>
    /// SaveData.cs — Contenedor serializable para guardar la partida completa.
    /// 
    /// QUÉ ES [Serializable]?
    /// Permite que esta clase se convierta a JSON y se guarde en disco.
    /// 
    /// QUÉ ES VERSION?
    /// Si en el futuro cambiamos la estructura del save, podemos incrementar
    /// la versión y manejar migraciones.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // =========================================================================
        // VERSIÓN DEL SAVE
        // =========================================================================
        
        public int SaveVersion { get; set; } = 1;
        public string SaveDate { get; set; }
        
        // =========================================================================
        // DATOS DE LA EMPRESA
        // =========================================================================
        
        public string CompanyName { get; set; }
        
        // =========================================================================
        // DATOS ECONÓMICOS
        // =========================================================================
        
        public int Money { get; set; }
        public int Reputation { get; set; }
        public int Level { get; set; }
        public int CurrentXP { get; set; }
        
        // =========================================================================
        // ESTADÍSTICAS ACUMULADAS
        // =========================================================================
        
        public int TotalCargosCompleted { get; set; }
        public int TotalCargosFailed { get; set; }
        public int TotalRevenue { get; set; }
        public int TotalCosts { get; set; }
        public int TotalCargosAbandoned { get; set; }
        
        // =========================================================================
        // FECHA DEL JUEGO
        // =========================================================================
        
        public int CurrentDay { get; set; }
        public DateTime CurrentDate { get; set; }
        public float ContinuousDays { get; set; }
        
        // =========================================================================
        // CARGAS
        // =========================================================================
        
        public List<Cargo> MarketCargos { get; set; }
        public List<Cargo> ActiveCargos { get; set; }
        public List<Cargo> CompletedCargos { get; set; }
        public List<Cargo> FailedCargos { get; set; }
        
        // =========================================================================
        // CLIENTES
        // =========================================================================
        
        public List<Client> Clients { get; set; }
        public Dictionary<string, float> ClientRelationships { get; set; }
        
        // =========================================================================
        // AGENTES
        // =========================================================================
        
        public List<Agent> Agents { get; set; }
        public Dictionary<string, List<string>> AgentActiveCargos { get; set; }
        
        // =========================================================================
        // OFICINAS Y CIUDADES
        // =========================================================================
        
        public Dictionary<string, int> Offices { get; set; }  // cityId -> level
        public List<string> UnlockedCityIds { get; set; }
        
        // =========================================================================
        // COTIZACIONES PENDIENTES
        // =========================================================================
        
        public List<Quote> PendingQuotes { get; set; }
        
        // =========================================================================
        // EVENTOS MUNDIALES ACTIVOS
        // =========================================================================
        
        public List<string> ActiveWorldEventIds { get; set; }
        
        // =========================================================================
        // CONSTRUCTOR
        // =========================================================================
        
        public SaveData()
        {
            MarketCargos = new List<Cargo>();
            ActiveCargos = new List<Cargo>();
            CompletedCargos = new List<Cargo>();
            FailedCargos = new List<Cargo>();
            Clients = new List<Client>();
            ClientRelationships = new Dictionary<string, float>();
            Agents = new List<Agent>();
            AgentActiveCargos = new Dictionary<string, List<string>>();
            Offices = new Dictionary<string, int>();
            UnlockedCityIds = new List<string>();
            PendingQuotes = new List<Quote>();
            ActiveWorldEventIds = new List<string>();
            SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}