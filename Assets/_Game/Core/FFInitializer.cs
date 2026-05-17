using FreightForwarder.Managers;
using FreightForwarder.Map;
using FreightForwarder.Models;
using FreightForwarder.Weather;
using UnityEngine;

namespace FreightForwarder.Core
{
    /// <summary>
    /// Inicializa todos los sistemas de Freight Forwarder.
    /// Agregá este componente a un GameObject vacío en la escena.
    /// El mapa 3D funciona igual aunque este componente no esté presente.
    /// </summary>
    public class FFInitializer : MonoBehaviour
    {
        private void Awake()
        {
            CityDatabase.Initialize();

            var _ = GameManager.Instance;
            var __ = FFTimeManager.Instance;
            var ___ = EconomyManager.Instance;
            var ____ = AgentManager.Instance;
            var _____ = ClientManager.Instance;
            var ______ = CargoManager.Instance;
            var _______ = EventManager.Instance;
            var ________ = RouteManager.Instance;
            var _________ = WeatherSystem.Instance;
            var __________ = WeatherManager.Instance;
            var ___________ = CloudRenderer.Instance;
            var ____________ = WeatherImpact.Instance;
            var _____________ = HurricaneController.Instance;

            GameManager.Instance.StartNewGame();
            Debug.Log("[FF] Sistemas inicializados correctamente.");
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(10, 200, 240, 160));
            GUILayout.BeginVertical("box");
            GUILayout.Label("── Freight Forwarder ──");

            if (EconomyManager.Instance != null)
            {
                GUILayout.Label($"Dinero:      ${EconomyManager.Instance.Money:N0}");
                GUILayout.Label($"Reputación:  {EconomyManager.Instance.Reputation}/100");
                GUILayout.Label($"Nivel:       {EconomyManager.Instance.Level}");
            }
            if (FFTimeManager.Instance != null)
                GUILayout.Label($"Día:         {FFTimeManager.Instance.CurrentDay}");
            if (CargoManager.Instance != null)
            {
                GUILayout.Label($"Mercado:     {CargoManager.Instance.MarketCargos.Count} cargas");
                GUILayout.Label($"Tránsito:    {CargoManager.Instance.ActiveCargos.Count} cargas");
            }
            if (WeatherSystem.Instance?.Grid != null)
            {
                int storms = 0, cyclones = 0;
                foreach (var c in WeatherSystem.Instance.Grid.AllCells)
                {
                    if (c.isStorming) storms++;
                    if (c.isCyclone) cyclones++;
                }
                GUILayout.Label($"Tormentas:   {storms}  Ciclones: {cyclones}");
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
