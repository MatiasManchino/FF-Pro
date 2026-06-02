using System;
using System.Collections.Generic;
using FreightForwarder.Models;
using FreightForwarder.Utils;
using UnityEngine;

namespace FreightForwarder.Managers
{
// Pago timing.
    public enum PaymentTiming { OnTime, Early, Late }

    // Un cobro pendiente de un cliente (cuenta por cobrar).
    [Serializable]
    public class PendingPayment
    {
        public string CargoId;
        public string ClientName;
        public string OriginCityId;
        public string DestinationCityId;
// Ejecuta cobrar
        public int    Amount;        // monto a cobrar (ya con penalización por mora si aplica)
        public int    QuotedAmount;  // precio originalmente cotizado
        public int    DueDay;        // día absoluto en que se cobra
        public int    CreatedDay;
        public PaymentTiming Timing;

// Días remaining.
        public int DaysRemaining(int currentDay) => Mathf.Max(0, DueDay - currentDay);
    }


    // Cuentas por cobrar: el ingreso del cliente se acredita de forma DIFERIDA según su tipo
    // (al contado, +15/30/45 días, anticipo, etc.). Genera presión de caja independiente
    // de la ganancia. Lo conduce <see cref="CargoManager"/> desde su tick diario.

    public class PaymentManager : Singleton<PaymentManager>
    {
        private readonly List<PendingPayment> _pending = new List<PendingPayment>();

// Devuelve el pending
        public IReadOnlyList<PendingPayment> Pending => _pending;
// Devuelve el pending cantidad
        public int PendingCount => _pending.Count;
        public int TotalReceivable
        {
            get { int s = 0; foreach (var p in _pending) s += p.Amount; return s; }
        }

        public event Action<PendingPayment> OnPaymentScheduled;
        public event Action<PendingPayment> OnPaymentReceived;

// Se ejecuta durante Awake al iniciar el componente.
        protected override void OnAwake() { _pending.Clear(); }


        // Programa el cobro del cliente por una carga completada, según el comportamiento
        // de pago de ese cliente (adelanto / al contado / atraso con posible penalización).

        public void SchedulePayment(Cargo cargo, int currentDay, int amountToDefer)
        {
            if (cargo == null) return;
            // Monto a cobrar del cliente, decidido por quien llama (neto en gracia, bruto luego).
            int revenue = amountToDefer;
            if (revenue <= 0) return;

            // Cliente real si existe; si no, uno temporal con el comportamiento del tipo.
            var client = ClientManager.Instance?.GetClientById(cargo.ClientId)
                         ?? new Client(cargo.ClientName, cargo.ClientType);

            int delay = Mathf.Max(0, client.PaymentDelay);
            var timing = PaymentTiming.OnTime;

            if (client.WillPayEarly())
            {
                delay  = Mathf.Max(0, delay - 7);
                timing = PaymentTiming.Early;
            }
            else if (client.WillPayLate())
            {
                delay += UnityEngine.Random.Range(5, 11); // +5..+10 días
                timing = PaymentTiming.Late;
            }

            int amount = revenue;
            if (timing == PaymentTiming.Late && client.LatePaymentPenalty > 0f)
                amount = Mathf.Max(1, Mathf.RoundToInt(revenue * (1f - client.LatePaymentPenalty)));

            var payment = new PendingPayment
            {
                CargoId           = cargo.Id,
                ClientName        = string.IsNullOrEmpty(cargo.ClientName) ? "Cliente" : cargo.ClientName,
                OriginCityId      = cargo.OriginCityId,
                DestinationCityId = cargo.DestinationCityId,
                Amount            = amount,
                QuotedAmount      = revenue,
                DueDay            = currentDay + delay,
                CreatedDay        = currentDay,
                Timing            = timing
            };

            if (delay <= 0)
            {
                Pay(payment);   // cobro al contado
                return;
            }

            _pending.Add(payment);
            OnPaymentScheduled?.Invoke(payment);
        }

        // Cobra todos los pagos cuyo vencimiento ya llegó. Llamado por CargoManager cada día.
        public void ProcessDuePayments(int currentDay)
        {
            if (_pending.Count == 0) return;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].DueDay <= currentDay)
                {
                    var p = _pending[i];
                    _pending.RemoveAt(i);
                    Pay(p);
                }
            }
        }

// Gestiona pay.
        private void Pay(PendingPayment p)
        {
            EconomyManager.Instance?.AddMoney(p.Amount, $"Cobro de {p.ClientName}");
            OnPaymentReceived?.Invoke(p);
        }

// Borra all.
        public void ClearAll() => _pending.Clear();
    }
}