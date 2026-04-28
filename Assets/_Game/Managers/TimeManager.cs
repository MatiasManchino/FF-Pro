using System;
using UnityEngine;
using FreightForwarder.Models;
using FreightForwarder.Utils;

namespace FreightForwarder.Managers
{
    public class TimeManager : Singleton<TimeManager>
    {
        [SerializeField] private float _dayDurationSeconds = Constants.DAY_DURATION_SECONDS;
        [SerializeField] private int _startYear = 2025;
        [SerializeField] private int _startMonth = 1;
        [SerializeField] private int _startDay = 1;
        
        public int CurrentDay { get; private set; } = 1;
        public DateTime CurrentDate { get; private set; }
        public float TimeScale { get; private set; } = 1f;
        public bool IsPaused => TimeScale == 0f;
        public float DayProgress { get; private set; }
        public float ContinuousDays { get; private set; }
        
        private float _accumulatedTime;
        
        public event Action OnDayPassed;
        public event Action OnMonthPassed;
        public event Action<DateTime> OnDateChanged;
        
        protected override void OnAwake()
        {
            CurrentDate = new DateTime(_startYear, _startMonth, _startDay);
            CurrentDay = 1;
            TimeScale = 1f;
            _accumulatedTime = 0f;
            DayProgress = 0f;
            ContinuousDays = 0f;
            Debug.Log($"[TimeManager] Iniciado en {CurrentDate:dd/MM/yyyy}");
        }
        
        private void Update()
        {
            if (IsPaused) return;
            
            _accumulatedTime += Time.deltaTime * TimeScale;
            DayProgress = Mathf.Clamp01(_accumulatedTime / _dayDurationSeconds);
            ContinuousDays += Time.deltaTime * TimeScale / _dayDurationSeconds;
            
            if (_accumulatedTime >= _dayDurationSeconds)
            {
                _accumulatedTime -= _dayDurationSeconds;
                AdvanceDay();
            }
        }
        
        private void AdvanceDay()
        {
            CurrentDate = CurrentDate.AddDays(1);
            CurrentDay++;
            ContinuousDays = CurrentDay - 1 + DayProgress;
            
            OnDayPassed?.Invoke();
            OnDateChanged?.Invoke(CurrentDate);
            
            if (CurrentDate.Day == 1)
            {
                OnMonthPassed?.Invoke();
            }
            
            Debug.Log($"[TimeManager] Día {CurrentDay} | Fecha: {CurrentDate:dd/MM/yyyy}");
        }
        
        public void SetTimeScale(float scale)
        {
            TimeScale = Mathf.Clamp(scale, 0f, 3f);
            Debug.Log($"[TimeManager] Velocidad cambiada a x{TimeScale}");
        }
        
        public void Pause() => SetTimeScale(0f);
        public void Resume() => SetTimeScale(1f);
        
        public string GetFormattedDate() => CurrentDate.ToString("dd MMMM yyyy");
        public string GetShortDate() => CurrentDate.ToString("dd/MM/yyyy");
    }
}