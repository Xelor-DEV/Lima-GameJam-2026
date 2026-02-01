using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIInfo : MonoBehaviour
{
    [Header("UI Components")]
    public Image characterIcon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI scoreText; // Referencia al texto del puntaje
    public Image frameOutline;

    // Referencia local para manipular el puntaje en tiempo real si es necesario
    private MinigameScore linkedScoreData; 

    public void SetupUI(string charName, Sprite icon, Color playerColor, int playerNumber, MinigameScore scoreData)
    {
        characterIcon.sprite = icon;
        nameText.text = $"{charName} (P{playerNumber})";
        frameOutline.color = playerColor;
        
        // Guardamos la referencia y actualizamos el texto
        linkedScoreData = scoreData;
        UpdateScoreDisplay();
    }

    public void UpdateScoreDisplay()
    {
        if (linkedScoreData != null)
        {
            scoreText.text = linkedScoreData.value.ToString("0"); // Muestra entero, o "F1" para decimal
        }
    }

    // Método helper por si quieres sumar puntos durante el juego
    public void AddScore(float amount)
    {
        if (linkedScoreData != null)
        {
            linkedScoreData.value += amount;
            UpdateScoreDisplay();
        }
    }
}