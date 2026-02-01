using UnityEngine;
using UnityEngine.Localization;
using System.Linq; // Necesario para sumar listas fácilmente

public class VictorySceneController : MonoBehaviour
{
    [Header("Data References")]
    public PlayersSessionData sessionData;

    [Header("Scene References")]
    [Tooltip("Punto en el mundo donde aparecerá el personaje ganador")]
    public Transform characterSpawnPoint;
    public VictoryScreenUI victoryUI;

    [Header("Localization")]
    [Tooltip("Debe contener un Smart String con {0} para el número de jugador")]
    public LocalizedString winnerAnnouncementString;

    private void Start()
    {
        CalculateAndShowWinner();
    }

    private void CalculateAndShowWinner()
    {
        if (sessionData == null || sessionData.players.Count == 0)
        {
            Debug.LogError("[VictorySceneController] No session data or players found!");
            return;
        }

        Player winner = null;
        float highestTotalScore = float.MinValue;

        // 1. Calcular puntajes
        foreach (var player in sessionData.players)
        {
            float currentTotal = 0f;

            // Sumamos todos los valores (si son negativos se restan automáticamente)
            if (player.scores != null)
            {
                currentTotal = player.scores.Sum(score => score.value);
            }

            Debug.Log($"Jugador {player.index + 1} Total: {currentTotal}");

            // Determinamos si es el nuevo líder
            // (Nota: En caso de empate, esto se queda con el primero que encontró, 
            // puedes agregar lógica extra aquí si quieres manejar empates)
            if (currentTotal > highestTotalScore)
            {
                highestTotalScore = currentTotal;
                winner = player;
            }
        }

        if (winner != null)
        {
            SpawnWinnerCharacter(winner);
            UpdateVictoryUI(winner);
        }
    }

    private void SpawnWinnerCharacter(Player winner)
    {
        // Verificamos que el personaje tenga un prefab asignado
        if (winner.selectedCharacter != null && winner.selectedCharacter.victoryPrefab != null)
        {
            Instantiate(
                winner.selectedCharacter.victoryPrefab,
                characterSpawnPoint.position,
                characterSpawnPoint.rotation,
                characterSpawnPoint
            );
        }
        else
        {
            Debug.LogWarning($"El personaje {winner.selectedCharacter?.characterName} no tiene un Victory Prefab asignado.");
        }
    }

    private void UpdateVictoryUI(Player winner)
    {
        // Obtenemos el string localizado. 
        // Asumimos que el string tiene formato "Player {0} has won"
        // Le pasamos (index + 1) para que muestre "Jugador 1" en vez de "Jugador 0"
        winnerAnnouncementString.Arguments = new object[] { winner.index + 1 };
        string localizedMessage = winnerAnnouncementString.GetLocalizedString();

        victoryUI.SetupUI(winner, localizedMessage);
    }
}