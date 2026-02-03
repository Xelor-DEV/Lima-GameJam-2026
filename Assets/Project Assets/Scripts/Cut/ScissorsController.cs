using UnityEngine;
using UnityEngine.InputSystem;
using NexusChaser.CycloneAMS;

public class ScissorsController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cuttingTip;

    [Header("Visuals & Audio")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite cutSprite;
    [SerializeField] private float animationSpeed = 0.15f;
    [SerializeField] private CycloneClip cutLoopSfx;
    [SerializeField] private CycloneClip completeSfx;

    // --- NUEVO: Variación de Pitch (Anti-Efecto Doble) ---
    [Header("Audio Variation")]
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;
    [SerializeField] private float noiseStep = 0.1f;

    private readonly float[] _pitchBuffer = new float[500];
    private int _bufferIndex = 0;
    // -----------------------------------------------------

    private CuttingPattern currentTargetPattern;
    private PlayerUIInfo linkedUI;
    private PatternMinigameManager manager;
    private int playerIndex;

    [Header("Configuración")]
    public float pointsPerNode = 10f;
    public float penaltyPerFrame = 0.5f;
    public float winThreshold = 0.98f;

    [HideInInspector] public Color lineColor;

    private bool isCuttingInputActive = false;
    private bool patternFinished = false;

    // Feedback vars
    private float _animTimer;
    private bool _toggleSpriteState;
    private CycloneAudioDriver _cachedDriver;

    // ID del audio actual
    private int _currentAudioId = -1;
    // Bandera de seguridad para bloquear la creación de audios en el último frame
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
        if (currentTargetPattern != null) currentTargetPattern.InitializeVisuals(lineColor);
    }

    public void OnCutAction(InputAction.CallbackContext context)
    {
        if (!this.enabled || _isFrozen) return;

        if (context.performed)
        {
            isCuttingInputActive = true;
            _bufferIndex = Random.Range(0, _pitchBuffer.Length); // Variación inicial
        }
        else if (context.canceled)
        {
            isCuttingInputActive = false;
        }
    }

    void Update()
    {
        // 1. SEGURIDAD MÁXIMA: Si está congelado, no ejecutar NADA.
        if (_isFrozen) return;

        // 2. Si el manager dice que terminó, congelar y salir.
        if (manager != null && manager.IsGameFinished)
        {
            FreezeController();
            return;
        }

        if (patternFinished || currentTargetPattern == null || cuttingTip == null)
        {
            HandleFeedback(false);
            return;
        }

        bool isEffectivelyCutting = false;

        if (isCuttingInputActive)
        {
            bool isSafe = currentTargetPattern.IsPositionSafe(cuttingTip.position);

            if (isSafe)
            {
                isEffectivelyCutting = true;
                ProcessCutMovement();
            }
            else
            {
                if (linkedUI != null) linkedUI.AddScore(-penaltyPerFrame);
            }
        }

        HandleFeedback(isEffectivelyCutting);
    }

    void ProcessCutMovement()
    {
        if (currentTargetPattern.TryCutNode(cuttingTip.position))
        {
            if (linkedUI != null) linkedUI.AddScore(pointsPerNode);
        }

        if (currentTargetPattern.GetProgress() >= winThreshold)
        {
            CompletePattern();
        }
    }

    void HandleFeedback(bool isActionActive)
    {
        // 3. SEGURIDAD REDUNDANTE: Si nos congelaron hace un microsegundo, abortar audio.
        if (_isFrozen)
        {
            StopMyAudio();
            return;
        }

        CycloneAudioDriver driver = _cachedDriver != null ? _cachedDriver : CycloneAudioDriver.Instance;
        if (driver == null) return;

        // --- LÓGICA DE AUDIO MEJORADA ---
        if (cutLoopSfx != null)
        {
            if (isActionActive)
            {
                // Solo iniciar si no tenemos un ID válido
                if (_currentAudioId == -1)
                {
                    // ANTI-DOBLE SONIDO: 
                    // Paramos explícitamente cualquier versión "Compartida" que pudiera haber quedado sonando
                    driver.Stop(cutLoopSfx);

                    // Iniciamos nuestra versión Trackeada
                    _currentAudioId = driver.PlayTracked(cutLoopSfx);
                }

                // APLICAR PITCH (Esto elimina el efecto robótico/doble)
                float targetPitch = _pitchBuffer[_bufferIndex];
                driver.SetPitchTracked(_currentAudioId, targetPitch);
                _bufferIndex = (_bufferIndex + 1) % _pitchBuffer.Length;
            }
            else
            {
                // Si soltamos el botón, apagar
                StopMyAudio();
            }
        }

        // Visuals
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

    // Helper privado para limpieza segura
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
        isCuttingInputActive = false;

        StopMyAudio(); // Parar loop

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
        // 1. Activar bandera INMEDIATAMENTE.
        // Esto previene que HandleFeedback vuelva a crear un audio si Update corre una vez más.
        _isFrozen = true;

        // 2. Matar el audio actual trackeado
        StopMyAudio();

        // 3. Matar el audio compartido por si acaso (Seguridad Anti-Doble)
        CycloneAudioDriver driver = _cachedDriver != null ? _cachedDriver : CycloneAudioDriver.Instance;
        if (driver != null && cutLoopSfx != null)
        {
            driver.Stop(cutLoopSfx);
        }

        // 4. Limpieza de estado visual
        isCuttingInputActive = false;
        _toggleSpriteState = false;
        if (visualRenderer != null) visualRenderer.sprite = idleSprite;

        // 5. Bloqueo UI y Apagado
        if (linkedUI != null) linkedUI.LockScoring();
        this.enabled = false;
    }

    private void OnDisable()
    {
        // Última red de seguridad: Si el objeto se destruye o desactiva por Unity
        StopMyAudio();
    }
}