using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using NexusChaser.CycloneAMS; // Importar tu namespace de audio
using System.Collections; // Necesario para IEnumerator

public class CheatCodeDetector : MonoBehaviour
{
    [System.Serializable]
    public struct CheatInputStep
    {
        [Tooltip("El nombre de la acción (ej: 'Stick', 'MoveSelection', 'Select')")]
        public string actionName;

        [Tooltip("Dirección esperada. Si usas un Axis (MoveSelection), 1 es (1,0) y -1 es (-1,0)")]
        public Vector2 requiredDirection;
    }

    [System.Serializable]
    public class CheatProfile
    {
        public string cheatName = "New Cheat";
        public CharacterData characterToUnlock;
        public List<CheatInputStep> sequence;
        [HideInInspector] public int currentStepIndex = 0;
    }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Configuración")]
    [SerializeField] private List<CheatProfile> cheatCodes = new List<CheatProfile>();
    [SerializeField] private float maxDelayBetweenInputs = 1.0f;

    [Header("Referencias")]
    [SerializeField] private PlayersSessionData sessionData;
    [SerializeField] private CharacterSelector characterSelector;
    [SerializeField] private PlayerInput playerInput; // Solo para referencia de índice

    [Header("Audio Feedback (Unlock)")]
    [SerializeField] private CycloneClip unlockBellSfx;   // Sonido 1: La Campana (Impacto)
    [SerializeField] private CycloneClip announcerSfx;    // Sonido 2: El Anunciador (Hype)
    [Tooltip("Tiempo en segundos a esperar antes de que hable el anunciador.")]
    [SerializeField] private float announcerDelay = 0.3f; // Configurable en Inspector

    private float _lastInputTime;
    private int _frameLastStepSuccessful = -1;

    // --- API PÚBLICA ---

    public bool WasInputConsumedByCheat()
    {
        return Time.frameCount == _frameLastStepSuccessful;
    }

    /// <summary>
    /// ESTE ES EL MÉTODO QUE DEBES ASIGNAR EN EL INSPECTOR (Events)
    /// Arrastra este script a los eventos 'Stick', 'MoveSelection', 'Select', etc.
    /// y selecciona esta función.
    /// </summary>
    public void OnInput(InputAction.CallbackContext context)
    {
        // 1. Filtrar fases: Solo nos interesa cuando se presiona (Performed)
        if (!context.performed) return;

        // 2. UNIFICACIÓN DE DATOS (Axis -> Vector2)
        Vector2 inputVector = GetUnifiedVector(context);
        string actionName = context.action.name;

        // Debug opcional para verificar la unificación
        if (showDebugLogs && !IsNoise(actionName))
        {
            Debug.Log($"[CheatSystem] Recibido: {actionName} | Valor Unificado: {inputVector}");
        }

        // 3. Reset por tiempo
        if (Time.time - _lastInputTime > maxDelayBetweenInputs)
        {
            ResetAllCheats();
        }
        _lastInputTime = Time.time;

        bool anyCheatAdvanced = false;

        // 4. Comprobar secuencia
        foreach (var cheat in cheatCodes)
        {
            if (cheat.sequence.Count == 0) continue;

            CheatInputStep step = cheat.sequence[cheat.currentStepIndex];

            // Verificamos si coincide
            if (IsMatchingInput(actionName, inputVector, step))
            {
                cheat.currentStepIndex++;
                anyCheatAdvanced = true;

                if (showDebugLogs) Debug.Log($"[CheatSystem] ✅ AVANCE: {cheat.cheatName} ({cheat.currentStepIndex}/{cheat.sequence.Count})");

                if (cheat.currentStepIndex >= cheat.sequence.Count)
                {
                    UnlockCharacter(cheat);
                    cheat.currentStepIndex = 0;
                }
            }
            else
            {
                // Si no es ruido, reseteamos
                if (!IsNoise(actionName))
                {
                    cheat.currentStepIndex = 0;
                    // Retry inmediato (por si el input actual es el inicio de la secuencia)
                    if (cheat.sequence.Count > 0 && IsMatchingInput(actionName, inputVector, cheat.sequence[0]))
                    {
                        cheat.currentStepIndex = 1;
                        anyCheatAdvanced = true;
                    }
                }
            }
        }

        if (anyCheatAdvanced)
        {
            _frameLastStepSuccessful = Time.frameCount;
        }
    }

