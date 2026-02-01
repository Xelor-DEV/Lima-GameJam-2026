using UnityEngine;
using UnityEngine.InputSystem;
using NexusChaser.CycloneAMS;

public class BrushController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La punta del pincel/brocha")]
    public Transform brushTip;

    [Header("Visuals & Audio")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite paintSprite;
    [SerializeField] private float animationSpeed = 0.2f;

    [SerializeField] private CycloneClip paintLoopSfx;
    [SerializeField] private CycloneClip completeSfx;

    // --- OPTIMIZACIÓN: Buffer de Pitch ---
    [Header("Audio Variation (Optimized)")]
    [Tooltip("Tono mínimo")]
    [SerializeField] private float minPitch = 0.9f;
    [Tooltip("Tono máximo")]
    [SerializeField] private float maxPitch = 1.1f;

    [Tooltip("Controla qué tan rápido cambia el valor en el buffer pre-calculado. Valores bajos (0.05) = ondas suaves. Valores altos (0.5) = cambios bruscos.")]
    [SerializeField] private float noiseStep = 0.1f;

    // Buffer de tamaño fijo para evitar cálculos en Update
    private readonly float[] _pitchBuffer = new float[500];
    private int _bufferIndex = 0;
    // -------------------------------------

    // Referencias inyectadas
    private FillablePattern currentPattern;
    private PlayerUIInfo linkedUI;
    private FillMinigameManager manager;
    private int playerIndex;

    [Header("Configuración")]
    public float pointsPerPixel = 1f;
    public float penaltyPerFrame = 0.5f;
    public float winThreshold = 0.95f;

    [HideInInspector] public Color brushColor;

    // Estado
    private bool isPaintingActive = false;
    private bool patternFinished = false;

    // Feedback
    private bool _isSfxPlaying = false;
    private float _animTimer;
    private bool _toggleSpriteState;
    private CycloneAudioDriver _cachedDriver;

    private void Start()
    {
        _cachedDriver = CycloneAudioDriver.Instance;

        // --- 1. PRE-CALCULAR BUFFER (Solo una vez) ---
        GeneratePitchBuffer();
    }

    private void GeneratePitchBuffer()
    {
        // Usamos un offset aleatorio para que el patrón base no sea siempre idéntico al iniciar el juego
        float randomSeed = Random.Range(0f, 100f);

        for (int i = 0; i < _pitchBuffer.Length; i++)
        {
            // Calculamos el ruido una sola vez aquí
            float noiseVal = Mathf.PerlinNoise((i * noiseStep) + randomSeed, 0f);

            // Mapeamos y guardamos el resultado final
            _pitchBuffer[i] = Mathf.Lerp(minPitch, maxPitch, noiseVal);
        }
    }

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

    public void OnAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isPaintingActive = true;
            // --- 2. OFFSET ALEATORIO ---
            // Al hacer clic, saltamos a una posición aleatoria del buffer.
            // Esto garantiza que cada trazo suene distinto aunque los datos sean los mismos.
            _bufferIndex = Random.Range(0, _pitchBuffer.Length);
        }
        else if (context.canceled)
        {
            isPaintingActive = false;
        }
    }

    void Update()
    {
        if (patternFinished || currentPattern == null || brushTip == null)
        {
            HandleFeedback(false);
            return;
        }

        Vector3 tipPos = brushTip.position;
        bool isSafe = currentPattern.IsPositionSafe(tipPos);
        bool isEffectivelyPainting = false;

        if (!isSafe)
        {
            if (linkedUI != null) linkedUI.AddScore(-penaltyPerFrame);
        }
        else if (isPaintingActive)
        {
            isEffectivelyPainting = true;
            int pixelsFilled = currentPattern.TryFillArea(tipPos);
            if (pixelsFilled > 0)
            {
                if (linkedUI != null) linkedUI.AddScore(pixelsFilled * pointsPerPixel);
            }

            if (currentPattern.GetProgress() >= winThreshold)
            {
                CompletePattern();
            }
        }

        HandleFeedback(isEffectivelyPainting);
    }

    void HandleFeedback(bool isActionActive)
    {
        CycloneAudioDriver driver = _cachedDriver != null ? _cachedDriver : CycloneAudioDriver.Instance;
        if (driver == null) return;

        // 1. Audio Logic
        if (paintLoopSfx != null)
        {
            if (isActionActive)
            {
                if (!_isSfxPlaying)
                {
                    driver.Play(paintLoopSfx);
                    _isSfxPlaying = true;
                }

                // --- 3. LECTURA OPTIMIZADA ---
                // Leemos del buffer pre-calculado (Coste insignificante)
                float targetPitch = _pitchBuffer[_bufferIndex];
                driver.SetPitch(paintLoopSfx, targetPitch);

                // Avanzamos el índice y damos la vuelta (Loop) si llegamos a 500
                _bufferIndex = (_bufferIndex + 1) % _pitchBuffer.Length;
            }
            else if (!isActionActive && _isSfxPlaying)
            {
                driver.Stop(paintLoopSfx);
                driver.SetPitch(paintLoopSfx, 1.0f);
                _isSfxPlaying = false;
            }
        }

        // 2. Visuals
        if (visualRenderer != null && idleSprite != null && paintSprite != null)
        {
            if (isActionActive)
            {
                _animTimer += Time.deltaTime;
                if (_animTimer >= animationSpeed)
                {
                    _animTimer = 0;
                    _toggleSpriteState = !_toggleSpriteState;
                    visualRenderer.sprite = _toggleSpriteState ? paintSprite : idleSprite;
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
        isPaintingActive = false;

        HandleFeedback(false);

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
        if (_cachedDriver != null && _isSfxPlaying)
        {
            _cachedDriver.Stop(paintLoopSfx);
            _cachedDriver.SetPitch(paintLoopSfx, 1.0f);
            _isSfxPlaying = false;
        }
    }
}