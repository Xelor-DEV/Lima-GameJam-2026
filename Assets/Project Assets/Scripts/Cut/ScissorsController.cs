using UnityEngine;
using UnityEngine.InputSystem;
using NexusChaser.CycloneAMS; // Necesario para audio

public class ScissorsController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El punto exacto de la punta de la tijera")]
    public Transform cuttingTip;

    // --- NUEVO: Referencias Visuales y de Audio ---
    [Header("Visuals & Audio")]
    [SerializeField] private SpriteRenderer visualRenderer; // El SpriteRenderer del objeto
    [SerializeField] private Sprite idleSprite;             // Sprite tijera abierta/quieta
    [SerializeField] private Sprite cutSprite;              // Sprite tijera cerrada/cortando
    [SerializeField] private float animationSpeed = 0.15f;  // Velocidad de cambio de sprite

    [SerializeField] private CycloneClip cutLoopSfx;
    [SerializeField] private CycloneClip completeSfx; // El clip loopeado en Cyclone
    // ----------------------------------------------

    // Referencias inyectadas
    private CuttingPattern currentTargetPattern;
    private PlayerUIInfo linkedUI;
    private PatternMinigameManager manager;
    private int playerIndex;

    [Header("Configuración")]
    public float pointsPerNode = 10f;
    public float penaltyPerFrame = 0.5f;
    public float winThreshold = 0.98f;

    [HideInInspector] public Color lineColor;

    // Estado
    private bool isCuttingInputActive = false;
    private bool patternFinished = false;

    // Variables internas para feedback
    private bool _isSfxPlaying = false;
    private float _animTimer;
    private bool _toggleSpriteState;
    private CycloneAudioDriver _cachedDriver;

    private void Start()
    {
        // Guardamos la referencia AQUÍ, cuando es seguro que existe.
        _cachedDriver = CycloneAudioDriver.Instance;
    }

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
        // Si terminamos, aseguramos que todo se detenga
        if (patternFinished || currentTargetPattern == null || cuttingTip == null)
        {
            HandleFeedback(false);
            return;
        }

        // Determinamos si realmente estamos "cortando" (Input activo + Zona Segura)
        bool isEffectivelyCutting = false;

        if (isCuttingInputActive)
        {
            bool isSafe = currentTargetPattern.IsPositionSafe(cuttingTip.position);

            if (isSafe)
            {
                isEffectivelyCutting = true;
                ProcessCutMovement(); // Lógica original de corte
            }
            else
            {
                // Penalización (Input activo pero fuera de línea)
                if (linkedUI != null) linkedUI.AddScore(-penaltyPerFrame);
            }
        }

        // Actualizamos Audio y Sprite basado en si estamos cortando efectivamente
        HandleFeedback(isEffectivelyCutting);
    }

    void ProcessCutMovement()
    {
        // La lógica de puntos ya está validada por el flag isSafe arriba
        if (currentTargetPattern.TryCutNode(cuttingTip.position))
        {
            if (linkedUI != null) linkedUI.AddScore(pointsPerNode);
        }

        if (currentTargetPattern.GetProgress() >= winThreshold)
        {
            CompletePattern();
        }
    }

    // --- NUEVO: Lógica de Animación y Audio ---
    void HandleFeedback(bool isActionActive)
    {
        // Usamos _cachedDriver si existe, o Instance si no lo tenemos (backup)
        CycloneAudioDriver driver = _cachedDriver != null ? _cachedDriver : CycloneAudioDriver.Instance;

        // Si por alguna razón el driver es nulo (ej. error init), salimos
        if (driver == null) return;

        // 1. Audio Logic
        if (cutLoopSfx != null)
        {
            if (isActionActive && !_isSfxPlaying)
            {
                driver.Play(cutLoopSfx);
                _isSfxPlaying = true;
            }
            else if (!isActionActive && _isSfxPlaying)
            {
                driver.Stop(cutLoopSfx);
                _isSfxPlaying = false;
            }
        }

        // ... (Visuals Logic igual) ...
        if (visualRenderer != null && idleSprite != null && cutSprite != null)
        {
            if (isActionActive)
            {
                _animTimer += Time.deltaTime;
                if (_animTimer >= animationSpeed)
                {
                    _animTimer = 0;
                    _toggleSpriteState = !_toggleSpriteState;
                    visualRenderer.sprite = _toggleSpriteState ? cutSprite : idleSprite;
                }
            }
            else
            {
                visualRenderer.sprite = idleSprite;
                _toggleSpriteState = false;
                _animTimer = 0;
            }
        }
    }

    void CompletePattern()
    {
        patternFinished = true;
        isCuttingInputActive = false;

        // Detenemos loop visual/audio
        HandleFeedback(false);

        // Sonido de éxito (usando caché)
        if (_cachedDriver != null && completeSfx != null)
        {
            _cachedDriver.PlayOneShot(completeSfx);
        }

        manager.OnPatternCompleted(playerIndex);
    }

    public void ForceAddScore(float amount)
    {
        if (linkedUI != null) linkedUI.AddScore(amount);
    }

    private void OnDisable()
    {
        // --- CORRECCIÓN CRÍTICA ---
        // Verificamos la referencia en caché. Si Unity ya destruyó el Driver (por cambio de escena),
        // _cachedDriver será "null" y NO entraremos al if, evitando resucitar el Singleton.
        if (_cachedDriver != null && _isSfxPlaying)
        {
            _cachedDriver.Stop(cutLoopSfx);
            _isSfxPlaying = false;
        }
    }
}