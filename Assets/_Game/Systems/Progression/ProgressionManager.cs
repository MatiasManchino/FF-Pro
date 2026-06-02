using System;
using System.Collections.Generic;
using FreightForwarder.Managers;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Systems.Progression
{

    // Controla desbloqueos: ciudades, rutas, oficinas y tier del jugador.
    // Escucha EconomyManager.OnLevelUp y CargoManager.OnCargoCompleted.
    // No modifica los managers existentes.

    public class ProgressionManager : Singleton<ProgressionManager>
    {
        // Gestiona office count.

        public int   OfficeCount    { get; private set; } = 1;
// Jugador categoría.
        public int   PlayerTier     { get; private set; } = 1;
// Devuelve la office upgrade cost
        public float OfficeUpgradeCost => 5000f * OfficeCount;

        // ── Ciudades desbloqueadas por tier ──────────────────────────────────

        private static readonly Dictionary<int, string[]> TierCityUnlocks = new Dictionary<int, string[]>
        {
            { 1, new[] { "buenos_aires", "sao_paulo", "miami" }},
            { 2, new[] { "rotterdam", "hamburg", "antwerp", "los_angeles" }},
            { 3, new[] { "shanghai", "dubai", "singapore", "tokyo" }},
            { 4, new[] { "new_york", "hong_kong", "sydney", "mumbai" }},
            { 5, new[] { "vancouver", "busan", "vladivostok", "cape_town", "johannesburg" }},
        };

        private static readonly Dictionary<int, int> TierXPThreshold = new Dictionary<int, int>
        {
            { 1, 0 }, { 2, 500 }, { 3, 2000 }, { 4, 5000 }, { 5, 12000 }
        };

        // ── Eventos ───────────────────────────────────────────────────────────

        public event Action<int>       OnTierUp;
        public event Action<string>    OnCityUnlocked;
        public event Action<int>       OnOfficeOpened;
        public event Action<string>    OnMilestoneReached;

        // Se ejecuta al iniciar el componente.

        private void Start()
        {
            ApplyTierUnlocks(1);

            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLevelUp  += OnPlayerLevelUp;
                EconomyManager.Instance.OnXPGained += OnXPGained;
            }

            if (CargoManager.Instance != null)
                CargoManager.Instance.OnCargoCompleted += OnCargoCompleted;
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnLevelUp  -= OnPlayerLevelUp;
                EconomyManager.Instance.OnXPGained -= OnXPGained;
            }
            if (CargoManager.Instance != null)
                CargoManager.Instance.OnCargoCompleted -= OnCargoCompleted;
        }

        // Se invoca cuando el jugador sube de nivel.

        private void OnPlayerLevelUp(int newLevel)
        {
            int newTier = ComputeTier(EconomyManager.Instance?.CurrentXP ?? 0, newLevel);
            if (newTier > PlayerTier)
            {
                PlayerTier = newTier;
                ApplyTierUnlocks(newTier);
                OnTierUp?.Invoke(newTier);
                Debug.Log($"[Progression] Tier up: {newTier}");
            }

            // Abre oficina automáticamente en niveles clave
            if (newLevel == 3 || newLevel == 6 || newLevel == 10 || newLevel == 15)
                OpenOffice();

            CheckMilestones(newLevel);
        }

// Se invoca cuando el jugador gana experiencia.
        private void OnXPGained(int gained, int total)
        {
            int newTier = ComputeTierByXP(total);
            if (newTier > PlayerTier)
            {
                PlayerTier = newTier;
                ApplyTierUnlocks(newTier);
                OnTierUp?.Invoke(newTier);
            }
        }

// Se invoca cuando un cargamento se completa.
        private void OnCargoCompleted(Cargo cargo)
        {
            int completed = EconomyManager.Instance?.TotalCargosCompleted ?? 0;
            if (completed == 10)  OnMilestoneReached?.Invoke("¡10 cargas completadas! Reputación +10.");
            if (completed == 50)  OnMilestoneReached?.Invoke("¡50 cargas! Bono de $2,000.");
            if (completed == 100) OnMilestoneReached?.Invoke("¡100 cargas! Tier especial desbloqueado.");
        }

        // ── Lógica de desbloqueo ──────────────────────────────────────────────

        private void ApplyTierUnlocks(int tier)
        {
            if (!TierCityUnlocks.TryGetValue(tier, out string[] cities)) return;
// Foreach
            foreach (var cityId in cities)
            {
                WorldCity city = CityDatabase.GetCity(cityId);
                if (city != null && !city.IsUnlocked)
                {
                    city.IsUnlocked = true;
                    OnCityUnlocked?.Invoke(cityId);
                    Debug.Log($"[Progression] Ciudad desbloqueada: {city.DisplayName}");
                }
            }
        }

// Intenta open office
        public bool TryOpenOffice()
        {
            int cost = (int)OfficeUpgradeCost;
            if (EconomyManager.Instance == null) return false;
            if (EconomyManager.Instance.Money < cost) return false;

            EconomyManager.Instance.SubtractMoney(cost, $"Apertura de oficina #{OfficeCount + 1}");
            OpenOffice();
            return true;
        }

// Abre office.
        private void OpenOffice()
        {
            OfficeCount++;
            OnOfficeOpened?.Invoke(OfficeCount);

            // ≥3 oficinas → desbloquea ciudades extra
            if (OfficeCount >= 3)
            {
                var city = CityDatabase.GetCity("new_york");
                if (city != null && !city.IsUnlocked)
                {
                    city.IsUnlocked = true;
                    OnCityUnlocked?.Invoke("new_york");
                }
            }

            Debug.Log($"[Progression] Oficina abierta. Total: {OfficeCount}");
        }

        // Calcula nivel

        private int ComputeTier(int xp, int level)
        {
            int tier = 1;
// Foreach
            foreach (var kv in TierXPThreshold)
                if (level >= kv.Key && xp >= kv.Value) tier = kv.Key;
            return tier;
        }

// Calcula nivel by xp
        private int ComputeTierByXP(int totalXP)
        {
            int tier = 1;
// Foreach
            foreach (var kv in TierXPThreshold)
                if (totalXP >= kv.Value) tier = kv.Key;
            return tier;
        }

// Verifica milestones.
        private void CheckMilestones(int level)
        {
            switch (level)
            {
                case 5:  OnMilestoneReached?.Invoke("¡Nivel 5! Acceso a rutas asiáticas."); break;
                case 10: OnMilestoneReached?.Invoke("¡Nivel 10! Agentes premium disponibles."); break;
                case 20: OnMilestoneReached?.Invoke("¡Nivel 20! Maestro de la logística global."); break;
            }
        }

// Obtiene ciudades unlocked
        public int GetCitiesUnlocked() => CityDatabase.AllCities?.Count ?? 0;
    }
}