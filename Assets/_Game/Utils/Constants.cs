using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreightForwarder.Utils
{
  public static class Constants
  {
      // Dinero y reputación inicial (usa en EconomyManager)
      public const int INITIAL_MONEY = 5000;
      public const int INITIAL_REPUTATION = 50;
      public const int GAME_OVER_DEBT_THRESHOLD = -10000;

      // Progresión
      public const int XP_PER_LEVEL = 1000;
      public const int XP_PER_CARGO = 150;

      // Enums
      public enum CargoType { General, Refrigerated, Hazardous, Urgent, Valuable }
      public enum ClientType { GoodPayer, BadPayer, Risky, VIP }
      public enum CargoStatus { Available, Quoting, Active, Completed, Failed, Expired }
      public enum TransportMode { Maritime, Air, Land, Rail, Multimodal }

      // Otros valores globales
      public const float BASE_SHIPPING_COST_PER_KM = 0.5f;
      public const int DEFAULT_EXPIRATION_DAYS = 7;
  }
}
