using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization; // Necesario para Localization

public class SimonPlayerController : MonoBehaviour
{
    [Header("Configuración de Juego")]
    public float timeToInputNextStep = 3.0f;
    public float pointsPerStep = 10f;
    public float penaltyOnFail = 50f;

    // --- PROPIEDAD PÚBLICA SOLICITADA ---
    // Esto permite al Manager acceder a la UI para dar el bonus final
    public PlayerUIInfo UiInfo { get; private set; }

    // Referencias privadas e inyectadas
    private List<SimonInputDefinition> targetSequence;
    private List<Image> uiIcons;
    private List<GameObject> initialIconObjects = new List<GameObject>();
    private TextMeshProUGUI myStatusText; // Texto específico de ESTE jugador
    private SimonMinigameManager manager;

    private int playerIndex;
    private int currentStepIndex = 0;
    private float lastInputTime;
    private bool isGameActive = false;

    // Referencias a textos de error (Inyectadas desde el Manager para no repetir config)
    private LocalizedString errTimeoutStr;
    private LocalizedString errWrongBtnStr;

    public void Initialize(int pIndex, PlayerUIInfo info, SimonMinigameManager gm,
                           List<SimonInputDefinition> seq, List<Image> icons,
                           TextMeshProUGUI statusText,
                           LocalizedString locTimeout, LocalizedString locWrong)
    {
        playerIndex = pIndex;
        UiInfo = info; // Guardamos en la propiedad pública
        manager = gm;
        targetSequence = seq;
        uiIcons = icons;
        myStatusText = statusText;

        // Guardamos las referencias de localización para usarlas al fallar
        errTimeoutStr = locTimeout;
        errWrongBtnStr = locWrong;

        // Guardamos estado inicial de iconos
        initialIconObjects.Clear();
        foreach (var img in uiIcons)
        {
            if (img != null) initialIconObjects.Add(img.gameObject);
        }

        currentStepIndex = 0;
        isGameActive = false; // Espera a que el manager active
    }

    public void EnableGame(bool enable)
    {
        isGameActive = enable;
        if (enable) lastInputTime = Time.time;
    }

    void Update()
    {
        if (!isGameActive) return;

        // Verificar Timeout
        if (Time.time - lastInputTime > timeToInputNextStep && currentStepIndex > 0)
        {
            HandleMistake(errTimeoutStr);
        }
    }

    // Método a conectar en el PlayerInput -> Events
    // Dentro de SimonPlayerController.cs

    public void OnInput(InputAction.CallbackContext context)
    {
        if (!isGameActive || !context.performed) return;

        // Ignorar ruido (mouse delta, look, etc.)
        if (context.action.name.Contains("Look") || context.action.name.Contains("Mouse")) return;

        // 1. Leemos el valor como Vector2 (incluso los botones devuelven vector, pero será 0,0 o 1,0 si es trigger)
        // Sin embargo, para botones simples usamos context.action.name. 
        // Para sticks usamos el vector.
        Vector2 inputVector = Vector2.zero;
        if (context.valueType == typeof(Vector2))
        {
            inputVector = context.ReadValue<Vector2>();
        }

        ValidateInput(context.action.name, inputVector);
    }

    private void ValidateInput(string inputActionName, Vector2 inputDir)
    {
        if (currentStepIndex >= targetSequence.Count) return;

        SimonInputDefinition expectedStep = targetSequence[currentStepIndex];
        bool isMatch = false;

        // A) Validación de Nombre
        if (string.Equals(inputActionName, expectedStep.actionName, System.StringComparison.OrdinalIgnoreCase))
        {
            // B) Validación de Dirección
            if (expectedStep.IsDirectional)
            {
                // Es un Stick/D-pad: Verificamos si la dirección coincide (usando producto punto o distancia)
                // Usamos 0.5f como umbral para ser permisivos con el stick analógico
                if (inputDir.magnitude > 0.1f && Vector2.Dot(inputDir.normalized, expectedStep.requiredDirection.normalized) > 0.5f)
                {
                    isMatch = true;
                }
            }
            else
            {
                // Es un Botón (0,0): Basta con que el nombre coincida (ya validado arriba)
                isMatch = true;
            }
        }

        if (isMatch)
        {
            HandleSuccessStep();
        }
        else
        {
            // Solo penalizamos si es una acción relevante del juego
            if (manager.IsActionInPool(inputActionName))
            {
                // Ojo: Si es el stick correcto pero dirección incorrecta, también debería fallar aquí
                HandleMistake(errWrongBtnStr);
            }
        }
    }

    private void HandleSuccessStep()
    {
        lastInputTime = Time.time;

        // Limpiar mensaje de error si había uno
        if (myStatusText != null) myStatusText.text = "";

        // Ocultar icono completado
        if (currentStepIndex < uiIcons.Count && uiIcons[currentStepIndex] != null)
        {
            uiIcons[currentStepIndex].gameObject.SetActive(false);
        }

        // Sumar puntos parciales
        if (UiInfo != null) UiInfo.AddScore(pointsPerStep);

        currentStepIndex++;

        // Verificar victoria
        if (currentStepIndex >= targetSequence.Count)
        {
            isGameActive = false;
            manager.PlayerFinished(playerIndex);
        }
    }

    private void HandleMistake(LocalizedString errorMsg)
    {
        // 1. Reiniciar lógica
        currentStepIndex = 0;
        lastInputTime = Time.time;

        // 2. Penalizar
        if (UiInfo != null) UiInfo.AddScore(-penaltyOnFail);

        // 3. Restaurar UI visual
        foreach (var obj in initialIconObjects)
        {
            if (obj != null) obj.SetActive(true);
        }

        // 4. Mostrar feedback visual (Localized)
        if (myStatusText != null)
        {
            // Refrescamos el string localizado
            errorMsg.RefreshString();
            myStatusText.text = errorMsg.GetLocalizedString();

            // Opcional: Borrar el texto después de 1 segundo (Corrutina simple)
            StopAllCoroutines();
            StartCoroutine(ClearTextRoutine());
        }
    }

    private System.Collections.IEnumerator ClearTextRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        if (myStatusText != null) myStatusText.text = "";
    }
}