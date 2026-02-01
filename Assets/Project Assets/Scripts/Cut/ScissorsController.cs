using UnityEngine;
using UnityEngine.InputSystem;

// Ya no requiere LineRenderer
public class ScissorsController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El punto exacto de la punta de la tijera")]
    public Transform cuttingTip;

    // Referencias inyectadas
    private CuttingPattern currentTargetPattern;
    private PlayerUIInfo linkedUI;
    private PatternMinigameManager manager;
    private int playerIndex;

    [Header("Configuración")]
    public float pointsPerNode = 10f;
    public float penaltyPerFrame = 0.5f;
    public float winThreshold = 0.98f;

    [HideInInspector] public Color lineColor; // El manager lo asigna, pero solo lo usaremos para pasarlo al patrón

    // Estado
    private bool isCuttingInputActive = false;
    private bool patternFinished = false;

    // --- SETUP ---
    public void Initialize(int pIndex, PlayerUIInfo ui, PatternMinigameManager mgr)
    {
        playerIndex = pIndex;
        linkedUI = ui;
        manager = mgr;
    }

    public void SetCurrentPattern(CuttingPattern pattern)
    {
        currentTargetPattern = pattern;
        patternFinished = false;

        // --- NUEVO: Inicializar visuales del patrón ---
        // Le decimos al patrón de qué color debe pintarse inicialmente
        if (currentTargetPattern != null)
        {
            currentTargetPattern.InitializeVisuals(lineColor);
        }
    }

    // --- INPUT SYSTEM CALLBACK ---
    public void OnCutAction(InputAction.CallbackContext context)
    {
        if (context.performed) isCuttingInputActive = true;
        else if (context.canceled) isCuttingInputActive = false;
    }

    // --- LOOP PRINCIPAL ---
    void Update()
    {
        if (patternFinished || currentTargetPattern == null || cuttingTip == null) return;

        if (isCuttingInputActive)
        {
            ProcessCutMovement();
        }
    }

    void ProcessCutMovement()
    {
        Vector3 tipPos = cuttingTip.position;

        // Lógica de juego
        bool isSafe = currentTargetPattern.IsPositionSafe(tipPos);

        if (isSafe)
        {
            // Intentar cortar y sumar puntos
            // Ahora TryCutNode se encarga de actualizar la visual del patrón
            if (currentTargetPattern.TryCutNode(tipPos))
            {
                if (linkedUI != null) linkedUI.AddScore(pointsPerNode);
            }

            // Chequeo de victoria
            if (currentTargetPattern.GetProgress() >= winThreshold)
            {
                CompletePattern();
            }
        }
        else
        {
            // Penalización
            if (linkedUI != null) linkedUI.AddScore(-penaltyPerFrame);
        }
    }

    void CompletePattern()
    {
        patternFinished = true;
        isCuttingInputActive = false;
        manager.OnPatternCompleted(playerIndex);
    }

    public void ForceAddScore(float amount)
    {
        if (linkedUI != null) linkedUI.AddScore(amount);
    }
}