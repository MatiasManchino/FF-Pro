using System;
using System.Collections.Generic;

namespace FreightForwarder.Models
{
    /// <summary>
    /// Client.cs — Modelo de cliente con PERSONALIDAD ACTIVA.
    /// 
    /// Los clientes no son pasivos. Tienen:
    /// - Personalidad única que afecta negociación y pagos
    /// - Memoria de cómo el jugador los trata
    /// - Reacciones activas (quejas, bloqueos, recomendaciones)
    /// - Relación que evoluciona con el tiempo
    /// </summary>
    [Serializable]
    public class Client
    {
        // =========================================================================
        // IDENTIFICACIÓN BÁSICA
        // =========================================================================
        
        /// <summary>
        /// ID único del cliente (ej: "cliente_001")
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Nombre de la empresa del cliente
        /// </summary>
        public string CompanyName { get; set; }
        
        // =========================================================================
        // PERSONALIDAD (determina comportamiento)
        // =========================================================================
        
        /// <summary>
        /// Tipo de cliente (GoodPayer, BadPayer, UrgentClient, etc.)
        /// </summary>
        public Constants.ClientType ClientType { get; set; }
        
        /// <summary>
        /// Descripción legible de la personalidad (para UI)
        /// </summary>
        public string PersonalityDescription { get; set; }
        
        // =========================================================================
        // RELACIÓN CON EL JUGADOR
        // =========================================================================
        
        /// <summary>
        /// Nivel de relación con el jugador (0-100)
        /// Mejora con entregas exitosas, empeora con fallos
        /// </summary>
        public float RelationshipLevel { get; set; }
        
        /// <summary>
        /// Nivel de enojo (0-5). Si llega a 5, el cliente bloquea al jugador
        /// </summary>
        public int AngerLevel { get; set; }
        
        /// <summary>
        /// ¿El cliente está bloqueado? (no acepta más cotizaciones)
        /// </summary>
        public bool IsBlacklisted { get; set; }
        
        /// <summary>
        /// Días hasta que el enojo disminuye (si no hay incidentes)
        /// </summary>
        public int DaysUntilAngerDecay { get; set; }
        
        // =========================================================================
        // COMPORTAMIENTO DE PAGO
        // =========================================================================
        
        /// <summary>
        /// Días que tarda en pagar (0 = al contado, >0 = crédito)
        /// </summary>
        public int PaymentDelay { get; set; }
        
        /// <summary>
        /// Probabilidad de pagar antes (0-1). Clientes VIP pagan más rápido
        /// </summary>
        public float EarlyPaymentChance { get; set; }
        
        /// <summary>
        /// Probabilidad de pagar después (0-1). Malos pagadores
        /// </summary>
        public float LatePaymentChance { get; set; }
        
        /// <summary>
        /// Penalidad por pago tardío (multiplicador)
        /// </summary>
        public float LatePaymentPenalty { get; set; }
        
        // =========================================================================
        // TOLERANCIA A PROBLEMAS
        // =========================================================================
        
        /// <summary>
        /// Tolerancia a retrasos (días). Si se excede, el cliente se enoja
        /// </summary>
        public int DelayTolerance { get; set; }
        
        /// <summary>
        /// Tolerancia a daños en carga (% del valor). Si se excede, el cliente se enoja
        /// </summary>
        public float DamageTolerance { get; set; }
        
        /// <summary>
        /// ¿Acepta negociar precios?
        /// </summary>
        public bool AcceptsNegotiation { get; set; }
        
        /// <summary>
        /// Margen máximo que tolera (porcentaje sobre valor de mercado)
        /// </summary>
        public float MaxMarginTolerance { get; set; }
        
        // =========================================================================
        // HISTORIAL DE INTERACCIONES
        // =========================================================================
        
        /// <summary>
        /// Número total de entregas realizadas
        /// </summary>
        public int TotalDeliveries { get; set; }
        
        /// <summary>
        /// Entregas exitosas
        /// </summary>
        public int SuccessfulDeliveries { get; set; }
        
