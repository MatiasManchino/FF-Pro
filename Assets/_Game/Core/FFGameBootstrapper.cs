using FreightForwarder.Managers;
using UnityEngine;

namespace FreightForwarder.Core
{
    /// <summary>
    /// Overlay de debug para el sistema Freight Forwarder.
    /// La inicialización real ocurre en GameBootstrapper (el del mapa).
    /// Agregá este componente solo si querés ver el HUD de debug en editor.
    /// </summary>
    public class FFGameBootstrapper : MonoBehaviour
    {
        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(10, 200, 260, 180));
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== Freight Forwarder ===");

            if (EconomyManager.Instance != null)
            {
                GUILayout.Label($"Dinero:      ${EconomyManager.Instance.Money:N0}");
                GUILayout.Label($"Reputación:  {EconomyManager.Instance.Reputation}/100");
                GUILayout.Label($"Nivel:       {EconomyManager.Instance.Level}");
            }

            if (FFTimeManager.Instance != null)
                GUILayout.Label($"Día de juego: {FFTimeManager.Instance.CurrentDay}");

            if (CargoManager.Instance != null)
            {
                GUILayout.Label($"Mercado:     {CargoManager.Instance.MarketCargos.Count} cargas");
                GUILayout.Label($"En tránsito: {CargoManager.Instance.ActiveCargos.Count} cargas");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
