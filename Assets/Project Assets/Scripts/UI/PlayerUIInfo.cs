using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIInfo : MonoBehaviour
{
    [Header("UI Components")]
    public Image characterIcon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText;
    public Image frameOutline;

    [Header("Dialogue System")]
    public GameObject dialogueContainer; // El objeto padre (burbuja/panel)
    public TextMeshProUGUI dialogueText; // El texto dentro de la burbuja

    // Referencias internas
    private MinigameScore linkedScoreData;
    private PlayerDialogueController dialogueController; // Referencia al nuevo script

    private bool isScoreLocked = false;

    // --- SETUP NORMAL (Minijuegos) ---
    public void SetupUI(string charName, Sprite icon, Color playerColor, int playerNumber, MinigameScore scoreData, CharacterData charData)
    {
        characterIcon.sprite = icon;
        nameText.text = $"{charName} (P{playerNumber})";
        frameOutline.color = playerColor;
        linkedScoreData = scoreData;
        isScoreLocked = false;

        UpdateScoreDisplay(); // Muestra números

        dialogueController = GetComponent<PlayerDialogueController>();
        if (dialogueController != null && charData != null)
        {
            dialogueController.Initialize(charData.dialogueData, playerNumber, charData);
        }
    }

    public void SetupUI(string charName, Sprite icon, Color playerColor, int playerNumber, MinigameScore scoreData)
    {
        SetupUI(charName, icon, playerColor, playerNumber, scoreData, null);
    }

    // --- SETUP DE VICTORIA (Modificado) ---
    public void SetupVictoryUI(Player winner, string victoryMessage)
    {
        if (winner == null || winner.selectedCharacter == null) return;

        CharacterData charData = winner.selectedCharacter;

        // 1. Configuración Visual
        characterIcon.sprite = charData.uiIcon;
        nameText.text = charData.characterName;
        frameOutline.color = winner.color;

        // CAMBIO PRINCIPAL: Asignamos el mensaje de texto en lugar del puntaje numérico
        scoreText.text = victoryMessage;

        // Desvinculamos el scoreData para que UpdateScoreDisplay no sobrescriba el texto con un "0"
        linkedScoreData = null;

        // 2. Configuración de Diálogo (Modo Victoria)
        dialogueController = GetComponent<PlayerDialogueController>();
        if (dialogueController != null)
        {
            dialogueController.InitializeVictory(charData.dialogueData);
            dialogueController.PlayRandomVictoryPhrase();
        }
    }
    public void UpdateScoreDisplay()
    {
        if (linkedScoreData != null)
        {
            scoreText.text = linkedScoreData.value.ToString("0");
        }
    }

    public void AddScore(float amount)
    {
        // --- MODIFICACIÓN: Si está bloqueado, no hacemos NADA ---
        if (isScoreLocked) return;

        if (linkedScoreData != null)
        {
            linkedScoreData.value += amount;
            UpdateScoreDisplay();
        }
    }

    // --- NUEVO: Método para cerrar el grifo de puntos ---
    public void LockScoring()
    {
        isScoreLocked = true;
    }

    // Método helper para acceder al controlador desde el orquestador
    public PlayerDialogueController GetDialogueController()
    {
        return dialogueController;
    }

    // Método helper para obtener el puntaje actual
    public float GetCurrentScore()
    {
        return linkedScoreData != null ? linkedScoreData.value : 0;
    }
}