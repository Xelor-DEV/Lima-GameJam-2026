using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class GameSceneInitializer : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent<int, GameObject, PlayerUIInfo> onPlayerReady;

    [Header("Game Configuration")]
    public int currentMinigameIndex = 0;

    [Header("Data References")]
    public PlayersSessionData sessionData;

    [Header("UI References")]
    public PlayerUIInfo[] playerUIElements;

    [Header("Player Spawning")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Camera Settings")]
    public Camera mainCamera;

    [Header("Input Settings")]
    // IMPORTANTE: Estos nombres deben coincidir EXACTAMENTE con los de tu Input Action Asset
    [Tooltip("Nombre del esquema para mando en tu Input Actions (ej. 'Gamepad')")]
    public string gamepadSchemeName = "Gamepad";
    [Tooltip("Nombre del esquema para teclado en tu Input Actions (ej. 'Keyboard&Mouse')")]
    public string keyboardSchemeName = "KeyboardAndMouse";

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (sessionData != null && currentMinigameIndex >= sessionData.totalMinigames)
        {
            Debug.LogError($"El índice del minijuego excede el total configurado.");
            return;
        }

        InitializeGame();
    }

    private void InitializeGame()
    {
        if (sessionData == null || sessionData.players.Count == 0) return;

        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;
        Vector2 halfScreenSize = new Vector2(screenWidth / 2f, screenHeight);
        Vector2 camPos = mainCamera.transform.position;

        for (int i = 0; i < sessionData.players.Count; i++)
        {
            if (i >= playerUIElements.Length) break;

            Player playerData = sessionData.players[i];
            MinigameScore currentScoreObj = playerData.scores[currentMinigameIndex];

            // 1. Configurar UI
            if (playerData.selectedCharacter != null)
            {
                playerUIElements[i].SetupUI(
                        playerData.selectedCharacter.characterName,
                        playerData.selectedCharacter.uiIcon,
                        playerData.color,
                        playerData.index + 1,
                        currentScoreObj,
                        playerData.selectedCharacter // <--- NUEVO: Pasamos la data completa
                    );
            }

            // 2. Instanciar Player
            Transform spawnPoint = (spawnPoints.Length > i) ? spawnPoints[i] : transform;
            GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // Configurar límites (Igual que antes)
            MovementBounds2D bounds = playerObj.GetComponent<MovementBounds2D>();
            if (bounds != null)
            {
                Vector2 areaCenter;
                if (i == 0) areaCenter = new Vector2(camPos.x - (screenWidth / 4f), camPos.y);
                else areaCenter = new Vector2(camPos.x + (screenWidth / 4f), camPos.y);
                bounds.SetBounds(areaCenter, halfScreenSize);
            }

            // 3. Asignar Controles (CORREGIDO)
            PlayerInput pInput = playerObj.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                // --- PASO A: Desactivar input para evitar conflictos durante la configuración ---
                pInput.DeactivateInput();

                // --- PASO B: Recolectar dispositivos ---
                List<InputDevice> devicesToPair = new List<InputDevice>();

                if (sessionData.useDeviceIdsForPairing)
                {
                    foreach (int devId in playerData.devIds)
                    {
                        InputDevice deviceFound = InputSystem.GetDeviceById(devId);
                        if (deviceFound != null) devicesToPair.Add(deviceFound);
                    }
                }
                else
                {
                    devicesToPair = playerData.devices;
                }

                // --- PASO C: Determinar el Esquema (Scheme) correcto ---
                // Si no especificamos el esquema, PlayerInput intentará adivinar y a menudo falla con el Player 1
                string schemeToUse = keyboardSchemeName; // Default fallback

                if (devicesToPair.Count > 0)
                {
                    InputDevice primaryDevice = devicesToPair[0];

                    if (primaryDevice is Gamepad)
                    {
                        schemeToUse = gamepadSchemeName;
                    }
                    else if (primaryDevice is Keyboard || primaryDevice is Mouse)
                    {
                        schemeToUse = keyboardSchemeName;
                    }
                }

                // --- PASO D: Asignación Atómica ---
                // SwitchCurrentControlScheme hace: Unpair -> Pair New Devices -> Set Scheme -> Habilitar
                if (devicesToPair.Count > 0)
                {
                    pInput.SwitchCurrentControlScheme(schemeToUse, devicesToPair.ToArray());
                }

                // --- PASO E: Reactivar ---
                pInput.ActivateInput();
            }

            onPlayerReady.Invoke(i, playerObj, playerUIElements[i]);
        }
    }
}