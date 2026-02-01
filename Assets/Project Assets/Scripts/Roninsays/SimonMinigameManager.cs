using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Localization; // Namespace necesario
using UnityEngine.Events;

public class SimonMinigameManager : MonoBehaviour
{
    [Header("Eventos")]
    public UnityEvent OnMinigameEnded; // <--- AÑADE ESTA LÍNEA

    [Header("Referencias Generales")]
    public SimonPatternGenerator patternGenerator;
    public GameObject iconUiPrefab;
    public float winnerBonus = 500f;

    [Header("Localization Keys")]
    // Asigna aquí las claves creadas en la tabla
    public LocalizedString locStart;      // "Simon_Inst_Start"
    public LocalizedString locCountdown;  // "Simon_Inst_Countdown" (Smart String)
    public LocalizedString locGo;         // "Simon_Inst_Go"
    public LocalizedString locWinner;     // "Simon_Game_Winner" (Smart String)
    public LocalizedString locErrTimeout; // "Simon_Err_Timeout"
    public LocalizedString locErrWrong;   // "Simon_Err_WrongBtn"

    [Header("UI por Jugador")]
    [Tooltip("Contenedores horizontales para los iconos de cada jugador")]
    public RectTransform[] playerSequenceContainers;

    [Tooltip("Textos de estado INDIVIDUALES para cada jugador (Instrucciones/Errores)")]
    public TextMeshProUGUI[] playerStatusTexts;

    private List<SimonInputDefinition> currentGlobalSequence;
    private Dictionary<int, SimonPlayerController> controllers = new Dictionary<int, SimonPlayerController>();
    private bool gameEnded = false;

    // --- MÉTODO LLAMADO POR GAMESCENEINITIALIZER ---
    public void RegisterPlayer(int playerIndex, GameObject playerObj, PlayerUIInfo uiInfo)
    {
        // 1. Obtener componente existente (NO AddComponent)
        SimonPlayerController controller = playerObj.GetComponent<SimonPlayerController>();

        if (controller == null)
        {
            Debug.LogError($"El Player {playerIndex} no tiene el SimonPlayerController en su prefab.");
            return;
        }

        controllers[playerIndex] = controller;

        // 2. Generar secuencia global si es el primero
        if (currentGlobalSequence == null)
        {
            currentGlobalSequence = patternGenerator.GenerateNewSequence();
            StartCoroutine(StartGameRoutine());
        }

        // 3. Detectar dispositivo (Keyboard vs Gamepad)
        bool isKeyboard = false;
        PlayerInput pInput = playerObj.GetComponent<PlayerInput>();
        if (pInput != null)
        {
            foreach (var device in pInput.devices)
            {
                if (device is Keyboard)
                {
                    isKeyboard = true;
                    break;
                }
            }
        }

        // 4. Instanciar Iconos UI
        List<Image> generatedIcons = new List<Image>();
        if (playerIndex < playerSequenceContainers.Length)
        {
            RectTransform container = playerSequenceContainers[playerIndex];
            // Limpiar
            foreach (Transform child in container) Destroy(child.gameObject);

            foreach (var step in currentGlobalSequence)
            {
                GameObject iconObj = Instantiate(iconUiPrefab, container);
                Image imgComp = iconObj.GetComponent<Image>();
                imgComp.sprite = isKeyboard ? step.keyboardIcon : step.gamepadIcon;
                generatedIcons.Add(imgComp);
            }
        }

        // 5. Asignar Texto de Estado específico para este jugador
        TextMeshProUGUI myText = null;
        if (playerIndex < playerStatusTexts.Length)
        {
            myText = playerStatusTexts[playerIndex];
            myText.text = ""; // Limpiar inicio
        }

        // 6. Inicializar Controlador (Pasamos los localized strings de error para que él los use)
        controller.Initialize(playerIndex, uiInfo, this, currentGlobalSequence, generatedIcons, myText, locErrTimeout, locErrWrong);
    }

    private IEnumerator StartGameRoutine()
    {
        // Fase 1: Anuncio Inicial
        UpdateAllPlayerTexts(locStart.GetLocalizedString());
        yield return new WaitForSeconds(2.0f);

        // Fase 2: Cuenta Regresiva
        float timer = 3.0f;
        while (timer > 0)
        {
            // Usamos Smart String {0} para el número
            locCountdown.Arguments = new object[] { timer };
            locCountdown.RefreshString(); // Forzar actualización

            UpdateAllPlayerTexts(locCountdown.GetLocalizedString());

            yield return new WaitForSeconds(1.0f);
            timer--;
        }

        // Fase 3: GO!
        UpdateAllPlayerTexts(locGo.GetLocalizedString());

        // Activar inputs de jugadores
        foreach (var ctrl in controllers.Values)
        {
            ctrl.EnableGame(true);
        }

        yield return new WaitForSeconds(1f);
        UpdateAllPlayerTexts(""); // Limpiar textos para dejar espacio a los errores
    }

    private void UpdateAllPlayerTexts(string message)
    {
        for (int i = 0; i < playerStatusTexts.Length; i++)
        {
            // Solo actualizamos textos de jugadores activos (que tengan controller registrado)
            if (controllers.ContainsKey(i) && playerStatusTexts[i] != null)
            {
                playerStatusTexts[i].text = message;
            }
        }
    }

    // --- LÓGICA DE VICTORIA ---
    public void PlayerFinished(int playerIndex)
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log($"¡JUEGO TERMINADO! Ganador: P{playerIndex + 1}");

        // --- BONUS AL GANADOR (Usando la propiedad pública) ---
        if (controllers.ContainsKey(playerIndex))
        {
            SimonPlayerController winnerCtrl = controllers[playerIndex];

            // Acceso limpio a través de la propiedad UiInfo
            if (winnerCtrl.UiInfo != null)
            {
                winnerCtrl.UiInfo.AddScore(winnerBonus);
            }
        }
        // -----------------------------------------------------

        // Desactivar a todos
        foreach (var ctrl in controllers.Values)
        {
            ctrl.EnableGame(false);
        }

        // Mostrar Ganador en todos los textos
        locWinner.Arguments = new object[] { playerIndex + 1 };
        locWinner.RefreshString();
        UpdateAllPlayerTexts(locWinner.GetLocalizedString());

        OnMinigameEnded?.Invoke();
    }

    public bool IsActionInPool(string actionName)
    {
        return patternGenerator.availableInputs.Any(x => string.Equals(x.actionName, actionName, System.StringComparison.OrdinalIgnoreCase));
    }
}