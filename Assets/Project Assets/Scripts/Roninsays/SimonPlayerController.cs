using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using DG.Tweening;
using System.Collections;

public class SimonPlayerController : MonoBehaviour
{
    [Header("Configuración de Juego")]
    public float timeToInputNextStep = 3.0f;
    public float pointsPerStep = 10f;
    public float penaltyOnFail = 50f;
    public float errorShowDuration = 1.5f;

    [Header("Configuración Visual")]
    [Tooltip("Velocidad del parpadeo cuando el tiempo está lleno (Lento)")]
    public float minBlinkSpeed = 1.0f; // Antes era 2f
    [Tooltip("Velocidad del parpadeo cuando el tiempo está por acabarse (Rápido)")]
    public float maxBlinkSpeed = 4.0f; // Antes era 8f (Bájalo a 3f si aún es muy rápido)

    public PlayerUIInfo UiInfo { get; private set; }

    private List<SimonInputDefinition> targetSequence;
    private List<Image> uiIcons;
    private List<GameObject> initialIconObjects = new List<GameObject>();
    private TextMeshProUGUI myStatusText;
    private SimonMinigameManager manager;

    private int playerIndex;
    private int currentStepIndex = 0;
    private float lastInputTime;

    // Estados
    private bool isGameActive = false;
    private bool isShowingError = false;
    private bool waitingForFirstInput = true;

    private LocalizedString errTimeoutStr;
    private LocalizedString errWrongBtnStr;

    public void Initialize(int pIndex, PlayerUIInfo info, SimonMinigameManager gm,
                           List<SimonInputDefinition> seq, List<Image> icons,
                           TextMeshProUGUI statusText,
                           LocalizedString locTimeout, LocalizedString locWrong)
    {
        playerIndex = pIndex;
        UiInfo = info;
        manager = gm;
        targetSequence = seq;
        uiIcons = icons;
        myStatusText = statusText;
        errTimeoutStr = locTimeout;
        errWrongBtnStr = locWrong;

        initialIconObjects.Clear();
        foreach (var img in uiIcons)
        {
            if (img != null)
            {
                initialIconObjects.Add(img.gameObject);
                img.transform.localScale = Vector3.one;
                img.color = Color.white;
            }
        }

        ResetState();
    }

    private void ResetState()
    {
        currentStepIndex = 0;
        isGameActive = false;
        isShowingError = false;
        waitingForFirstInput = true;
    }

    public void EnableGame(bool enable)
    {
        isGameActive = enable;
        if (enable)
        {
            waitingForFirstInput = true;
            isShowingError = false;
            lastInputTime = Time.time;
        }
    }

