using UnityEngine;
using UnityEngine.Localization;
using System.Linq;

public class VictorySceneController : MonoBehaviour
{
    [Header("Data References")]
    public PlayersSessionData sessionData;

    [Header("Scene References")]
    public Transform characterSpawnPoint;

    // Referencia al PlayerUIInfo que usaremos como pantalla de victoria
    public PlayerUIInfo winnerUI;

    [Header("Localization")]
    [Tooltip("Debe contener un Smart String con {0} para el número de jugador")]
    public LocalizedString winnerAnnouncementString;

    private void Start()
    {
        // Ocultamos la UI al inicio por si acaso
        if (winnerUI != null) winnerUI.gameObject.SetActive(false);

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

        // Calcular puntajes para encontrar al ganador
        foreach (var player in sessionData.players)
        {
            float currentTotal = 0f;
            if (player.scores != null)
            {
                currentTotal = player.scores.Sum(score => score.value);
            }

            if (currentTotal > highestTotalScore)
            {
                highestTotalScore = currentTotal;
                winner = player;
            }
        }

        if (winner != null)
        {
            SpawnWinnerCharacter(winner);

            if (winnerUI != null)
            {
                winnerUI.gameObject.SetActive(true);

                // 1. Preparamos el mensaje localizado (Ej: "Jugador 1 ha ganado")
                winnerAnnouncementString.Arguments = new object[] { winner.index + 1 };
                string victoryMessage = winnerAnnouncementString.GetLocalizedString();

                // 2. Llamamos al método modificado pasando el mensaje
                winnerUI.SetupVictoryUI(winner, victoryMessage);
            }
        }
    }

    private void SpawnWinnerCharacter(Player winner)
    {
        if (winner.selectedCharacter != null && winner.selectedCharacter.victoryPrefab != null)
        {
            Instantiate(
                winner.selectedCharacter.victoryPrefab,
                characterSpawnPoint.position,
                characterSpawnPoint.rotation,
                characterSpawnPoint
            );
        }
    }
}