        /// <summary>
        /// Entregas fallidas
        /// </summary>
        public int FailedDeliveries { get; set; }
        
        /// <summary>
        /// Número de quejas registradas
        /// </summary>
        public int ComplaintsCount { get; set; }
        
        /// <summary>
        /// Número de recomendaciones hechas a otros clientes
        /// </summary>
        public int RecommendationsGiven { get; set; }
        
        /// <summary>
        /// Rutas favoritas (para estadísticas)
        /// </summary>
        public List<string> FavoriteRoutes { get; set; }
        
        /// <summary>
        /// Día en que se conoció al cliente
        /// </summary>
        public int FirstSeenDay { get; set; }
        
        /// <summary>
        /// Día de la última interacción
        /// </summary>
        public int LastInteractionDay { get; set; }
        
        // =========================================================================
        // ESTADO ACTIVO DEL CLIENTE
        // =========================================================================
        
        /// <summary>
        /// ¿El cliente está activo (puede generar cargas)?
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// ¿El cliente es VIP? (beneficios especiales)
        /// </summary>
        public bool IsVip { get; set; }
        
        /// <summary>
        /// Ofertas pendientes (sin responder)
        /// </summary>
        public int PendingOffers { get; set; }
        
        /// <summary>
        /// Contrato activo (si aplica)
        /// </summary>
        public bool HasActiveContract { get; set; }
        
        /// <summary>
        /// Días restantes del contrato
        /// </summary>
        public int ContractDaysRemaining { get; set; }
        
        // =========================================================================
        // CONSTRUCTORES
        // =========================================================================
        
        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public Client()
        {
            Id = Guid.NewGuid().ToString();
            CompanyName = string.Empty;
            PersonalityDescription = string.Empty;
            FavoriteRoutes = new List<string>();
            RelationshipLevel = 50f;
            AngerLevel = 0;
            IsBlacklisted = false;
            DaysUntilAngerDecay = 0;
            TotalDeliveries = 0;
            SuccessfulDeliveries = 0;
            FailedDeliveries = 0;
            ComplaintsCount = 0;
            RecommendationsGiven = 0;
            IsActive = true;
            IsVip = false;
            PendingOffers = 0;
            HasActiveContract = false;
            ContractDaysRemaining = 0;
            
            // Valores por defecto
            PaymentDelay = 0;
            EarlyPaymentChance = 0.1f;
            LatePaymentChance = 0.05f;
            LatePaymentPenalty = 1.0f;
            DelayTolerance = 3;
            DamageTolerance = 0.1f;
            AcceptsNegotiation = true;
            MaxMarginTolerance = 0.35f;
        }
        