    // --- LÓGICA DE UNIFICACIÓN ---

    private Vector2 GetUnifiedVector(InputAction.CallbackContext context)
    {
        // Si es Vector2 (Stick, D-Pad), lo usamos tal cual
        if (context.valueType == typeof(Vector2))
        {
            return context.ReadValue<Vector2>();
        }

        // Si es Float (Axis, MoveSelection), lo convertimos a Vector Horizontal
        // 1.0 -> (1, 0) | -1.0 -> (-1, 0)
        if (context.valueType == typeof(float))
        {
            float val = context.ReadValue<float>();
            return new Vector2(val, 0);
        }

        // Si es botón simple sin valor, asumimos magnitud 0 o dirección neutra
        return Vector2.zero;
    }

    private bool IsMatchingInput(string currentAction, Vector2 currentDir, CheatInputStep step)
    {
        // 1. Validar Nombre (Ignorando mayúsculas)
        if (!string.Equals(currentAction, step.actionName, System.StringComparison.OrdinalIgnoreCase))
            return false;

        // 2. Validar Dirección
        if (step.requiredDirection != Vector2.zero)
        {
            // Producto punto para saber si van en la misma dirección
            // Normalizamos para evitar problemas de magnitud (0.5 vs 1.0)
            if (currentDir.magnitude < 0.1f) return false; // Evitar errores con vectores cero

            if (Vector2.Dot(currentDir.normalized, step.requiredDirection.normalized) < 0.5f)
                return false;
        }

        return true;
    }

    private bool IsNoise(string actionName)
    {
        return actionName.Equals("Look", System.StringComparison.OrdinalIgnoreCase) ||
               actionName.Equals("Mouse Position", System.StringComparison.OrdinalIgnoreCase);
    }

    private void ResetAllCheats()
    {
        foreach (var cheat in cheatCodes) cheat.currentStepIndex = 0;
    }

    private void UnlockCharacter(CheatProfile cheat)
    {
        Debug.Log($"[CheatSystem] 🎉 DESBLOQUEADO: {cheat.cheatName}");

        // 1. Lógica Visual y de Datos (Existente)
        if (sessionData != null && cheat.characterToUnlock != null && playerInput.playerIndex < sessionData.players.Count)
        {
            Player p = sessionData.players[playerInput.playerIndex];
            if (p.lockedChars.Contains(cheat.characterToUnlock))
            {
                p.lockedChars.Remove(cheat.characterToUnlock);
            }
            if (characterSelector != null) characterSelector.JumpToCharacter(cheat.characterToUnlock);
        }

        // 2. NUEVA Lógica de Audio Secuencial
        StartCoroutine(PlayUnlockSequence());
    }

    private IEnumerator PlayUnlockSequence()
    {
        // Verificamos que el driver exista para evitar errores
        if (CycloneAudioDriver.Instance == null) yield break;

        // PASO 1: Suena la campana inmediatamente (T=0)
        if (unlockBellSfx != null)
        {
            CycloneAudioDriver.Instance.PlayOneShot(unlockBellSfx); // [Cite: CycloneAudioDriver.cs]
        }

        // PASO 2: Esperamos el tiempo configurado (el "respiro" dramático)
        // Esto permite que el agudo de la campana brille antes de que entre la voz
        if (announcerDelay > 0)
        {
            yield return new WaitForSeconds(announcerDelay);
        }

        // PASO 3: Entra el Anunciador
        if (announcerSfx != null)
        {
            CycloneAudioDriver.Instance.PlayOneShot(announcerSfx); // [Cite: CycloneAudioDriver.cs]
        }
    }
}