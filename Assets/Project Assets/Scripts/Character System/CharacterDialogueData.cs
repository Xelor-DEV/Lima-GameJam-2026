using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Game/Character System/Dialogue Data")]
public class CharacterDialogueData : ScriptableObject
{
    [Header("Gameplay Chatter")]
    [Tooltip("Random phrases displayed during gameplay loops")]
    [ReorderableList(ListStyle.Boxed, "Random Phrases", Foldable = true)]
    public LocalizedString[] randomPhrases;

    [Header("Minigame Feedback")]
    [Tooltip("Phrases displayed when the player successfully completes a minigame")]
    [ReorderableList(ListStyle.Boxed, "On Minigame Success", Foldable = true)]
    public LocalizedString[] onMinigameSuccess;

    [Tooltip("Phrases displayed when the player fails a minigame")]
    [ReorderableList(ListStyle.Boxed, "On Minigame Fail", Foldable = true)]
    public LocalizedString[] onMinigameFail;

    [Header("Results")]
    [ReorderableList(ListStyle.Boxed, "Victory Phrases", Foldable = true)]
    public LocalizedString[] victoryPhrases;
}