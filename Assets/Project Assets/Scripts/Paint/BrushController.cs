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

    [Tooltip("Controla qué tan rápido cambia el valor en el buffer pre-calculado.")]
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
    private float _animTimer;
    private bool _toggleSpriteState;
    private CycloneAudioDriver _cachedDriver;

    // ID del audio trackeado
    private int _currentAudioId = -1;

    // --- NUEVO: Bandera de seguridad "Freeze" ---
    private bool _isFrozen = false;

    private void Start()
    {
        _cachedDriver = CycloneAudioDriver.Instance;
        GeneratePitchBuffer();
    }

    private void GeneratePitchBuffer()
    {
        float randomSeed = Random.Range(0f, 100f);
        for (int i = 0; i < _pitchBuffer.Length; i++)
        {
            float noiseVal = Mathf.PerlinNoise((i * noiseStep) + randomSeed, 0f);
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
        // 1. SEGURIDAD: Si está congelado, ignorar input
        if (!this.enabled || _isFrozen) return;

        if (context.performed)
        {
            isPaintingActive = true;
            _bufferIndex = Random.Range(0, _pitchBuffer.Length);
        }
        else if (context.canceled)
        {
            isPaintingActive = false;
        }
    }

    void Update()
    {
        // 2. SEGURIDAD: Si está congelado, salir inmediatamente
        if (_isFrozen) return;

        // Si el manager dice que terminó, congelar y salir
        if (manager != null && manager.IsGameFinished)
        {
            FreezeController();
            return;
        }

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
        // 3. SEGURIDAD: Si está congelado, asegurar silencio y salir
        if (_isFrozen)
        {
            StopMyAudio();
            return;
        }

        CycloneAudioDriver driver = _cachedDriver != null ? _cachedDriver : CycloneAudioDriver.Instance;
        if (driver == null) return;

        // --- AUDIO LOGIC ---
        if (paintLoopSfx != null)
        {
            if (isActionActive)
            {
                if (_currentAudioId == -1)
                {
                    // ANTI-DOBLE SONIDO: Parar versión compartida antes de iniciar la nuestra
                    driver.Stop(paintLoopSfx);
                    _currentAudioId = driver.PlayTracked(paintLoopSfx);
                }

                float targetPitch = _pitchBuffer[_bufferIndex];
                driver.SetPitchTracked(_currentAudioId, targetPitch);
                _bufferIndex = (_bufferIndex + 1) % _pitchBuffer.Length;
            }
            else
            {
                StopMyAudio();
            }
        }

        // --- VISUAL LOGIC ---
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

    // Helper unificado para detener el audio trackeado
    private void StopMyAudio()
    {
        CycloneAudioDriver driver = _cachedDriver != null ? _cachedDriver : CycloneAudioDriver.Instance;
        if (driver != null && _currentAudioId != -1)
        {
            driver.StopTracked(_currentAudioId);
            _currentAudioId = -1;
        }
    }

    void CompletePattern()
    {
        patternFinished = true;
        isPaintingActive = false;

        StopMyAudio();

        if (visualRenderer != null) visualRenderer.sprite = idleSprite;

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

    // --- FREEZE CONTROLLER BLINDADO ---
    public void FreezeController()
    {
        // 1. Activar bandera INMEDIATAMENTE
        _isFrozen = true;

        // 2. Matar el audio actual trackeado
        StopMyAudio();

        // 3. Matar el audio compartido por si acaso (Seguridad Anti-Doble)
        CycloneAudioDriver driver = _cachedDriver != null ? _cachedDriver : CycloneAudioDriver.Instance;
        if (driver != null && paintLoopSfx != null)
        {
            driver.Stop(paintLoopSfx);
        }

        // 4. Limpieza de estado visual
        isPaintingActive = false;
        if (visualRenderer != null) visualRenderer.sprite = idleSprite;

        // 5. Bloqueo UI y Apagado
        if (linkedUI != null) linkedUI.LockScoring();
        this.enabled = false;
    }

    private void OnDisable()
    {
        // Red de seguridad final
        StopMyAudio();
    }
}