        /// <summary>
        /// Constructor para crear clientes predefinidos.
        /// </summary>
        public Client(string companyName, Constants.ClientType clientType, 
                      int paymentDelay = 0, float earlyPaymentChance = 0.1f,
                      float latePaymentChance = 0.05f, int delayTolerance = 3,
                      float damageTolerance = 0.1f, bool acceptsNegotiation = true,
                      float maxMarginTolerance = 0.35f)
        {
            Id = Guid.NewGuid().ToString();
            CompanyName = companyName;
            ClientType = clientType;
            PersonalityDescription = GetPersonalityDescription(clientType);
            FavoriteRoutes = new List<string>();
            RelationshipLevel = 50f;
            AngerLevel = 0;
            IsBlacklisted = false;
            DaysUntilAngerDecay = 0;
            TotalDeliveries = 0;
            SuccessfulDeliveries = 0;
            FailedDeliveries = 0;
            ComplaintsCount = 0;
            RecommendationsGiven = 0;
            IsActive = true;
            IsVip = false;
            PendingOffers = 0;
            HasActiveContract = false;
            ContractDaysRemaining = 0;
            
            // Configurar según tipo de cliente
            PaymentDelay = paymentDelay;
            EarlyPaymentChance = earlyPaymentChance;
            LatePaymentChance = latePaymentChance;
            LatePaymentPenalty = 1.0f;
            DelayTolerance = delayTolerance;
            DamageTolerance = damageTolerance;
            AcceptsNegotiation = acceptsNegotiation;
            MaxMarginTolerance = maxMarginTolerance;
            
            // Ajustes específicos por tipo de cliente
            switch (clientType)
            {
                case Constants.ClientType.GoodPayer:
                    EarlyPaymentChance = 0.3f;
                    LatePaymentChance = 0.01f;
                    DelayTolerance = 5;
                    MaxMarginTolerance = 0.25f;
                    break;
                    
                case Constants.ClientType.BadPayer:
                    PaymentDelay = 10;
                    LatePaymentChance = 0.4f;
                    LatePaymentPenalty = 1.2f;
                    DelayTolerance = 2;
                    MaxMarginTolerance = 0.15f;
                    break;
                    
                case Constants.ClientType.UrgentClient:
                    PaymentDelay = -3; // Paga antes
                    EarlyPaymentChance = 0.5f;
                    DelayTolerance = 1;
                    MaxMarginTolerance = 0.5f; // Acepta márgenes más altos
                    AcceptsNegotiation = false; // No negocia, paga lo que sea
                    break;
                    
                case Constants.ClientType.CreditClient:
                    PaymentDelay = 30;
                    EarlyPaymentChance = 0.05f;
                    DelayTolerance = 10;
                    MaxMarginTolerance = 0.20f;
                    break;
                    
                case Constants.ClientType.VeryBadClient:
                    PaymentDelay = 15;
                    LatePaymentChance = 0.6f;
                    LatePaymentPenalty = 1.3f;
                    DelayTolerance = 1;
                    DamageTolerance = 0.05f;
                    MaxMarginTolerance = 0.10f;
                    break;
                    
                case Constants.ClientType.ContractClient:
                    PaymentDelay = 15;
                    EarlyPaymentChance = 0.1f;
                    DelayTolerance = 4;
                    MaxMarginTolerance = 0.15f;
                    HasActiveContract = true;
                    ContractDaysRemaining = 180;
                    break;
            }
        }
        
        // =========================================================================
        // COMPORTAMIENTOS ACTIVOS (reacciones del cliente)
        // =========================================================================
        
        /// <summary>
        /// El cliente reacciona a un retraso.
        /// Retorna la pérdida de reputación y si el cliente se enoja.
        /// </summary>
        public (float reputationLoss, bool becomesAngry) ReactToDelay(int delayDays)
        {
            float reputationLoss = 0f;
            bool becomesAngry = false;
            
            if (delayDays <= DelayTolerance)
                return (0, false);
            
            int excessDelay = delayDays - DelayTolerance;
            
            // Pérdida base de reputación
            reputationLoss = excessDelay * 2f;
            
            // Clientes urgentes se enojan más
            if (ClientType == Constants.ClientType.UrgentClient)
            {
                reputationLoss *= 2f;
                becomesAngry = true;
            }
            
            // Clientes con contrato tienen menos tolerancia
            if (HasActiveContract)
            {
                reputationLoss *= 1.5f;
            }
            
            // Incrementar enojo
            if (excessDelay >= 3)
                becomesAngry = true;
            
            return (reputationLoss, becomesAngry);
        }
        
        /// <summary>
        /// El cliente reacciona a daños en la carga.
        /// </summary>
        public (float reputationLoss, float compensationClaim) ReactToDamage(float damagePercentage)
        {
            float reputationLoss = 0f;
            float compensationClaim = 0f;
            
            if (damagePercentage <= DamageTolerance)
                return (0, 0);
            
            float excessDamage = damagePercentage - DamageTolerance;
            
            // Pérdida de reputación
            reputationLoss = excessDamage * 10f;
            
            // Reclamo de compensación
            compensationClaim = excessDamage * 100f; // Porcentaje convertido a dólares base
            
            // Clientes difíciles reclaman más
            if (ClientType == Constants.ClientType.VeryBadClient)
            {
                compensationClaim *= 1.5f;
                reputationLoss *= 1.5f;
            }
            
            return (reputationLoss, compensationClaim);
        }
        
