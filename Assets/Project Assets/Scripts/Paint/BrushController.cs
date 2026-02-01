using UnityEngine;
using UnityEngine.InputSystem;

public class BrushController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La punta del pincel/brocha")]
    public Transform brushTip;

    // Referencias inyectadas
    private FillablePattern currentPattern;
    private PlayerUIInfo linkedUI;
    private FillMinigameManager manager;
    private int playerIndex;

    [Header("Configuración")]
    public float pointsPerPixel = 1f;
    public float penaltyPerFrame = 0.5f;
    public float winThreshold = 0.95f; // 95% rellenado para ganar

    [HideInInspector] public Color brushColor;

    // Estado
    private bool isPaintingActive = false; // Si se presiona el botón
    private bool patternFinished = false;

    public void Initialize(int pIndex, PlayerUIInfo ui, FillMinigameManager mgr)
    {
        playerIndex = pIndex;
        linkedUI = ui;
        manager = mgr;
    }

    public void SetCurrentPattern(FillablePattern pattern)
    {
        currentPattern = pattern;
        patternFinished = false;

        if (currentPattern != null)
        {
            currentPattern.InitializeVisuals(brushColor);
        }
    }

    // Callback de Input System (Igual que en Scissors)
    public void OnAction(InputAction.CallbackContext context)
    {
        if (context.performed) isPaintingActive = true;
        else if (context.canceled) isPaintingActive = false;
    }

    void Update()
    {
        if (patternFinished || currentPattern == null || brushTip == null) return;

        Vector3 tipPos = brushTip.position;

        // 1. Verificar si estamos "dentro de la línea" (zona segura)
        // Nota: A veces queremos penalizar solo si está PINTANDO fuera, 
        // o si simplemente ESTÁ fuera. Aquí asumo penalización si el pincel está fuera.
        bool isSafe = currentPattern.IsPositionSafe(tipPos);

        if (!isSafe)
        {
            // Penalización por salirse
            if (linkedUI != null) linkedUI.AddScore(-penaltyPerFrame);
            // Opcional: Feedback visual de error aquí
        }
        else if (isPaintingActive)
        {
            // 2. Si es seguro y estamos pintando -> Rellenar
            int pixelsFilled = currentPattern.TryFillArea(tipPos);

            if (pixelsFilled > 0)
            {
                if (linkedUI != null) linkedUI.AddScore(pixelsFilled * pointsPerPixel);
            }

            // 3. Chequeo de victoria
            if (currentPattern.GetProgress() >= winThreshold)
            {
                CompletePattern();
            }
        }
    }

    void CompletePattern()
    {
        patternFinished = true;
        isPaintingActive = false;
        manager.OnPatternCompleted(playerIndex);
    }

    public void ForceAddScore(float amount)
    {
        if (linkedUI != null) linkedUI.AddScore(amount);
    }
}