    void Update()
    {
        if (!isGameActive || isShowingError) return;

        // --- LÓGICA DE ESPERA INICIAL ---
        if (waitingForFirstInput)
        {
            if (myStatusText != null)
            {
                myStatusText.text = timeToInputNextStep.ToString("F1") + "s";
                myStatusText.color = Color.white;
            }
            return;
        }

        float timeElapsed = Time.time - lastInputTime;
        float remainingTime = timeToInputNextStep - timeElapsed;

        // 1. Mostrar Cuenta Regresiva
        if (myStatusText != null)
        {
            float displayTime = Mathf.Max(0, remainingTime);
            myStatusText.text = displayTime.ToString("F1") + "s";

            if (remainingTime < 1.0f) myStatusText.color = Color.red;
            else myStatusText.color = Color.white;
        }

        // 2. Efecto de Parpadeo CONTROLADO
        if (currentStepIndex < uiIcons.Count && uiIcons[currentStepIndex] != null)
        {
            Image currentIcon = uiIcons[currentStepIndex];

            float urgency = timeElapsed / timeToInputNextStep;

            // Usamos las variables públicas para controlar la velocidad
            float blinkSpeed = Mathf.Lerp(minBlinkSpeed, maxBlinkSpeed, urgency * urgency);

            float blinkVal = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            // Interpolamos hacia rojo suave
            currentIcon.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), blinkVal + (urgency * 0.6f));
        }

        // 3. Verificar Timeout
        if (timeElapsed > timeToInputNextStep)
        {
            HandleMistake(errTimeoutStr);
        }
    }

    public void OnInput(InputAction.CallbackContext context)
    {
        if (!isGameActive || isShowingError || !context.performed) return;
        if (context.action.name.Contains("Look") || context.action.name.Contains("Mouse")) return;

        if (waitingForFirstInput)
        {
            waitingForFirstInput = false;
            lastInputTime = Time.time;
        }

        Vector2 inputVector = Vector2.zero;
        if (context.valueType == typeof(Vector2)) inputVector = context.ReadValue<Vector2>();

        ValidateInput(context.action.name, inputVector);
    }

    private void ValidateInput(string inputActionName, Vector2 inputDir)
    {
        if (currentStepIndex >= targetSequence.Count) return;

        SimonInputDefinition expectedStep = targetSequence[currentStepIndex];
        bool isMatch = false;

        if (string.Equals(inputActionName, expectedStep.actionName, System.StringComparison.OrdinalIgnoreCase))
        {
            if (expectedStep.IsDirectional)
            {
                if (inputDir.magnitude > 0.1f && Vector2.Dot(inputDir.normalized, expectedStep.requiredDirection.normalized) > 0.5f)
                    isMatch = true;
            }
            else isMatch = true;
        }

        if (isMatch) HandleSuccessStep();
        else if (manager.IsActionInPool(inputActionName)) HandleMistake(errWrongBtnStr);
    }

    private void HandleSuccessStep()
    {
        lastInputTime = Time.time;
        if (UiInfo != null) UiInfo.AddScore(pointsPerStep);

        // Pop Out Effect
        if (currentStepIndex < uiIcons.Count && uiIcons[currentStepIndex] != null)
        {
            Image completedIcon = uiIcons[currentStepIndex];
            completedIcon.color = Color.white;
            completedIcon.transform
                .DOScale(Vector3.one * 1.5f, 0.15f)
                .OnComplete(() => {
                    completedIcon.transform
                        .DOScale(Vector3.zero, 0.15f)
                        .OnComplete(() => completedIcon.gameObject.SetActive(false));
                });
        }

        currentStepIndex++;

        if (currentStepIndex >= targetSequence.Count)
        {
            isGameActive = false;
            if (myStatusText != null) myStatusText.text = "";
            manager.PlayerFinished(playerIndex);
        }
    }

    private void HandleMistake(LocalizedString errorMsg)
    {
        StartCoroutine(ShowErrorAndResetRoutine(errorMsg));
    }

    private IEnumerator ShowErrorAndResetRoutine(LocalizedString errorMsg)
    {
        isShowingError = true;

        if (currentStepIndex < uiIcons.Count && uiIcons[currentStepIndex] != null)
        {
            uiIcons[currentStepIndex].transform.DOKill();
            uiIcons[currentStepIndex].transform.localScale = Vector3.one;
            uiIcons[currentStepIndex].color = Color.white;
        }

        if (UiInfo != null) UiInfo.AddScore(-penaltyOnFail);

        foreach (var obj in initialIconObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                obj.transform.DOKill();
                obj.transform.localScale = Vector3.one;
                obj.GetComponent<Image>().color = Color.white;
            }
        }

        if (myStatusText != null)
        {
            myStatusText.DOKill();
            errorMsg.RefreshString();
            myStatusText.text = errorMsg.GetLocalizedString();
            myStatusText.color = Color.red;
            myStatusText.transform.DOPunchPosition(Vector3.right * 10f, 0.5f, 20, 90);
        }

        yield return new WaitForSeconds(errorShowDuration);

        currentStepIndex = 0;

        if (myStatusText != null)
        {
            myStatusText.color = Color.white;
            myStatusText.text = "";
        }

        isShowingError = false;
        waitingForFirstInput = true;
    }
}