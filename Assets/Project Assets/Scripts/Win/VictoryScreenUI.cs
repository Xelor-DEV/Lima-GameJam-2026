using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VictoryScreenUI : MonoBehaviour
{
    [Header("UI References")]
    public Image characterIcon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI winText;
    public Image frameOutline;

    /// <summary>
    /// Configura la UI con los datos del ganador.
    /// </summary>
    /// <param name="winner">El objeto Player ganador</param>
    /// <param name="winMessage">El mensaje ya traducido y formateado</param>
    public void SetupUI(Player winner, string winMessage)
    {
        if (winner == null || winner.selectedCharacter == null) return;

        // 1. Icono del personaje
        characterIcon.sprite = winner.selectedCharacter.uiIcon;

        // 2. Nombre del personaje
        nameText.text = winner.selectedCharacter.characterName;

        // 3. Color del marco (Color del jugador)
        frameOutline.color = winner.color;

        // 4. Texto de victoria (Ej: "Jugador 1 ha ganado")
        winText.text = winMessage;
    }
}