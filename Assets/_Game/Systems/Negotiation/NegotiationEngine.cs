using System;
using FreightForwarder.Models;
using FreightForwarder.Systems.World;
using UnityEngine;

namespace FreightForwarder.Systems.Negotiation
{

    // Motor de negociación V2. Reemplaza la lógica inline de ClientManager.EvaluateQuote.
    // Activar con FeatureFlags.USE_NEGOTIATION_V2 = true.
    // No modifica Client.cs ni ClientManager.cs.

    public static class NegotiationEngine
    {
        public static NegotiationOutcome Evaluate(Quote quote, Client client, Cargo cargo,
                                                   int playerLevel = 1, int playerReputation = 50)
        {
            float acceptance = ComputeAcceptance(quote, client, cargo, playerLevel, playerReputation);

            // Aceptación directa
            if (UnityEngine.Random.value < acceptance)
                return NegotiationOutcome.Accept(acceptance, "Trato cerrado.");

            // Contraoferta si aplica
            if (client.AcceptsNegotiation && quote.NegotiationRound <= 2)
            {
                float desiredMult = client.GetDesiredPriceMultiplier();
                int counterPrice  = Mathf.Max(quote.AgentCost + 100,
                                              (int)(quote.AgentCost * desiredMult * 1.1f));
                string msg = BuildCounterOfferMessage(client, counterPrice, quote.NegotiationRound);
                return NegotiationOutcome.Counter(counterPrice, msg, acceptance);
            }

            return NegotiationOutcome.Reject(BuildRejectionMessage(client, quote), acceptance);
        }

        // ── Cálculo de aceptación ────────────────────────────────────────────

        private static float ComputeAcceptance(Quote quote, Client client, Cargo cargo,
                                                int playerLevel, int playerReputation)
        {
            float base_ = Constants.NEGOTIATION_BASE_ACCEPTANCE;

            // Tipo de cliente
            switch (client.ClientType)
            {
                case Constants.ClientType.UrgentClient:   base_ += 0.22f; break;
                case Constants.ClientType.GoodPayer:      base_ += 0.12f; break;
                case Constants.ClientType.ContractClient: base_ += 0.08f; break;
                case Constants.ClientType.CreditClient:   base_ -= 0.06f; break;
                case Constants.ClientType.BadPayer:       base_ -= 0.16f; break;
                case Constants.ClientType.VeryBadClient:  base_ -= 0.28f; break;
            }

            // Margen
            base_ += client.GetNegotiationBonus();
            var (rejChance, _) = client.ReactToHighPrice(quote.Margin);
            base_ -= rejChance * 0.5f;

            // Transporte preferido
            if (cargo.PreferredTransport == quote.TransportMode) base_ += 0.10f;

            // Reputación del jugador (V2: no estaba en V1)
            base_ += (playerReputation - 50) * 0.002f;

            // Nivel del jugador — clientes valoran experiencia
            base_ += Mathf.Clamp(playerLevel - 1, 0, 9) * 0.01f;

            // Modificador mundial de demanda
            float demandMult = WorldStateManager.Instance?.DemandMultiplier ?? 1f;
            base_ *= demandMult;

            // VIP es más fiel
            if (client.IsVip) base_ += 0.15f;

            return Mathf.Clamp01(base_);
        }

        // Construye counter oferta message.

        private static string BuildCounterOfferMessage(Client client, int price, int round)
        {
            string[] phrases = {
                $"Le ofrezco ${price:N0}. Es lo máximo que podemos pagar.",
                $"Nuestra contraoferta es ${price:N0}. ¿Cerramos?",
                $"${price:N0} es nuestro límite final."
            };
            return phrases[Mathf.Min(round, phrases.Length - 1)];
        }

// Construye rejection message.
        private static string BuildRejectionMessage(Client client, Quote quote)
        {
            if (quote.HasExcessiveMargin())
                return "El margen es demasiado alto para nuestros estándares.";

            switch (client.ClientType)
            {
                case Constants.ClientType.VeryBadClient:
                    return "No nos conviene esta propuesta en este momento.";
                case Constants.ClientType.BadPayer:
                    return "Necesitamos más flexibilidad en los términos.";
                case Constants.ClientType.UrgentClient:
                    return "El tiempo de tránsito no cumple con nuestra urgencia.";
                default:
                    return "Sus condiciones no se ajustan a nuestras necesidades actuales.";
            }
        }
    }

    // ── Resultado ────────────────────────────────────────────────────────────

    public class NegotiationOutcome
    {
// Gestiona kind.
        public enum Kind { Accepted, CounterOffer, Rejected }

// Gestiona resultado.
        public Kind   Result         { get; }
// Gestiona acceptance prob.
        public float  AcceptanceProb { get; }
// Mensaje.
        public string Message        { get; }
// Gestiona counter precio.
        public int    CounterPrice   { get; }

// Realiza negotiation outcome
        private NegotiationOutcome(Kind kind, float prob, string msg, int counter = 0)
        {
            Result         = kind;
            AcceptanceProb = prob;
            Message        = msg;
            CounterPrice   = counter;
        }

// Aceptado.
        public static NegotiationOutcome Accept(float prob, string msg)
            => new NegotiationOutcome(Kind.Accepted, prob, msg);

// Gestiona counter.
        public static NegotiationOutcome Counter(int price, string msg, float prob)
            => new NegotiationOutcome(Kind.CounterOffer, prob, msg, price);

// Gestiona reject.
        public static NegotiationOutcome Reject(string msg, float prob)
            => new NegotiationOutcome(Kind.Rejected, prob, msg);
    }
}