        /// <summary>
        /// El cliente reacciona a un precio demasiado alto.
        /// Retorna la probabilidad de rechazo y si se enoja.
        /// </summary>
        public (float rejectionChance, bool becomesAngry) ReactToHighPrice(float marginPercentage)
        {
            float rejectionChance = 0f;
            bool becomesAngry = false;
            
            if (marginPercentage <= MaxMarginTolerance)
                return (0, false);
            
            float excessMargin = marginPercentage - MaxMarginTolerance;
            
            // Probabilidad de rechazo base
            rejectionChance = excessMargin * 2f;
            rejectionChance = Math.Min(rejectionChance, 0.9f);
            
            // Clientes con contrato son más sensibles
            if (HasActiveContract)
            {
                rejectionChance *= 1.5f;
                becomesAngry = true;
            }
            
            // Si el margen es muy alto (>50%), se enoja
            if (marginPercentage > 0.50f)
                becomesAngry = true;
            
            return (rejectionChance, becomesAngry);
        }
        
        /// <summary>
        /// El cliente decide si recomendar al jugador a otros.
        /// </summary>
        public bool DecideToRecommend()
        {
            // Solo si relación es buena (RelationshipLevel > 70)
            if (RelationshipLevel < 70)
                return false;
            
            // Probabilidad base: 5% por entrega exitosa
            float recommendChance = 0.05f;
            
            // Aumenta con entregas exitosas consecutivas
            if (SuccessfulDeliveries >= 5)
                recommendChance += 0.05f;
            if (SuccessfulDeliveries >= 10)
                recommendChance += 0.05f;
            
            // VIP recomienda más
            if (IsVip)
                recommendChance += 0.10f;
            
            return UnityEngine.Random.value < recommendChance;
        }
        
        /// <summary>
        /// El cliente decide si volverse VIP (programa de fidelidad).
        /// </summary>
        public bool DecideToBecomeVip()
        {
            // Requisitos: muchas entregas exitosas y buena relación
            if (SuccessfulDeliveries < 10)
                return false;
            
            if (RelationshipLevel < 80)
                return false;
            
            if (IsVip)
                return false;
            
            // 10% de chance después de cumplir requisitos
            return UnityEngine.Random.value < 0.10f;
        }
        
        /// <summary>
        /// El cliente decide si renovar el contrato (si aplica).
        /// </summary>
        public bool DecideToRenewContract()
        {
            if (!HasActiveContract)
                return false;
            
            if (ContractDaysRemaining > 0)
                return false;
            
            // Renueva si relación es buena y pocos fallos
            if (RelationshipLevel >= 60 && FailedDeliveries == 0)
                return true;
            
            if (RelationshipLevel >= 70 && FailedDeliveries <= 1)
                return true;
            
            return false;
        }
        
        // =========================================================================
        // MÉTODOS DE ACTUALIZACIÓN
        // =========================================================================
        
        /// <summary>
        /// Registra una entrega completada.
        /// </summary>
        public void RecordDelivery(bool wasSuccessful, string originCityId, string destinationCityId, int currentDay, bool wasDelayed = false, bool wasDamaged = false)
        {
            TotalDeliveries++;
            LastInteractionDay = currentDay;
            
            if (wasSuccessful)
            {
                SuccessfulDeliveries++;
                
                // Mejora relación (+2 a +8 según éxito)
                float relationshipGain = 2f;
                if (!wasDelayed && !wasDamaged)
                    relationshipGain = 8f;
                else if (!wasDelayed)
                    relationshipGain = 5f;
                
                RelationshipLevel = Math.Min(100, RelationshipLevel + relationshipGain);
                
                // Registrar ruta favorita
                string routeKey = $"{originCityId}→{destinationCityId}";
                if (!FavoriteRoutes.Contains(routeKey))
                    FavoriteRoutes.Add(routeKey);
                if (FavoriteRoutes.Count > 5)
                    FavoriteRoutes.RemoveAt(0);
            }
            else
            {
                FailedDeliveries++;
                
                // Empeora relación (-5 a -15 según gravedad)
                float relationshipLoss = 5f;
                if (wasDelayed && wasDamaged)
                    relationshipLoss = 15f;
                else if (wasDelayed || wasDamaged)
                    relationshipLoss = 10f;
                
                RelationshipLevel = Math.Max(0, RelationshipLevel - relationshipLoss);
                
                // Aumenta enojo
                IncreaseAnger(2);
            }
            
            // Disminuir enojo con el tiempo (se maneja desde afuera)
            DaysUntilAngerDecay = 5; // Se calmará después de 5 días sin incidentes
            PendingOffers = 0;
        }
        
