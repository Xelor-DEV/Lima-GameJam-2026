using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class FillMinigameManager : MonoBehaviour
{
    [Header("Configuración de Figuras")]
    public FillablePattern[] patternPrefabs; // Tus prefabs con PolygonCollider2D y FillablePattern
    public Transform[] patternSpawnPoints;

    [Header("Configuración de Ventaja/Bonus")]
    public float winnerBonusAmount = 500f;

    [Header("Eventos")]
    public UnityEvent OnMinigameEnded;

    // Diccionarios con los nuevos tipos
    private Dictionary<int, BrushController> playerControllers = new Dictionary<int, BrushController>();
    private Dictionary<int, int> playerProgress = new Dictionary<int, int>();
    private Dictionary<int, FillablePattern> activePatterns = new Dictionary<int, FillablePattern>();

    private bool gameEnded = false;

    // --- MÉTODO PÚBLICO para GameSceneInitializer ---
    public void RegisterPlayer(int playerIndex, GameObject playerObj, PlayerUIInfo uiInfo)
    {
        if (gameEnded) return;

        // Buscamos el BrushController en vez del ScissorsController
        BrushController bc = playerObj.GetComponent<BrushController>();

        // Si no tiene el componente correcto, salimos (seguridad por si usas el prefab equivocado)
        if (bc == null)
        {
            Debug.LogError("El objeto instanciado no tiene BrushController.");
            return;
        }

        // Configurar color visual del jugador (brocha)
        SpriteRenderer playerSprite = playerObj.GetComponentInChildren<SpriteRenderer>();
        if (playerSprite != null && uiInfo.frameOutline != null)
        {
            playerSprite.color = uiInfo.frameOutline.color;
        }

        playerControllers[playerIndex] = bc;
        playerProgress[playerIndex] = 0;

        // Inicializar controlador
        bc.Initialize(playerIndex, uiInfo, this);

        if (uiInfo.frameOutline != null)
            bc.brushColor = uiInfo.frameOutline.color;

        SpawnNextPattern(playerIndex);
    }

    public void OnPatternCompleted(int pIndex)
    {
        if (gameEnded) return;

        if (activePatterns.ContainsKey(pIndex) && activePatterns[pIndex] != null)
        {
            Destroy(activePatterns[pIndex].gameObject);
            activePatterns.Remove(pIndex);
        }

        playerProgress[pIndex]++;
        SpawnNextPattern(pIndex);
    }

    void SpawnNextPattern(int pIndex)
    {
        if (gameEnded) return;

        int patternIdx = playerProgress[pIndex];

        if (patternIdx >= patternPrefabs.Length)
        {
            HandleWinner(pIndex);
            return;
        }

        Transform spawnPt = transform;
        if (patternSpawnPoints != null && pIndex < patternSpawnPoints.Length)
        {
            spawnPt = patternSpawnPoints[pIndex];
        }

        FillablePattern newPattern = Instantiate(patternPrefabs[patternIdx], spawnPt.position, spawnPt.rotation);
        activePatterns[pIndex] = newPattern;

        if (playerControllers.ContainsKey(pIndex))
        {
            playerControllers[pIndex].SetCurrentPattern(newPattern);
        }
    }

    void HandleWinner(int pIndex)
    {
        if (gameEnded) return;

        gameEnded = true;
        Debug.Log($"¡JUEGO TERMINADO! Ganador: Jugador {pIndex}");

        if (playerControllers.ContainsKey(pIndex))
        {
            playerControllers[pIndex].ForceAddScore(winnerBonusAmount);
        }

        // Opcional: Desactivar inputs
        foreach (var ctrl in playerControllers.Values) ctrl.enabled = false;

        OnMinigameEnded?.Invoke();
    }
}