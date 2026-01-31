using UnityEngine;
using UnityEngine.Localization;
using NexusChaser.CycloneAMS;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game/Character System/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("General Configuration")]
    public string characterName;
    public bool isSecret;

    [Tooltip("The phrase displayed or spoken when the player confirms/selects this character")]
    public LocalizedString selectionQuote;

    [Header("Visual Assets")]
    public Sprite selectorImage;
    public Sprite uiIcon;

    [Header("Dialogue & Audio")]
    public CharacterDialogueData dialogueData;

    [Tooltip("The short sound clip (blip) played repeatedly while text is typing")]
    public CycloneClip voiceBlip;
}