        /// <summary>
        /// Registra una queja del cliente.
        /// </summary>
        public void RecordComplaint()
        {
            ComplaintsCount++;
            RelationshipLevel = Math.Max(0, RelationshipLevel - 10);
            IncreaseAnger(3);
        }
        
        /// <summary>
        /// Registra una recomendación del cliente.
        /// </summary>
        public void RecordRecommendation()
        {
            RecommendationsGiven++;
            RelationshipLevel = Math.Min(100, RelationshipLevel + 5);
        }
        
        /// <summary>
        /// Aumenta el nivel de enojo del cliente.
        /// </summary>
        private void IncreaseAnger(int amount)
        {
            AngerLevel = Math.Min(5, AngerLevel + amount);
            
            if (AngerLevel >= 5)
            {
                IsBlacklisted = true;
                IsActive = false;
            }
        }
        
        /// <summary>
        /// Disminuye el enojo con el tiempo.
        /// </summary>
        public void DecayAnger()
        {
            if (DaysUntilAngerDecay > 0)
            {
                DaysUntilAngerDecay--;
                if (DaysUntilAngerDecay <= 0 && AngerLevel > 0)
                {
                    AngerLevel--;
                    if (AngerLevel < 5 && IsBlacklisted)
                    {
                        IsBlacklisted = false;
                        IsActive = true;
                    }
                    DaysUntilAngerDecay = 5; // Reiniciar para el siguiente nivel
                }
            }
        }
        
        /// <summary>
        /// Actualiza el estado del contrato.
        /// </summary>
        public void UpdateContract(int currentDay)
        {
            if (HasActiveContract && ContractDaysRemaining > 0)
            {
                ContractDaysRemaining--;
                if (ContractDaysRemaining <= 0)
                {
                    if (DecideToRenewContract())
                    {
                        ContractDaysRemaining = 180;
                        RelationshipLevel = Math.Min(100, RelationshipLevel + 10);
                    }
                    else
                    {
                        HasActiveContract = false;
                    }
                }
            }
        }
        
        // =========================================================================
        // MÉTODOS DE CÁLCULO
        // =========================================================================
        
        /// <summary>
        /// Calcula el bono de relación para negociación.
        /// </summary>
        public float GetNegotiationBonus()
        {
            if (RelationshipLevel >= 80)
                return 0.20f;  // +20% chance de aceptación
            if (RelationshipLevel >= 60)
                return 0.10f;
            if (RelationshipLevel >= 40)
                return 0.00f;
            if (RelationshipLevel >= 20)
                return -0.10f;
            return -0.20f;
        }
        
        /// <summary>
        /// Calcula el multiplicador de precio deseado por el cliente.
        /// </summary>
        public float GetDesiredPriceMultiplier()
        {
            float multiplier = 1.0f;
            
            // Clientes VIP aceptan precios más altos
            if (IsVip)
                multiplier = 1.15f;
            
            // Clientes urgentes pagan más
            if (ClientType == Constants.ClientType.UrgentClient)
                multiplier = 1.25f;
            
            // Clientes con contrato pagan menos
            if (HasActiveContract)
                multiplier = 0.90f;
            
            // Buena relación permite precios más altos
            if (RelationshipLevel >= 70)
                multiplier += 0.05f;
            
            return multiplier;
        }
        
