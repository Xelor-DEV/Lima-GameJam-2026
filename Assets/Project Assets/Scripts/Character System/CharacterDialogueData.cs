using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Game/Character System/Dialogue Data")]
public class CharacterDialogueData : ScriptableObject
{
    [Header("Gameplay Chatter")]
    [Tooltip("Random phrases displayed during gameplay loops")]
    public LocalizedString[] randomPhrases;

    [Header("Minigame Feedback")]
    [Tooltip("Phrases displayed when the player successfully completes a minigame")]
    public LocalizedString[] onMinigameSuccess;

    [Tooltip("Phrases displayed when the player fails a minigame")]
    public LocalizedString[] onMinigameFail;

    [Header("Results")]
    public LocalizedString[] victoryPhrases;
}