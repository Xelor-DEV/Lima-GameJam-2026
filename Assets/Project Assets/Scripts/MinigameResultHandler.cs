using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq; // Necesario para ordenar listas fácilmente

public class MinigameResultHandler : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tiempo extra de espera después de que terminen de hablar antes de cambiar escena")]
    public float postDialogueDelay = 2.0f;
    [Tooltip("Nombre de la escena a cargar (o usa índice)")]
    public string nextSceneName = "ResultsScreen"; // O "MapScreen", etc.

    [Header("References")]
    public PlayersSessionData sessionData;
    public PlayerUIInfo[] allPlayerUIs; // Arrástralos en el inspector o búscalos dinámicamente

    // Método a suscribir en el evento OnMinigameEnded del PatternMinigameManager
    public void OnMinigameEnded()
    {
        StartCoroutine(EndGameSequence());
    }

    IEnumerator EndGameSequence()
    {
        // 1. Encontrar el puntaje máximo para saber quién ganó
        // NOTA: Asumimos que allPlayerUIs tiene el orden correcto de jugadores (0, 1, 2...)
        // O mejor, usamos sessionData si está actualizado, pero UIInfo tiene el score visual actual.

        float maxScore = -1f;

        // Buscar puntaje más alto
        foreach (var ui in allPlayerUIs)
        {
            if (ui.gameObject.activeSelf)
            {
                float s = ui.GetCurrentScore();
                if (s > maxScore) maxScore = s;
            }
        }

        float maxDurationFound = 0f;

        // 2. Iterar jugadores y disparar diálogos
        for (int i = 0; i < allPlayerUIs.Length; i++)
        {
            PlayerUIInfo ui = allPlayerUIs[i];

            // Solo procesar si hay un jugador activo en ese slot
            if (ui.gameObject.activeSelf && i < sessionData.players.Count)
            {
                PlayerDialogueController dialogCtrl = ui.GetDialogueController();

                if (dialogCtrl != null)
                {
                    float currentScore = ui.GetCurrentScore();
                    // Es ganador si tiene el puntaje máximo (permite empates)
                    bool isWinner = (currentScore >= maxScore && maxScore > 0);

                    // Disparar diálogo y obtener cuánto tardará
                    float duration = dialogCtrl.PlayEndGameDialogue(isWinner);

                    if (duration > maxDurationFound)
                    {
                        maxDurationFound = duration;
                    }
                }
            }
        }

        // 3. Esperar a que el texto más largo termine de escribirse y leerse
        Debug.Log($"Esperando {maxDurationFound} segundos por diálogos finales...");
        yield return new WaitForSeconds(maxDurationFound);

        // 4. Esperar el delay configurable extra
        yield return new WaitForSeconds(postDialogueDelay);

        SceneLoader.Instance.LoadLevel(nextSceneName);
    }
}