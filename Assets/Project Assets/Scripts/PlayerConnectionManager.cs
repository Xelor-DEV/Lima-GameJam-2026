using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerConnectionManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayersSessionData playersSessionData;

    [Tooltip("El objeto que agrupará a todos los jugadores en la jerarquía.")]
    [SerializeField] private Transform playerContainer;

    private void Awake()
    {
        // Limpiar datos anteriores al cargar la escena
        if (playersSessionData != null)
        {
            playersSessionData.ResetSession();
        }

        // Validación amigable por si olvidas asignar el contenedor
        if (playerContainer == null)
        {
            Debug.LogWarning("No has asignado un 'Player Container'. Los jugadores se instanciarán en la raíz de la escena.");
        }
    }

    // ARRASTRA ESTA FUNCIÓN al evento "Player Joined Event" en el Inspector del PlayerInputManager
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (playersSessionData == null)
        {
            Debug.LogError("Falta asignar el ScriptableObject 'PlayersSessionData' en el Manager.");
            return;
        }

        // Registrar datos en el ScriptableObject
        playersSessionData.AddPlayer(playerInput);

        // --- Lógica de Organización en la Jerarquía ---

        // 1. Cambiar el nombre para identificarlo fácilmente
        playerInput.gameObject.name = $"Player_Input_{playerInput.playerIndex}";

        // 2. Hacerlo hijo del contenedor si existe
        if (playerContainer != null)
        {
            // El segundo parámetro 'false' hace que el objeto ignore su posición/escala previa
            // en el mundo y adopte las coordenadas locales relativas al padre.
            playerInput.transform.SetParent(playerContainer, false);

        }
    }

    // ARRASTRA ESTA FUNCIÓN al evento "Player Left Event"
    public void OnPlayerLeft(PlayerInput playerInput)
    {
        if (playersSessionData != null)
        {
            playersSessionData.RemovePlayer(playerInput);
        }
    }
}