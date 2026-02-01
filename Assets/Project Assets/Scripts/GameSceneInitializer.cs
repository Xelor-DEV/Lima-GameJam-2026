using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class GameSceneInitializer : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("Se dispara cuando un jugador ha sido instanciado y configurado. Conecta aquí al PatternMinigameManager.")]
    public UnityEvent<int, GameObject, PlayerUIInfo> onPlayerReady;

    [Header("Game Configuration")]
    [Tooltip("Índice del minijuego actual (0 para el primero, 1 para el segundo, etc.)")]
    public int currentMinigameIndex = 0;

    [Header("Data References")]
    public PlayersSessionData sessionData;

    [Header("UI References")]
    public PlayerUIInfo[] playerUIElements;

    [Header("Player Spawning")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Camera Settings")]
    [Tooltip("Referencia a la cámara para calcular los límites. Si está vacía usa Camera.main")]
    public Camera mainCamera;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Validación de seguridad para el índice
        if (sessionData != null && currentMinigameIndex >= sessionData.totalMinigames)
        {
            Debug.LogError($"El índice del minijuego ({currentMinigameIndex}) excede el total configurado en SessionData ({sessionData.totalMinigames}).");
            return;
        }

        InitializeGame();
    }

    private void InitializeGame()
    {
        if (sessionData == null || sessionData.players.Count == 0) return;

        // 1. Calcular dimensiones de la pantalla en Unidades de Mundo (World Units)
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;

        // El tamaño de cada "mitad" de pantalla
        Vector2 halfScreenSize = new Vector2(screenWidth / 2f, screenHeight);

        // Posición base de la cámara (por si la cámara no está en 0,0)
        Vector2 camPos = mainCamera.transform.position;

        for (int i = 0; i < sessionData.players.Count; i++)
        {
            if (i >= playerUIElements.Length) break;

            Player playerData = sessionData.players[i];

            // Obtenemos la clase score específica para este minijuego
            MinigameScore currentScoreObj = playerData.scores[currentMinigameIndex];

            // 1. Configurar UI
            if (playerData.selectedCharacter != null)
            {
                playerUIElements[i].SetupUI(
                    playerData.selectedCharacter.characterName,
                    playerData.selectedCharacter.uiIcon,
                    playerData.color,
                    playerData.index + 1,
                    currentScoreObj // Pasamos la referencia de la clase score
                );
            }

            // 2. Instanciar Player
            Transform spawnPoint = (spawnPoints.Length > i) ? spawnPoints[i] : transform;
            GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            MovementBounds2D bounds = playerObj.GetComponent<MovementBounds2D>();
            if (bounds != null)
            {
                // Lógica para dividir la pantalla:
                // Si i == 0 (Player 1) -> Mitad Izquierda
                // Si i == 1 (Player 2) -> Mitad Derecha

                Vector2 areaCenter;

                if (i == 0) // Jugador 1 (Izquierda)
                {
                    // El centro es la posición de la cámara MENOS un cuarto del ancho total
                    areaCenter = new Vector2(camPos.x - (screenWidth / 4f), camPos.y);
                }
                else // Jugador 2 (Derecha) u otros
                {
                    // El centro es la posición de la cámara MÁS un cuarto del ancho total
                    areaCenter = new Vector2(camPos.x + (screenWidth / 4f), camPos.y);
                }

                // Inyectamos la configuración
                bounds.SetBounds(areaCenter, halfScreenSize);
            }
            else
            {
                Debug.LogWarning("El prefab del jugador no tiene el componente MovementBounds2D.");
            }

            // 3. Asignar Controles (Lógica Modificada)
            PlayerInput pInput = playerObj.GetComponent<PlayerInput>();
            if (pInput != null)
            {
                pInput.user.UnpairDevices();

                // --- NUEVA LÓGICA DE SELECCIÓN DE DISPOSITIVOS ---
                List<InputDevice> devicesToPair = new List<InputDevice>();

                if (sessionData.useDeviceIdsForPairing)
                {
                    // Opción A: Recuperar usando IDs (Más robusto entre escenas)
                    foreach (int devId in playerData.devIds)
                    {
                        // InputSystem.GetDeviceById devuelve el dispositivo activo actual con ese ID
                        InputDevice deviceFound = InputSystem.GetDeviceById(devId);

                        if (deviceFound != null)
                        {
                            devicesToPair.Add(deviceFound);
                        }
                        else
                        {
                            Debug.LogWarning($"Jugador {i}: No se encontró dispositivo con ID {devId}. Puede estar desconectado.");
                        }
                    }
                }
                else
                {
                    // Opción B: Usar referencias directas (Puede fallar si son stale objects)
                    devicesToPair = playerData.devices;
                }

                // Emparejar los dispositivos encontrados/seleccionados
                foreach (var device in devicesToPair)
                {
                    InputUser.PerformPairingWithDevice(device, pInput.user);
                }
                // -------------------------------------------------
            }
            onPlayerReady.Invoke(i, playerObj, playerUIElements[i]);
        }
    }
}