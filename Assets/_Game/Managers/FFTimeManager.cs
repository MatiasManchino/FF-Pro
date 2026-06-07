using System;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{

    // ═══════════════════════════════════════════════════════════════════════
    //  FFTimeManager — CONTADOR DE DÍAS DE PARTIDA (capa "núcleo" / juego)
    // ═══════════════════════════════════════════════════════════════════════
    //  NO es un reloj y NO corre su propio timer. Es un adaptador finito que se
    //  monta ENCIMA del TimeManager (el motor del reloj, en Assets/_Game/World):
    //  escucha su OnNewDay/OnNewMonth y lleva el conteo de "días de partida"
    //  (CurrentDay, ContinuousDays) que usa la lógica del juego (economía, etc.).
    //
    //  ⚠️ Depende de TimeManager. No fusionar ni borrar TimeManager: esta clase
    //     quedaría sin fuente de tiempo. Son dos capas, a propósito.
    // ═══════════════════════════════════════════════════════════════════════
    public class FFTimeManager : Singleton<FFTimeManager>
    {
// Actual día.
        public int CurrentDay { get; private set; }
// Actual date.
        public DateTime CurrentDate { get; private set; }
// Día progress.
        public float DayProgress { get; private set; }
// Gestiona continuous días.
        public float ContinuousDays { get; private set; }

        private int _previousMonth;

        public event Action OnDayPassed;
        public event Action OnMonthPassed;
        public event Action<DateTime> OnDateChanged;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake()
        {
            CurrentDay = 0;
            CurrentDate = DateTime.UtcNow;
        }

// Se ejecuta al iniciar el componente.
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

// Ejecuta las comprobaciones necesarias en cada fotograma del juego.
        private void Update()
        {
            if (TimeManager.Instance != null)
                DayProgress = TimeManager.Instance.DayProgress;
        }

// Gestiona nuevo día.
        private void HandleNewDay(DateTime date)
        {
            CurrentDay++;
            ContinuousDays++;
            CurrentDate = date;
            OnDayPassed?.Invoke();
            OnDateChanged?.Invoke(date);
        }

// Gestiona nuevo mes.
        private void HandleNewMonth(DateTime date)
        {
            OnMonthPassed?.Invoke();
        }

// Elimina el marcador del registro y destruye su label al destruir el objeto.
        protected override void OnDestroy()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnNewDay   -= HandleNewDay;
                TimeManager.Instance.OnNewMonth -= HandleNewMonth;
            }
        }

// Obtiene formatted date
        public string GetFormattedDate()
            => CurrentDate.ToString("dd/MM/yyyy");
    }
}