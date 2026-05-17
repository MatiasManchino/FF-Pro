using System;
using FreightForwarder.Managers;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Systems.World
{
    /// <summary>
    /// Estado económico global del mundo: combustible, demanda, riesgo.
    /// Los managers existentes leen estos multiplicadores para escalar precios/probabilidades.
    /// Se actualiza mensualmente y puede ser afectado por noticias y eventos globales.
    /// </summary>
    public class WorldStateManager : Singleton<WorldStateManager>
    {
        // ── Multiplicadores globales ──────────────────────────────────────────

        public float FuelMultiplier   { get; private set; } = 1f;
        public float DemandMultiplier { get; private set; } = 1f;
        public float RiskMultiplier   { get; private set; } = 1f;

        // Tendencias suaves (lerp mensual)
        private float _targetFuel   = 1f;
        private float _targetDemand = 1f;
        private float _targetRisk   = 1f;

        // ── Eventos ───────────────────────────────────────────────────────────

        public event Action<string, float> OnFuelChanged;
        public event Action<string, float> OnDemandChanged;
        public event Action<string, float> OnRiskChanged;

        // ── Historial para UI ─────────────────────────────────────────────────

        public float FuelHistory_1M   { get; private set; } = 1f;
        public float DemandHistory_1M { get; private set; } = 1f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnMonthPassed += OnMonthPassed;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (FFTimeManager.Instance != null)
                FFTimeManager.Instance.OnMonthPassed -= OnMonthPassed;
        }

        // ── Tick mensual ──────────────────────────────────────────────────────

        private void OnMonthPassed()
        {
            FuelHistory_1M   = FuelMultiplier;
            DemandHistory_1M = DemandMultiplier;

            // Movimiento aleatorio suave de los multiplicadores
            _targetFuel   = Mathf.Clamp(_targetFuel   + UnityEngine.Random.Range(-0.15f, 0.20f), 0.6f, 2.5f);
            _targetDemand = Mathf.Clamp(_targetDemand + UnityEngine.Random.Range(-0.10f, 0.15f), 0.5f, 2.0f);
            _targetRisk   = Mathf.Clamp(_targetRisk   + UnityEngine.Random.Range(-0.08f, 0.12f), 0.7f, 2.0f);

            FuelMultiplier   = Mathf.Lerp(FuelMultiplier,   _targetFuel,   0.4f);
            DemandMultiplier = Mathf.Lerp(DemandMultiplier, _targetDemand, 0.4f);
            RiskMultiplier   = Mathf.Lerp(RiskMultiplier,   _targetRisk,   0.4f);

            float fuelDelta   = FuelMultiplier - FuelHistory_1M;
            float demandDelta = DemandMultiplier - DemandHistory_1M;

            if (Mathf.Abs(fuelDelta) > 0.05f)
                OnFuelChanged?.Invoke(fuelDelta > 0 ? "Precio del combustible sube." : "Precio del combustible baja.", FuelMultiplier);

            if (Mathf.Abs(demandDelta) > 0.05f)
                OnDemandChanged?.Invoke(demandDelta > 0 ? "Demanda global en alza." : "Demanda global en baja.", DemandMultiplier);

            if (DebugFlags.LOG_EVENTS)
                Debug.Log($"[WorldState] Fuel={FuelMultiplier:F2} Demand={DemandMultiplier:F2} Risk={RiskMultiplier:F2}");
        }

        // ── API para aplicar shocks externos (guerras, crisis, etc.) ─────────

        public void ApplyFuelShock(float delta, string reason)
        {
            _targetFuel = Mathf.Clamp(_targetFuel + delta, 0.6f, 2.5f);
            OnFuelChanged?.Invoke(reason, _targetFuel);
            Debug.Log($"[WorldState] FuelShock: {delta:+0.00} — {reason}");
        }

        public void ApplyDemandShock(float delta, string reason)
        {
            _targetDemand = Mathf.Clamp(_targetDemand + delta, 0.5f, 2.0f);
            OnDemandChanged?.Invoke(reason, _targetDemand);
        }

        public void ApplyRiskShock(float delta, string reason)
        {
            _targetRisk = Mathf.Clamp(_targetRisk + delta, 0.7f, 2.0f);
            OnRiskChanged?.Invoke(reason, _targetRisk);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public string GetFuelTrend()
        {
            float d = FuelMultiplier - FuelHistory_1M;
            if (d >  0.05f) return "↑";
            if (d < -0.05f) return "↓";
            return "→";
        }

        public string GetDemandTrend()
        {
            float d = DemandMultiplier - DemandHistory_1M;
            if (d >  0.05f) return "↑";
            if (d < -0.05f) return "↓";
            return "→";
        }
    }
}
