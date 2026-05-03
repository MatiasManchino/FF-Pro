using System;
using UnityEngine;
using FreightForwarder.Models;
using FreightForwarder.Utils;

namespace FreightForwarder.Managers
{
    public class EconomyManager : Singleton<EconomyManager>
    {
        // =========================================================================
        // PROPIEDADES
        // =========================================================================
        
        public int Money { get; private set; }
        public int Reputation { get; private set; }
        public int Level { get; private set; }
        public int CurrentXP { get; private set; }
        
        // Estadísticas acumuladas
        public int TotalCargosCompleted { get; private set; }
        public int TotalCargosFailed { get; private set; }
        public int TotalRevenue { get; private set; }
        public int TotalCosts { get; private set; }
        public int TotalCargosAbandoned { get; private set; }
        
        // Costos mensuales
        public int MonthlyOfficeCosts { get; private set; }
        
        // =========================================================================
        // EVENTOS
        // =========================================================================
        
        public event Action<int> OnMoneyChanged;
        public event Action<int> OnReputationChanged;
        public event Action<int> OnLevelUp;
        public event Action<int, int> OnXPGained;
        public event Action OnGameOver;
        
        // =========================================================================
        // INICIALIZACIÓN
        // =========================================================================
        
        protected override void OnAwake()
        {
            ResetGame();
        }
        
        public void ResetGame()
        {
            Money = Constants.INITIAL_MONEY;
            Reputation = Constants.INITIAL_REPUTATION;
            Level = 1;
            CurrentXP = 0;
            
            TotalCargosCompleted = 0;
            TotalCargosFailed = 0;
            TotalRevenue = 0;
            TotalCosts = 0;
            TotalCargosAbandoned = 0;
            MonthlyOfficeCosts = 0;
            
            Debug.Log($"[EconomyManager] Reiniciado. Dinero: ${Money}, Reputación: {Reputation}");
        }
        
        // =========================================================================
        // MÉTODOS DE DINERO
        // =========================================================================
        
        public void AddMoney(int amount, string reason)
        {
            if (amount <= 0) return;
            
            Money += amount;
            TotalRevenue += amount;
            
            OnMoneyChanged?.Invoke(Money);
            Debug.Log($"[EconomyManager] +${amount} | {reason} | Total: ${Money}");
        }
        
        public bool SubtractMoney(int amount, string reason)
        {
            if (amount <= 0) return true;
            
            if (Money < amount)
            {
                Debug.LogWarning($"[EconomyManager] Fondos insuficientes para {reason}. Necesita ${amount}, tiene ${Money}");
                CheckGameOver();
                return false;
            }
            
            Money -= amount;
            TotalCosts += amount;
            
            OnMoneyChanged?.Invoke(Money);
            Debug.Log($"[EconomyManager] -${amount} | {reason} | Total: ${Money}");
            
            return true;
        }
        
        public bool TrySubtractMoney(int amount, string reason)
        {
            if (Money >= amount)
            {
                SubtractMoney(amount, reason);
                return true;
            }
            return false;
        }
        
        // =========================================================================
        // MÉTODOS DE REPUTACIÓN
        // =========================================================================
        
        public void AddReputation(int amount)
        {
            if (amount == 0) return;
            
            int oldReputation = Reputation;
            Reputation = Mathf.Clamp(Reputation + amount, 0, 100);
            
            if (Reputation != oldReputation)
            {
                OnReputationChanged?.Invoke(Reputation);
                Debug.Log($"[EconomyManager] Reputación {(amount > 0 ? "+" : "")}{amount} → {Reputation}/100");
            }
            
            if (Reputation <= 0)
            {
                OnGameOver?.Invoke();
            }
        }
        
        // =========================================================================
        // MÉTODOS DE XP Y NIVEL
        // =========================================================================
        
        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            
            CurrentXP += amount;
            OnXPGained?.Invoke(amount, CurrentXP);
            
            int xpNeeded = Level * Constants.XP_PER_LEVEL;
            
            while (CurrentXP >= xpNeeded)
            {
                CurrentXP -= xpNeeded;
                Level++;
                
                int levelUpBonus = Level * 100;
                AddMoney(levelUpBonus, $"Bono nivel {Level}");
                AddReputation(5);
                
                OnLevelUp?.Invoke(Level);
                Debug.Log($"[EconomyManager] 🎉 Subió al nivel {Level}! +${levelUpBonus} +5 reputación");
                
                xpNeeded = Level * Constants.XP_PER_LEVEL;
            }
        }
        
        // =========================================================================
        // MÉTODOS DE ESTADÍSTICAS
        // =========================================================================
        
        public void RecordCargoCompleted(int revenue, int cost)
        {
            TotalCargosCompleted++;
            AddMoney(revenue, $"Carga completada #{TotalCargosCompleted}");
            AddXP(Constants.XP_PER_CARGO);
        }
        
        public void RecordCargoFailed(int penalty = 0)
        {
            TotalCargosFailed++;
            
            if (penalty > 0)
            {
                SubtractMoney(penalty, $"Penalidad carga fallida #{TotalCargosFailed}");
            }
            
            AddReputation(-5);
        }
        
        public void RecordCargoAbandoned(int penalty)
        {
            TotalCargosAbandoned++;
            SubtractMoney(penalty, $"Carga abandonada #{TotalCargosAbandoned}");
            AddReputation(-10);
        }
        
        // =========================================================================
        // COSTOS MENSUALES
        // =========================================================================
        
        public void ProcessMonthlyCosts(int monthlyCosts)
        {
            MonthlyOfficeCosts = monthlyCosts;
            
            if (monthlyCosts > 0)
            {
                if (SubtractMoney(monthlyCosts, "Costos mensuales de oficinas"))
                {
                    Debug.Log($"[EconomyManager] Costos mensuales pagados: ${monthlyCosts}");
                }
                else
                {
                    AddReputation(-10);
                }
            }
        }
        
        // =========================================================================
        // UTILIDADES
        // =========================================================================
        
        public int GetNetProfit()
        {
            return TotalRevenue - TotalCosts;
        }
        
        public float GetSuccessRate()
        {
            int total = TotalCargosCompleted + TotalCargosFailed;
            if (total == 0) return 0.5f;
            return (float)TotalCargosCompleted / total;
        }
        
        public bool IsGameOver()
        {
            return Money <= Constants.GAME_OVER_DEBT_THRESHOLD || Reputation <= 0;
        }
        
        private void CheckGameOver()
        {
            if (IsGameOver())
            {
                OnGameOver?.Invoke();
            }
        }
        
        public static string FormatMoney(int amount)
        {
            return $"${amount:N0}";
        }

        // =========================================================================
        // RESTAURACIÓN DE ESTADO (PARA SAVE/LOAD)
        // =========================================================================
        
        public void RestoreState(int money, int reputation, int level, int currentXP,
                                 int totalCompleted, int totalFailed, int totalRevenue, 
                                 int totalCosts, int totalAbandoned)
        {
            Money = money;
            Reputation = reputation;
            Level = level;
            CurrentXP = currentXP;
            TotalCargosCompleted = totalCompleted;
            TotalCargosFailed = totalFailed;
            TotalRevenue = totalRevenue;
            TotalCosts = totalCosts;
            TotalCargosAbandoned = totalAbandoned;
            
            OnMoneyChanged?.Invoke(Money);
            OnReputationChanged?.Invoke(Reputation);
            
            Debug.Log($"[EconomyManager] Estado restaurado. Dinero: ${Money}, Reputación: {Reputation}");
        }
    }
}