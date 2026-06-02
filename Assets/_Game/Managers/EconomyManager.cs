using System;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    public class EconomyManager : Singleton<EconomyManager>
    {
// Dinero.
        public int Money { get; private set; }
// Reputación.
        public int Reputation { get; private set; }
// Nivel.
        public int Level { get; private set; } = 1;
// Actual xp.
        public int CurrentXP { get; private set; }
// Gestiona total cargos completado.
        public int TotalCargosCompleted { get; private set; }
// Gestiona total cargos fallado.
        public int TotalCargosFailed { get; private set; }
// Gestiona total revenue.
        public int TotalRevenue { get; private set; }
// Gestiona total costs.
        public int TotalCosts { get; private set; }
// Gestiona total cargos abandoned.
        public int TotalCargosAbandoned { get; private set; }
// Gestiona monthly office costs.
        public int MonthlyOfficeCosts { get; private set; }

        public event Action<int> OnMoneyChanged;
        public event Action<int> OnReputationChanged;
        public event Action<int> OnLevelUp;
        public event Action<int, int> OnXPGained;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            Money = Constants.INITIAL_MONEY;
            Reputation = Constants.INITIAL_REPUTATION;
        }

        // ═══════════════════════════════════
        // DINERO
        // Añade dinero

        public void AddMoney(int amount, string reason = "")
        {
            Money += amount;
            TotalRevenue += amount;
            OnMoneyChanged?.Invoke(Money);
            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[Economy] +${amount} — {reason}. Total: ${Money}");
            CheckGameOver();
        }

// Gestiona subtract dinero.
        public bool SubtractMoney(int amount, string reason = "")
        {
            bool hadFunds = Money >= amount;
            Money -= amount;
            TotalCosts += amount;
            OnMoneyChanged?.Invoke(Money);
            if (!string.IsNullOrEmpty(reason))
                Debug.Log($"[Economy] -${amount} — {reason}. Total: ${Money}{(!hadFunds ? " [SIN FONDOS]" : "")}");
            CheckGameOver();
            return hadFunds;
        }

        // ═══════════════════════════════════
        // REPUTACIÓN
        // Añade reputación

        public void AddReputation(int amount)
        {
            Reputation = Mathf.Clamp(Reputation + amount, 0, 100);
            OnReputationChanged?.Invoke(Reputation);
            CheckGameOver();
        }

        // ═══════════════════════════════════
        // XP Y NIVELES
        // Añade xp

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

// Obtiene xp for next level
        public int GetXPForNextLevel() => Level * Constants.XP_PER_LEVEL;
// Obtiene xp progress
        public float GetXPProgress() => (float)CurrentXP / GetXPForNextLevel();

        // ═══════════════════════════════════
        // ESTADÍSTICAS DE CARGAS
        // Registra cargamento completado.

        public void RecordCargoCompleted(int revenue, int cost)
        {
            TotalCargosCompleted++;
            int profit = revenue - cost;
            AddMoney(profit, "Carga completada");
            AddXP(Constants.XP_PER_CARGO);
        }


        // Registra la entrega (estadística + XP) SIN acreditar dinero: la ganancia neta se
        // cobra de forma diferida según los términos del cliente, vía
        // <see cref="PaymentManager"/> (cuentas por cobrar).

        public void RecordCargoCompletedDeferred()
        {
            TotalCargosCompleted++;
            AddXP(Constants.XP_PER_CARGO);
        }


        // Paga al contado el costo del transportista (gasto de bolsillo). Puede dejar la caja
        // en negativo y disparar la bancarrota — es el riesgo del modelo de pago diferido.

        public void PayCarrierCost(int cost)
        {
            if (cost > 0) SubtractMoney(cost, "Pago al transportista");
        }

// Registra cargamento fallado.
        public void RecordCargoFailed(int penalty = 0)
        {
            TotalCargosFailed++;
            if (penalty > 0) SubtractMoney(penalty, "Penalidad por carga fallida");
            AddReputation(-5);
        }

// Registra cargamento abandoned.
        public void RecordCargoAbandoned(int penalty)
        {
            TotalCargosAbandoned++;
            if (penalty > 0) SubtractMoney(penalty, "Penalidad por carga abandonada");
            AddReputation(-10);
        }

// Gestiona process monthly costs.
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
        // Indica si juego terminado.

        public bool IsGameOver()
            => Money <= Constants.GAME_OVER_DEBT_THRESHOLD || Reputation <= 0;

// Verifica juego terminado.
        private void CheckGameOver()
        {
            if (IsGameOver())
                GameManager.Instance?.TriggerGameOver();
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
            OnMoneyChanged?.Invoke(Money);
            OnReputationChanged?.Invoke(Reputation);
        }

        // ═══════════════════════════════════
        // AUXILIARES
        // Obtiene net profit

        public int GetNetProfit() => TotalRevenue - TotalCosts;
// Obtiene success rate
        public float GetSuccessRate()
            => (TotalCargosCompleted + TotalCargosFailed) == 0
               ? 0f
               : (float)TotalCargosCompleted / (TotalCargosCompleted + TotalCargosFailed);
    }
}