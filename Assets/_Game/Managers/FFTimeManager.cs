using System;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
    /// <summary>
    /// Puente entre el TimeManager del mapa (UTC real) y el sistema de días del juego.
    /// No corre su propio timer: escucha TimeManager.OnNewDay y lleva el conteo de días de partida.
    /// </summary>
    public class FFTimeManager : Singleton<FFTimeManager>
    {
        public int CurrentDay { get; private set; }
        public DateTime CurrentDate { get; private set; }
        public float DayProgress { get; private set; }
        public float ContinuousDays { get; private set; }

        private int _previousMonth;

        public event Action OnDayPassed;
        public event Action OnMonthPassed;
        public event Action<DateTime> OnDateChanged;

        protected override void OnAwake()
        {
            CurrentDay = 0;
            CurrentDate = DateTime.UtcNow;
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
            {
                CurrentDate = TimeManager.Instance.CurrentUtcTime;
                _previousMonth = CurrentDate.Month;
                TimeManager.Instance.OnNewDay += HandleNewDay;
                TimeManager.Instance.OnNewMonth += HandleNewMonth;
            }
            else
            {
                Debug.LogWarning("[FFTimeManager] TimeManager no encontrado. El tiempo del juego no avanzará.");
            }
        }

        private void Update()
        {
            if (TimeManager.Instance != null)
                DayProgress = TimeManager.Instance.DayProgress;
        }

        private void HandleNewDay(DateTime date)
        {
            CurrentDay++;
            ContinuousDays++;
            CurrentDate = date;
            OnDayPassed?.Invoke();
            OnDateChanged?.Invoke(date);
        }

        private void HandleNewMonth(DateTime date)
        {
            OnMonthPassed?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnNewDay   -= HandleNewDay;
                TimeManager.Instance.OnNewMonth -= HandleNewMonth;
            }
        }

        public string GetFormattedDate()
            => CurrentDate.ToString("dd/MM/yyyy");
    }
}
