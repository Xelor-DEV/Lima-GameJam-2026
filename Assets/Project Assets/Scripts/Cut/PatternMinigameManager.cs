using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class PatternMinigameManager : MonoBehaviour
{
    [Header("Configuración de Patrones")]
    public CuttingPattern[] patternPrefabs;
    public Transform[] patternSpawnPoints;

    [Header("Configuración de Ventaja/Bonus")]
    [Tooltip("Puntos extra para el ganador (el que termine primero).")]
    public float winnerBonusAmount = 500f;

    [Header("Eventos")]
    [Tooltip("Se dispara INMEDIATAMENTE cuando el primer jugador termina.")]
    public UnityEvent OnMinigameEnded;

    // Estado interno
    private Dictionary<int, ScissorsController> playerControllers = new Dictionary<int, ScissorsController>();
    private Dictionary<int, int> playerProgress = new Dictionary<int, int>();
    private Dictionary<int, CuttingPattern> activePatterns = new Dictionary<int, CuttingPattern>();

    // Bandera para asegurar que solo se dispara una vez
    private bool gameEnded = false;

    // --- MÉTODO PÚBLICO (Conectado al GameInitializer) ---
    public void RegisterPlayer(int playerIndex, GameObject playerObj, PlayerUIInfo uiInfo)
    {
        // Si el juego ya terminó (alguien ganó mientras este spawneaba), no hacemos nada
        if (gameEnded) return;

        ScissorsController sc = playerObj.GetComponent<ScissorsController>();
        if (sc == null) return;

        SpriteRenderer playerSprite = playerObj.GetComponentInChildren<SpriteRenderer>();
        if (playerSprite != null && uiInfo.frameOutline != null)
        {
            // Aplicamos el color que ya tienes configurado en el frameOutline de la UI
            playerSprite.color = uiInfo.frameOutline.color;
        }

        // Registrar datos
        playerControllers[playerIndex] = sc;
        playerProgress[playerIndex] = 0;

        // Inicializar controlador
        sc.Initialize(playerIndex, uiInfo, this);

        // Color
        if (uiInfo.frameOutline != null)
            sc.lineColor = uiInfo.frameOutline.color;

        // Spawnear primer patrón
        SpawnNextPattern(playerIndex);
    }

    // Llamado por ScissorsController
    public void OnPatternCompleted(int pIndex)
    {
        if (gameEnded) return;

        // 1. Limpieza
        if (activePatterns.ContainsKey(pIndex) && activePatterns[pIndex] != null)
        {
            Destroy(activePatterns[pIndex].gameObject);
            activePatterns.Remove(pIndex);
        }

        // 2. Avanzar progreso
        playerProgress[pIndex]++;

        // 3. Siguiente paso
        SpawnNextPattern(pIndex);
    }

    void SpawnNextPattern(int pIndex)
    {
        if (gameEnded) return;

        int patternIdx = playerProgress[pIndex];

        // --- CONDICIÓN DE VICTORIA ---
        // Si el índice supera la cantidad de prefabs, este jugador ha terminado todos.
        if (patternIdx >= patternPrefabs.Length)
        {
            HandleWinner(pIndex);
            return;
        }

        // Spawnear siguiente
        Transform spawnPt = transform;
        if (patternSpawnPoints != null && pIndex < patternSpawnPoints.Length)
        {
            spawnPt = patternSpawnPoints[pIndex];
        }

        CuttingPattern newPattern = Instantiate(patternPrefabs[patternIdx], spawnPt.position, spawnPt.rotation);
        activePatterns[pIndex] = newPattern;

        if (playerControllers.ContainsKey(pIndex))
        {
            playerControllers[pIndex].SetCurrentPattern(newPattern);
        }
    }

    void HandleWinner(int pIndex)
    {
        // Doble seguridad para evitar condiciones de carrera (dos terminan en el mismo frame)
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log($"ĄJUEGO TERMINADO! Ganador: Jugador {pIndex}");

        // 1. Dar la ventaja (Bonus)
        // Esto modifica el ScriptableObject (SessionData) antes de cambiar de escena
        if (playerControllers.ContainsKey(pIndex))
        {
            playerControllers[pIndex].ForceAddScore(winnerBonusAmount);
            Debug.Log($"Ventaja de {winnerBonusAmount} puntos otorgada al Jugador {pIndex}");
        }

        // 2. Desactivar inputs de los demás (Opcional, pero recomendado para limpieza visual)
        foreach (var kvp in playerControllers)
        {
            if (kvp.Value != null)
            {
                // Podrías añadir un método en ScissorsController para .DisableInput() si quieres
                // kvp.Value.enabled = false; 
            }
        }

        // 3. CAMBIO DE ESCENA
        // Aquí conectas tu SceneManager o lo que uses en el editor
        OnMinigameEnded?.Invoke();
    }
}