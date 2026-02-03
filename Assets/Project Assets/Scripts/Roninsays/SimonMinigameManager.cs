using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Events;
using DG.Tweening; // <--- NECESARIO PARA DOTWEEN

public class SimonMinigameManager : MonoBehaviour
{
    [Header("Eventos")]
    public UnityEvent OnMinigameEnded;

    [Header("Referencias Generales")]
    public SimonPatternGenerator patternGenerator;
    public GameObject iconUiPrefab;
    public float winnerBonus = 500f;

    [Header("Configuración Tiempos")]
    public float startMessageDuration = 5.0f; // <--- Solicitado: 5 segundos

    [Header("Localization Keys")]
    public LocalizedString locStart;
    public LocalizedString locCountdown;
    public LocalizedString locGo;
    public LocalizedString locWinner;
    public LocalizedString locErrTimeout;
    public LocalizedString locErrWrong;

    [Header("UI por Jugador")]
    public RectTransform[] playerSequenceContainers;
    public TextMeshProUGUI[] playerStatusTexts;

    private List<SimonInputDefinition> currentGlobalSequence;
    private Dictionary<int, SimonPlayerController> controllers = new Dictionary<int, SimonPlayerController>();
    private bool gameEnded = false;

    public bool IsGameFinished => gameEnded;

    // --- REGISTER PLAYER (Igual que antes) ---
    public void RegisterPlayer(int playerIndex, GameObject playerObj, PlayerUIInfo uiInfo)
    {
        SimonPlayerController controller = playerObj.GetComponent<SimonPlayerController>();

        if (controller == null)
        {
            Debug.LogError($"El Player {playerIndex} no tiene el SimonPlayerController.");
            return;
        }

        controllers[playerIndex] = controller;

        if (currentGlobalSequence == null)
        {
            currentGlobalSequence = patternGenerator.GenerateNewSequence();
            StartCoroutine(StartGameRoutine());
        }

        // Detectar Teclado vs Gamepad
        bool isKeyboard = false;
        PlayerInput pInput = playerObj.GetComponent<PlayerInput>();
        if (pInput != null)
        {
            foreach (var device in pInput.devices)
            {
                if (device is Keyboard) { isKeyboard = true; break; }
            }
        }

        // Instanciar UI
        List<Image> generatedIcons = new List<Image>();
        if (playerIndex < playerSequenceContainers.Length)
        {
            RectTransform container = playerSequenceContainers[playerIndex];
            foreach (Transform child in container) Destroy(child.gameObject);

            foreach (var step in currentGlobalSequence)
            {
                GameObject iconObj = Instantiate(iconUiPrefab, container);
                Image imgComp = iconObj.GetComponent<Image>();
                imgComp.sprite = isKeyboard ? step.keyboardIcon : step.gamepadIcon;
                generatedIcons.Add(imgComp);
            }
        }

        TextMeshProUGUI myText = null;
        if (playerIndex < playerStatusTexts.Length)
        {
            myText = playerStatusTexts[playerIndex];
            myText.text = "";
        }

        controller.Initialize(playerIndex, uiInfo, this, currentGlobalSequence, generatedIcons, myText, locErrTimeout, locErrWrong);
    }

    private IEnumerator StartGameRoutine()
    {
        // --- NUEVO: Esperar un frame para asegurar que todos los jugadores se hayan registrado ---
        yield return null;

        // Fase 1: Anuncio Inicial (Máquina de escribir)
        // Solicitado: Que dure 5 segundos en pantalla
        string startMsg = locStart.GetLocalizedString();
        UpdateAllPlayerTextsTypewriter(startMsg, 1.5f); // Escribe en 1.5s

        yield return new WaitForSeconds(startMessageDuration); // Espera los 5s solicitados

        // Fase 2: Cuenta Regresiva
        float timer = 3.0f;
        while (timer > 0)
        {
            locCountdown.Arguments = new object[] { timer };
            locCountdown.RefreshString();

            // Para la cuenta regresiva corta, quizás mejor texto directo sin efecto lento
            UpdateAllPlayerTexts(locCountdown.GetLocalizedString());

            yield return new WaitForSeconds(1.0f);
            timer--;
        }

        // Fase 3: GO!
        UpdateAllPlayerTexts(locGo.GetLocalizedString());

        // Efecto punch en el texto GO
        PunchAllTexts();

        // Activar inputs
        foreach (var ctrl in controllers.Values)
        {
            ctrl.EnableGame(true);
        }

        yield return new WaitForSeconds(1f);
        // NO borramos el texto aquí, porque ahora el Controller se encarga de mostrar
        // la cuenta regresiva del tiempo restante en ese mismo texto.
    }

    // --- Helper: Efecto Máquina de Escribir ---
    private void UpdateAllPlayerTextsTypewriter(string message, float duration)
    {
        for (int i = 0; i < playerStatusTexts.Length; i++)
        {
            if (controllers.ContainsKey(i) && playerStatusTexts[i] != null)
            {
                // Kill tweens previos para evitar conflictos
                playerStatusTexts[i].DOKill();
                playerStatusTexts[i].text = "";
                playerStatusTexts[i].DOText(message, duration).SetEase(Ease.Linear);
            }
        }
    }

    private void UpdateAllPlayerTexts(string message)
    {
        for (int i = 0; i < playerStatusTexts.Length; i++)
        {
            if (controllers.ContainsKey(i) && playerStatusTexts[i] != null)
            {
                playerStatusTexts[i].DOKill(); // Detener typewriter si estaba corriendo
                playerStatusTexts[i].text = message;
            }
        }
    }

    private void PunchAllTexts()
    {
        for (int i = 0; i < playerStatusTexts.Length; i++)
        {
            if (controllers.ContainsKey(i) && playerStatusTexts[i] != null)
            {
                playerStatusTexts[i].transform.DOPunchScale(Vector3.one * 0.5f, 0.3f);
            }
        }
    }

    public void PlayerFinished(int playerIndex)
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log($"¡JUEGO TERMINADO! Ganador: P{playerIndex + 1}");

        if (controllers.ContainsKey(playerIndex))
        {
            SimonPlayerController winnerCtrl = controllers[playerIndex];
            if (winnerCtrl.UiInfo != null)
            {
                winnerCtrl.UiInfo.AddScore(winnerBonus);
            }
        }

        foreach (var ctrl in controllers.Values)
        {
            // En vez de solo EnableGame(false), llamamos al Freeze total
            if (ctrl != null)
            {
                ctrl.FreezeController();
            }
        }

        locWinner.Arguments = new object[] { playerIndex + 1 };
        locWinner.RefreshString();

        // Mostrar ganador con typewriter
        UpdateAllPlayerTextsTypewriter(locWinner.GetLocalizedString(), 1.0f);

        OnMinigameEnded?.Invoke();
    }

    public bool IsActionInPool(string actionName)
    {
        return patternGenerator.availableInputs.Any(x => string.Equals(x.actionName, actionName, System.StringComparison.OrdinalIgnoreCase));
    }
}