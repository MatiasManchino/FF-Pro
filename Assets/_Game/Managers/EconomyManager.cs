using System;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class EconomyManager : Singleton<EconomyManager>
    {
        public int Money { get; private set; }
        public int Reputation { get; private set; }
        public int Level { get; private set; } = 1;
        public int CurrentXP { get; private set; }
        public int TotalCargosCompleted { get; private set; }
        public int TotalCargosFailed { get; private set; }
        public int TotalRevenue { get; private set; }
        public int TotalCosts { get; private set; }
        public int TotalCargosAbandoned { get; private set; }
        public int MonthlyOfficeCosts { get; private set; }

        public event Action<int> OnMoneyChanged;
        public event Action<int> OnReputationChanged;
        public event Action<int> OnLevelUp;
        public event Action<int, int> OnXPGained;
        public event Action OnGameOver;

        protected override void OnAwake()
        {
            Money = Constants.INITIAL_MONEY;
            Reputation = Constants.INITIAL_REPUTATION;
        }

        // ═══════════════════════════════════
        // DINERO
        // ═══════════════════════════════════

        public void AddMoney(int amount, string reason = "")
        {
            Money += amount;
            TotalRevenue += amount;
            OnMoneyChanged?.Invoke(Money);
            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[Economy] +${amount} — {reason}. Total: ${Money}");
            CheckGameOver();
        }

        public bool SubtractMoney(int amount, string reason = "")
        {
            Money -= amount;
            TotalCosts += amount;
            OnMoneyChanged?.Invoke(Money);
            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[Economy] -${amount} — {reason}. Total: ${Money}");
            CheckGameOver();
            return true;
        }

        // ═══════════════════════════════════
        // REPUTACIÓN
        // ═══════════════════════════════════

        public void AddReputation(int amount)
        {
            Reputation = Mathf.Clamp(Reputation + amount, 0, 100);
            OnReputationChanged?.Invoke(Reputation);
            CheckGameOver();
        }

        // ═══════════════════════════════════
        // XP Y NIVELES
        // ═══════════════════════════════════

        public void AddXP(int amount)
        {
            CurrentXP += amount;
            OnXPGained?.Invoke(amount, CurrentXP);

            int xpNeeded = Level * Constants.XP_PER_LEVEL;
            while (CurrentXP >= xpNeeded)
            {
                CurrentXP -= xpNeeded;
                Level++;
                int bonus = Level * 100;
                AddMoney(bonus, $"Level up a Nivel {Level}");
                AddReputation(5);
                OnLevelUp?.Invoke(Level);
                Debug.Log($"[Economy] ¡Level Up! Nivel {Level}. Bonus: ${bonus}");
                xpNeeded = Level * Constants.XP_PER_LEVEL;
            }
        }

        public int GetXPForNextLevel() => Level * Constants.XP_PER_LEVEL;
        public float GetXPProgress() => (float)CurrentXP / GetXPForNextLevel();

        // ═══════════════════════════════════
        // ESTADÍSTICAS DE CARGAS
        // ═══════════════════════════════════

        public void RecordCargoCompleted(int revenue, int cost)
        {
            TotalCargosCompleted++;
            int profit = revenue - cost;
            AddMoney(profit, "Carga completada");
            AddXP(Constants.XP_PER_CARGO);
        }

        public void RecordCargoFailed(int penalty = 0)
        {
            TotalCargosFailed++;
            if (penalty > 0) SubtractMoney(penalty, "Penalidad por carga fallida");
            AddReputation(-5);
        }

        public void RecordCargoAbandoned(int penalty)
        {
            TotalCargosAbandoned++;
            if (penalty > 0) SubtractMoney(penalty, "Penalidad por carga abandonada");
            AddReputation(-10);
        }

        public void ProcessMonthlyCosts(int monthlyCosts)
        {
            MonthlyOfficeCosts = monthlyCosts;
            if (monthlyCosts <= 0) return;

            if (Money >= monthlyCosts)
            {
                SubtractMoney(monthlyCosts, "Costos mensuales de oficinas");
            }
            else
            {
                SubtractMoney(monthlyCosts, "Costos mensuales (fondos insuficientes)");
                AddReputation(-10);
                Debug.LogWarning("[Economy] No hubo fondos para los costos mensuales. -10 reputación.");
            }
        }

        // ═══════════════════════════════════
        // GAME OVER
        // ═══════════════════════════════════

        public bool IsGameOver()
            => Money <= Constants.GAME_OVER_DEBT_THRESHOLD || Reputation <= 0;

        private void CheckGameOver()
        {
            if (IsGameOver())
            {
                OnGameOver?.Invoke();
                GameManager.Instance?.TriggerGameOver();
            }
        }

        // ═══════════════════════════════════
        // SAVE / RESTORE
        // ═══════════════════════════════════

        public void RestoreState(int money, int reputation, int level, int xp,
                                  int completed, int failed, int revenue, int costs)
        {
            Money = money;
            Reputation = reputation;
            Level = level;
            CurrentXP = xp;
            TotalCargosCompleted = completed;
            TotalCargosFailed = failed;
            TotalRevenue = revenue;
            TotalCosts = costs;
        }

        // ═══════════════════════════════════
        // AUXILIARES
        // ═══════════════════════════════════

        public int GetNetProfit() => TotalRevenue - TotalCosts;
        public float GetSuccessRate()
            => (TotalCargosCompleted + TotalCargosFailed) == 0
               ? 0f
               : (float)TotalCargosCompleted / (TotalCargosCompleted + TotalCargosFailed);
    }
}