        /// <summary>
        /// Calcula la probabilidad de pago temprano.
        /// </summary>
        public bool WillPayEarly()
        {
            if (PaymentDelay <= 0)
                return true;
            
            float chance = EarlyPaymentChance;
            
            // VIP paga más temprano
            if (IsVip)
                chance += 0.20f;
            
            // Buena relación mejora chance
            if (RelationshipLevel >= 70)
                chance += 0.10f;
            
            return UnityEngine.Random.value < chance;
        }
        
        /// <summary>
        /// Calcula la probabilidad de pago tardío.
        /// </summary>
        public bool WillPayLate()
        {
            if (LatePaymentChance <= 0)
                return false;
            
            float chance = LatePaymentChance;
            
            // Mala relación empeora chance
            if (RelationshipLevel <= 30)
                chance += 0.20f;
            
            // Cliente enojado paga tarde
            if (AngerLevel >= 3)
                chance += 0.30f;
            
            return UnityEngine.Random.value < chance;
        }
        
        // =========================================================================
        // MÉTODOS AUXILIARES
        // =========================================================================
        
        /// <summary>
        /// Obtiene el nivel de relación como texto con emoji.
        /// </summary>
        public string GetRelationshipEmoji()
        {
            if (RelationshipLevel >= 90)
                return "💎 Excelente";
            if (RelationshipLevel >= 70)
                return "😊 Muy Buena";
            if (RelationshipLevel >= 50)
                return "😐 Buena";
            if (RelationshipLevel >= 30)
                return "😠 Regular";
            if (RelationshipLevel >= 10)
                return "😤 Mala";
            return "👎 Pésima";
        }
        
        /// <summary>
        /// Obtiene el nivel de enojo como emoji.
        /// </summary>
        public string GetAngerEmoji()
        {
            switch (AngerLevel)
            {
                case 0: return "😊";
                case 1: return "😐";
                case 2: return "😠";
                case 3: return "😤";
                case 4: return "💢";
                case 5: return "🚫";
                default: return "😐";
            }
        }
        
        /// <summary>
        /// Obtiene la descripción de personalidad según el tipo.
        /// </summary>
        private string GetPersonalityDescription(Constants.ClientType clientType)
        {
            switch (clientType)
            {
                case Constants.ClientType.GoodPayer:
                    return "✅ Paga al contado, confiable. Muy tolerante.";
                case Constants.ClientType.BadPayer:
                    return "⚠️ Se retrasa en pagos. Baja tolerancia.";
                case Constants.ClientType.UrgentClient:
                    return "⚡ Necesita rapidez, paga más. Impaciente.";
                case Constants.ClientType.CreditClient:
                    return "🏦 Paga a 30-60 días. Exigente.";
                case Constants.ClientType.VeryBadClient:
                    return "🔥 Difícil, reclama siempre. ¡Cuidado!";
                case Constants.ClientType.ContractClient:
                    return "📄 Contrato a largo plazo. Busca estabilidad.";
                default:
                    return "Estándar.";
            }
        }
        
        /// <summary>
        /// Devuelve un resumen legible del cliente.
        /// </summary>
        public override string ToString()
        {
            string vipTag = IsVip ? " 👑 VIP" : "";
            string contractTag = HasActiveContract ? " 📄 Contrato" : "";
            string blacklistTag = IsBlacklisted ? " 🚫 BLOQUEADO" : "";
            return $"{CompanyName} | {GetRelationshipEmoji()}{vipTag}{contractTag}{blacklistTag} | Éxito: {GetSuccessRate():P0}";
        }
        
        /// <summary>
        /// Obtiene la tasa de éxito (0-1).
        /// </summary>
        public float GetSuccessRate()
        {
            if (TotalDeliveries == 0)
                return 0.5f;
            return (float)SuccessfulDeliveries / TotalDeliveries;
        }
    }
}